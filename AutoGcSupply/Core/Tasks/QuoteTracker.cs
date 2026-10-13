using AutoGcSupply.Core.Shopper;
using AutoGcSupply.Core.Shopper.Model;
using AutoGcSupply.Core.Tasks.Model;

namespace AutoGcSupply.Core.Tasks;

internal sealed class QuoteTracker
{
    private const int MaxUnreadableReplies = 30;

    private int unreadable;

    public QuoteBook Book { get; } = new();

    public void Start(ShopperQuoteLine[] lines)
    {
        var acceptance = AutoShopperApi.Instance.QuoteStart(lines);
        var nowMs = Environment.TickCount64;
        if (acceptance.Accepted)
        {
            unreadable = 0;
            Book.Begin(acceptance.Id, lines, nowMs);
            RunLog.Debug($"Quote {acceptance.Id} started for {lines.Length} item(s).");
            return;
        }

        if (acceptance.Refusal == ShopperRefusal.Busy)
        {
            Book.Defer(lines, nowMs);
            RunLog.Debug($"Auto Shopper still has earlier quotes waiting; asking again in {QuoteBook.BusyRetryMs / TimeUnits.MillisecondsPerSecond}s.");
            return;
        }

        RunLog.Warning($"Auto Shopper refused the quote ({acceptance.Reply}, {acceptance.RefusalName}): {acceptance.Message}");
        Book.Fail(lines);
    }

    public void Poll()
    {
        var nowMs = Environment.TickCount64;
        if (Book.TimedOut(nowMs))
        {
            RunLog.Warning($"Auto Shopper gave no prices within {QuoteBook.TimeoutMs / TimeUnits.MillisecondsPerMinute} minutes ({Book.Status}).");
            Book.Fail(Book.PendingLines);
            return;
        }

        if (Book.RetryDue(nowMs))
        {
            Start(Book.PendingLines);
            return;
        }

        if (Book.Status != QuoteStatus.Pending)
        {
            return;
        }

        var quote = AutoShopperApi.Instance.QuoteResult(Book.QuoteId);
        if (quote.Reply != ShopperReply.Valid)
        {
            unreadable++;
            if (unreadable >= MaxUnreadableReplies)
            {
                RunLog.Warning($"Auto Shopper gave no usable answer about quote {Book.QuoteId} {MaxUnreadableReplies} times in a row ({quote.Reply}).");
                Book.Fail(Book.PendingLines);
            }

            return;
        }

        unreadable = 0;

        switch (quote.State)
        {
            case ShopperQuoteState.Ready:
                Book.Land(QuotePrices.From(quote.Items), quote.FetchedAtUtc);
                RunLog.Debug($"Quote {quote.QuoteId} is ready with {quote.Items.Length} item(s).");
                break;
            case ShopperQuoteState.Failed:
                RunLog.Warning($"Quote {quote.QuoteId} failed ({quote.RefusalName}).");
                Book.Fail(Book.PendingLines);
                break;
            case ShopperQuoteState.Unknown:
                Restart();
                break;
        }
    }

    public void Clear() => Book.Clear();

    public void Retry() => Book.Retry();

    private void Restart()
    {
        var lines = Book.PendingLines;
        if (!Book.Expire())
        {
            RunLog.Warning($"Auto Shopper kept forgetting the quote; giving up after {QuoteBook.MaxExpiredRetries} retries.");
            return;
        }

        RunLog.Debug("Auto Shopper no longer knows the quote; asking again.");
        Start(lines);
    }
}
