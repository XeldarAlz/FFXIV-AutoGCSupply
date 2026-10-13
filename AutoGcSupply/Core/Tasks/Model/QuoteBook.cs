using AutoGcSupply.Core.Shopper.Model;
using AutoGcSupply.Core.Value.Model;

namespace AutoGcSupply.Core.Tasks.Model;

internal enum QuoteStatus : byte { Idle, Pending, Ready, Failed, Waiting }

internal sealed class QuoteBook
{
    public const long TimeoutMs = 600_000;
    public const long BusyRetryMs = 10_000;
    public const int MaxExpiredRetries = 3;

    public static readonly TimeSpan FreshFor = TimeSpan.FromMinutes(10);

    private ShopperQuoteLine[] pendingLines = [];
    private ShopperQuoteLine[] readyLines = [];
    private ShopperQuoteLine[] failedLines = [];
    private ItemPrice[] prices = [];

    public QuoteStatus Status { get; private set; } = QuoteStatus.Idle;

    public string QuoteId { get; private set; } = string.Empty;

    public long StartedAtMs { get; private set; }

    public long RetryAtMs { get; private set; }

    public DateTime PricedAtUtc { get; private set; }

    public int ExpiredCount { get; private set; }

    public int Revision { get; private set; }

    public ReadOnlySpan<ItemPrice> Prices => prices;

    public ShopperQuoteLine[] PendingLines => pendingLines;

    public bool Outstanding => Status is QuoteStatus.Pending or QuoteStatus.Waiting;

    public bool NeedsQuote(ReadOnlySpan<ShopperQuoteLine> needed)
    {
        if (needed.IsEmpty || Outstanding || QuoteLines.Covers(readyLines, needed))
        {
            return false;
        }

        return Status != QuoteStatus.Failed || !QuoteLines.Covers(failedLines, needed);
    }

    public bool IsFresh(ReadOnlySpan<ShopperQuoteLine> needed, DateTime nowUtc)
        => readyLines.Length > 0 && QuoteLines.Covers(readyLines, needed) && nowUtc - PricedAtUtc <= FreshFor;

    public bool AsksFor(ReadOnlySpan<ShopperQuoteLine> needed) => Outstanding && QuoteLines.Covers(pendingLines, needed);

    public bool TimedOut(long nowMs) => Outstanding && nowMs - StartedAtMs >= TimeoutMs;

    public bool RetryDue(long nowMs) => Status == QuoteStatus.Waiting && nowMs >= RetryAtMs;

    public PricingState StateFor(ReadOnlySpan<ShopperQuoteLine> needed, bool shopperAvailable)
    {
        if (!shopperAvailable)
        {
            return PricingState.Unavailable;
        }

        if (needed.IsEmpty)
        {
            return PricingState.NotNeeded;
        }

        if (QuoteLines.Covers(readyLines, needed))
        {
            return PricingState.Ready;
        }

        if (Status == QuoteStatus.Failed && QuoteLines.Covers(failedLines, needed))
        {
            return PricingState.Failed;
        }

        return Status == QuoteStatus.Waiting ? PricingState.Queued : PricingState.Pending;
    }

    public void Begin(string quoteId, ShopperQuoteLine[] lines, long nowMs)
    {
        QuoteId = quoteId;
        pendingLines = lines;
        StartedAtMs = nowMs;
        Status = QuoteStatus.Pending;
        Revision++;
    }

    public void Defer(ShopperQuoteLine[] lines, long nowMs)
    {
        if (Status != QuoteStatus.Waiting)
        {
            StartedAtMs = nowMs;
        }

        pendingLines = lines;
        RetryAtMs = nowMs + BusyRetryMs;
        Settle(QuoteStatus.Waiting);
    }

    public void Land(ItemPrice[] landed, DateTime pricedAtUtc)
    {
        readyLines = pendingLines;
        prices = landed;
        PricedAtUtc = pricedAtUtc;
        ExpiredCount = 0;
        Settle(QuoteStatus.Ready);
    }

    public void Fail(ShopperQuoteLine[] lines)
    {
        failedLines = lines;
        Settle(QuoteStatus.Failed);
    }

    public bool Expire()
    {
        ExpiredCount++;
        if (ExpiredCount > MaxExpiredRetries)
        {
            Fail(pendingLines);
            return false;
        }

        Settle(QuoteStatus.Idle);
        return true;
    }

    public void Retry()
    {
        if (Status != QuoteStatus.Failed)
        {
            return;
        }

        failedLines = [];
        ExpiredCount = 0;
        Settle(QuoteStatus.Idle);
    }

    public void Clear()
    {
        readyLines = [];
        failedLines = [];
        prices = [];
        PricedAtUtc = default;
        ExpiredCount = 0;
        if (Outstanding)
        {
            Revision++;
            return;
        }

        pendingLines = [];
        Settle(QuoteStatus.Idle);
    }

    private void Settle(QuoteStatus status)
    {
        Status = status;
        QuoteId = string.Empty;
        Revision++;
    }
}
