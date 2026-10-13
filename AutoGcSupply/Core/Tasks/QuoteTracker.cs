using AutoGcSupply.Core.Shopper;
using AutoGcSupply.Core.Shopper.Model;
using AutoGcSupply.Core.Tasks.Model;

namespace AutoGcSupply.Core.Tasks;

internal sealed class QuoteTracker
{
    public QuoteBook Book { get; } = new();

    public void Start(ShopperQuoteLine[] lines)
    {
        var acceptance = AutoShopperApi.Instance.QuoteStart(lines);
        if (acceptance.Accepted)
        {
            Book.Begin(acceptance.Id, lines, Environment.TickCount64);
            RunLog.Debug($"Quote {acceptance.Id} started for {lines.Length} item(s).");
            return;
        }

        RunLog.Warning($"Auto Shopper refused the quote ({acceptance.Reply}, {acceptance.RefusalName}): {acceptance.Message}");
        Book.Fail(lines);
    }

    public void Poll()
    {
        if (Book.Status != QuoteStatus.Pending)
        {
            return;
        }

        if (Book.TimedOut(Environment.TickCount64))
        {
            RunLog.Warning($"Quote {Book.QuoteId} gave no prices within {QuoteBook.TimeoutMs / TimeUnits.MillisecondsPerSecond}s.");
            Book.Fail(Book.PendingLines);
            return;
        }

        var quote = AutoShopperApi.Instance.QuoteResult(Book.QuoteId);
        if (quote.Reply != ShopperReply.Valid)
        {
            return;
        }

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
