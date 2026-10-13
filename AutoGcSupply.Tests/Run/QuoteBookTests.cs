using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Shopper.Model;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Core.Value.Model;
using System.Collections.Immutable;
using static AutoGcSupply.Tests.Run.RunFixtures;

namespace AutoGcSupply.Tests.Run;

public sealed class QuoteBookTests
{
    private static readonly DateTime pricedAtUtc = new(2026, 10, 13, 21, 0, 0, DateTimeKind.Utc);
    private static readonly ShopperQuoteLine[] twoItems = [new(CarpenterItem, 1), new(MinerItem, 10)];

    [Fact]
    public void QuoteLinesAskForTheRequestedCountOfEveryIncludedRowNotCoveredByTheBags()
    {
        RowDecision[] decisions =
        [
            ToBuy(SupplyClasses.Carpenter, CarpenterItem, 1, ItemQuality.High, 1, 10_000),
            Row(SupplyClasses.Blacksmith, CarpenterItem, 2, RowCoverage.Unpriced, ItemQuality.Normal, 2, 0, 2, 0, 0, 0),
            Row(SupplyClasses.Armorer, 3, 1, RowCoverage.NoListings, ItemQuality.Normal, 1, 0, 0, 0, 0, 0),
            Owned(SupplyClasses.Goldsmith, 4, 1),
            Row(SupplyClasses.Leatherworker, 5, 1, RowCoverage.Unpriced, ItemQuality.Normal, 1, 0, 0, 0, 0, 0, included: false),
            ToBuy(SupplyClasses.Miner, MinerItem, 10, ItemQuality.Normal, 4, 263),
        ];

        var lines = QuoteLines.ForDecisions(decisions);

        Assert.Equal(new ShopperQuoteLine[] { new(CarpenterItem, 3), new(3, 1), new(MinerItem, 10) }, lines);
        Assert.Equal(new ShopperQuoteLine[] { new(CarpenterItem, 1), new(MinerItem, 10) }, QuoteLines.ForPurchases(decisions));
    }

    [Fact]
    public void CoverageNeedsEveryItemInAtLeastTheQuantity()
    {
        Assert.True(QuoteLines.Covers(twoItems, [new(MinerItem, 10)]));
        Assert.True(QuoteLines.Covers(twoItems, [new(MinerItem, 4), new(CarpenterItem, 1)]));
        Assert.False(QuoteLines.Covers(twoItems, [new(MinerItem, 11)]));
        Assert.False(QuoteLines.Covers(twoItems, [new(CulinarianItem, 1)]));
        Assert.True(QuoteLines.Covers(twoItems, []));
    }

    [Fact]
    public void AQuoteIsNeededUntilOneCoversTheItems()
    {
        var book = new QuoteBook();

        Assert.False(book.NeedsQuote([]));
        Assert.True(book.NeedsQuote(twoItems));
        Assert.Equal(PricingState.Pending, book.StateFor(twoItems, shopperAvailable: true));

        book.Begin("quote", twoItems, 1_000);
        Assert.False(book.NeedsQuote(twoItems));
        Assert.Equal(PricingState.Pending, book.StateFor(twoItems, shopperAvailable: true));

        book.Land([], pricedAtUtc);
        Assert.False(book.NeedsQuote([new(MinerItem, 10)]));
        Assert.Equal(PricingState.Ready, book.StateFor(twoItems, shopperAvailable: true));
        Assert.True(book.NeedsQuote([new(CulinarianItem, 3)]));
    }

    [Fact]
    public void PricingStateNamesTheMissingShopperAndTheEmptyList()
    {
        var book = new QuoteBook();

        Assert.Equal(PricingState.Unavailable, book.StateFor(twoItems, shopperAvailable: false));
        Assert.Equal(PricingState.NotNeeded, book.StateFor([], shopperAvailable: true));
    }

    [Fact]
    public void AFailedQuoteWaitsForRefreshUnlessTheItemsChange()
    {
        var book = new QuoteBook();
        book.Begin("quote", twoItems, 0);
        book.Fail(twoItems);

        Assert.False(book.NeedsQuote(twoItems));
        Assert.Equal(PricingState.Failed, book.StateFor(twoItems, shopperAvailable: true));
        Assert.True(book.NeedsQuote([new(CulinarianItem, 3)]));

        book.Clear();
        Assert.True(book.NeedsQuote(twoItems));
    }

    [Fact]
    public void APendingQuoteTimesOutAfterTwoMinutes()
    {
        var book = new QuoteBook();
        book.Begin("quote", twoItems, 10_000);

        Assert.False(book.TimedOut(10_000 + QuoteBook.TimeoutMs - 1));
        Assert.True(book.TimedOut(10_000 + QuoteBook.TimeoutMs));
    }

    [Fact]
    public void AnExpiredQuoteIsAskedAgainAFewTimesThenFails()
    {
        var book = new QuoteBook();
        for (var attempt = 1; attempt <= QuoteBook.MaxExpiredRetries; attempt++)
        {
            book.Begin("quote", twoItems, 0);
            Assert.True(book.Expire());
            Assert.Equal(QuoteStatus.Idle, book.Status);
            Assert.Equal(twoItems, book.PendingLines);
        }

        book.Begin("quote", twoItems, 0);
        Assert.False(book.Expire());
        Assert.Equal(QuoteStatus.Failed, book.Status);
    }

    [Fact]
    public void AQuoteIsFreshForTenMinutes()
    {
        var book = new QuoteBook();
        Assert.False(book.IsFresh([], pricedAtUtc));

        book.Begin("quote", twoItems, 0);
        book.Land([], pricedAtUtc);

        Assert.True(book.IsFresh([new(MinerItem, 10)], pricedAtUtc + QuoteBook.FreshFor));
        Assert.False(book.IsFresh([new(MinerItem, 10)], pricedAtUtc + QuoteBook.FreshFor + TimeSpan.FromSeconds(1)));
        Assert.False(book.IsFresh([new(CulinarianItem, 1)], pricedAtUtc));
    }

    [Fact]
    public void ReadyPricesStayWhileTheNextQuoteIsPending()
    {
        var book = new QuoteBook();
        book.Begin("first", twoItems, 0);
        book.Land([PriceFixtures.Item(MinerItem, 10, PriceFixtures.PerUnit(10, 210), PriceFixtures.None)], pricedAtUtc);

        book.Begin("second", [new(CulinarianItem, 3)], 0);

        Assert.Equal(1, book.Prices.Length);
        Assert.Equal(PricingState.Pending, book.StateFor([new(CulinarianItem, 3)], shopperAvailable: true));
    }

    [Fact]
    public void QuoteItemsBecomeDecisionPricesWithTheirHighestListing()
    {
        var items = ImmutableArray.Create(new ShopperQuoteItem(MinerItem, 10, new ShopperQuotePrice(true, 10, 2_100, 210, 260), new ShopperQuotePrice(false, 0, 0, 0)));

        var prices = QuotePrices.From(items);

        Assert.Equal(new ItemPrice(MinerItem, 10, new QualityPrice(true, 10, 2_100, 210, 260), new QualityPrice(false, 0, 0, 0)), Assert.Single(prices));
    }
}
