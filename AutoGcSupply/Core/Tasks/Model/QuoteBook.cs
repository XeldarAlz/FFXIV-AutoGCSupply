using AutoGcSupply.Core.Shopper.Model;
using AutoGcSupply.Core.Value.Model;

namespace AutoGcSupply.Core.Tasks.Model;

internal enum QuoteStatus : byte { Idle, Pending, Ready, Failed }

internal sealed class QuoteBook
{
    public const long TimeoutMs = 120_000;
    public const int MaxExpiredRetries = 3;

    public static readonly TimeSpan FreshFor = TimeSpan.FromMinutes(10);

    private ShopperQuoteLine[] pendingLines = [];
    private ShopperQuoteLine[] readyLines = [];
    private ShopperQuoteLine[] failedLines = [];
    private ItemPrice[] prices = [];

    public QuoteStatus Status { get; private set; } = QuoteStatus.Idle;

    public string QuoteId { get; private set; } = string.Empty;

    public long StartedAtMs { get; private set; }

    public DateTime PricedAtUtc { get; private set; }

    public int ExpiredCount { get; private set; }

    public int Revision { get; private set; }

    public ReadOnlySpan<ItemPrice> Prices => prices;

    public ShopperQuoteLine[] PendingLines => pendingLines;

    public bool NeedsQuote(ReadOnlySpan<ShopperQuoteLine> needed)
    {
        if (needed.IsEmpty || Status == QuoteStatus.Pending || QuoteLines.Covers(readyLines, needed))
        {
            return false;
        }

        return Status != QuoteStatus.Failed || !QuoteLines.Covers(failedLines, needed);
    }

    public bool IsFresh(ReadOnlySpan<ShopperQuoteLine> needed, DateTime nowUtc)
        => readyLines.Length > 0 && QuoteLines.Covers(readyLines, needed) && nowUtc - PricedAtUtc <= FreshFor;

    public bool TimedOut(long nowMs) => Status == QuoteStatus.Pending && nowMs - StartedAtMs >= TimeoutMs;

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

        return Status == QuoteStatus.Failed && QuoteLines.Covers(failedLines, needed) ? PricingState.Failed : PricingState.Pending;
    }

    public void Begin(string quoteId, ShopperQuoteLine[] lines, long nowMs)
    {
        QuoteId = quoteId;
        pendingLines = lines;
        StartedAtMs = nowMs;
        Status = QuoteStatus.Pending;
        Revision++;
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

    public void Clear()
    {
        pendingLines = [];
        readyLines = [];
        failedLines = [];
        prices = [];
        PricedAtUtc = default;
        ExpiredCount = 0;
        Settle(QuoteStatus.Idle);
    }

    private void Settle(QuoteStatus status)
    {
        Status = status;
        QuoteId = string.Empty;
        Revision++;
    }
}
