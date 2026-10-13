using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Shopper.Model;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Core.Value.Model;
using System.Collections.Immutable;
using static AutoGcSupply.Tests.PriceFixtures;

namespace AutoGcSupply.Tests.Run;

public sealed class TodayBuilderTests
{
    private const int Revision = 7;
    private static readonly DateTime nowUtc = new(2026, 10, 13, 21, 30, 0, DateTimeKind.Utc);
    private static readonly IRowLabels labels = new FixedLabels();

    [Fact]
    public void LoggedOutShowsNothingAndBlocksStart()
    {
        var model = Build(Inputs() with { LoggedIn = false, HasCharacter = false, Character = default });

        Assert.Equal(TodayState.NotLoggedIn, model.State);
        Assert.Empty(model.Rows);
        Assert.Equal(StartBlock.NotLoggedIn, model.StartBlock);
        Assert.Equal(DayKey.For(nowUtc).NextResetUtc, model.Header.NextResetUtc);
        Assert.Equal("2026-10-13", model.Header.DayLabel);
    }

    [Fact]
    public void CharacterOutsideAGrandCompanyCannotStart()
    {
        var model = Build(Inputs() with { Character = CharacterFixtures.Standard() with { GrandCompany = 0 } });

        Assert.Equal(TodayState.NoGrandCompany, model.State);
        Assert.Equal(StartBlock.NoGrandCompany, model.StartBlock);
    }

    [Fact]
    public void StartWaitsForTheHomeWorldAndTheEndOfADuty()
    {
        var visiting = Build(Inputs() with { Character = CharacterFixtures.Standard() with { CurrentWorldId = CharacterFixtures.HomeWorldId + 1 } });
        var inDuty = Build(Inputs() with { InDuty = true });

        Assert.Equal(TodayState.Ready, visiting.State);
        Assert.Equal(StartBlock.NotOnHomeWorld, visiting.StartBlock);
        Assert.Equal(StartBlock.InDuty, inDuty.StartBlock);
    }

    [Fact]
    public void ReadingWhileTheTimersLoaderRuns()
    {
        var model = Build(Inputs() with { Reading = true });

        Assert.Equal(TodayState.Reading, model.State);
        Assert.Equal(StartBlock.NotLoaded, model.StartBlock);
    }

    [Fact]
    public void UnloadedRequestsAreNeverDone()
    {
        var model = Build(Inputs() with { Snapshot = RequestSnapshot.Unread(nowUtc), OwnedBySlot = ImmutableArray<OwnedCount>.Empty });

        Assert.Equal(TodayState.NotLoaded, model.State);
        Assert.Empty(model.Rows);
        Assert.Equal(StartBlock.NotLoaded, model.StartBlock);
    }

    [Fact]
    public void RequestsWithoutATimersPassTodayAreNotTrusted()
    {
        var stale = Build(Inputs() with { RequestsCurrent = false });
        var staleAllDone = Build(Inputs() with { RequestsCurrent = false, Snapshot = SupplyFixtures.Snapshot(SupplyFixtures.Closed(SupplyFixtures.OpenDayCopy())) });

        Assert.Equal(TodayState.NotLoaded, stale.State);
        Assert.Empty(stale.Rows);
        Assert.Equal(StartBlock.NotLoaded, stale.StartBlock);
        Assert.Equal(TodayState.NotLoaded, staleAllDone.State);
    }

    [Fact]
    public void ADayWithEveryRequestClosedIsAllDone()
    {
        var model = Build(Inputs() with { Snapshot = SupplyFixtures.Snapshot(SupplyFixtures.Closed(SupplyFixtures.OpenDayCopy())) });

        Assert.Equal(TodayState.AllDone, model.State);
        Assert.Equal(StartBlock.NothingSelected, model.StartBlock);
    }

    [Fact]
    public void RowsCarryTheirLabelsInDisplayOrder()
    {
        var model = Build(Inputs());

        Assert.Equal(TodayState.Ready, model.State);
        Assert.Equal(Revision, model.Revision);
        Assert.Equal(SupplyClasses.Count, model.Rows.Length);
        for (var index = 0; index < model.Rows.Length; index++)
        {
            var row = model.Rows[index];
            Assert.Equal(SupplyClasses.DisplayOrder[index], row.Decision.Request.ClassJobId);
            Assert.Equal($"class {row.Decision.Request.ClassJobId}", row.ClassName);
            Assert.Equal($"item {row.Decision.Request.ItemId}", row.ItemName);
            Assert.Equal(row.Decision.Request.ItemId + 20_000, row.ItemIconId);
        }

        Assert.Equal(CharacterFixtures.MaxLevel, model.Rows[0].Level);
        Assert.Equal(90, model.Rows[1].Level);
        Assert.Equal(new TodayHeader(1, 9, 12_000, 15_000, DayKey.For(nowUtc).NextResetUtc, "2026-10-13"), model.Header);
    }

    [Fact]
    public void WithoutAQuoteEveryRowToBuyWaitsForItsPrice()
    {
        var model = Build(Inputs(), new QuoteBook(), out var neededQuote);

        Assert.Equal(PricingState.Pending, model.Pricing);
        Assert.Equal(StartBlock.PricesPending, model.StartBlock);
        Assert.Equal(10, neededQuote.Length);
        Assert.Contains(new ShopperQuoteLine(5509, 20), neededQuote);
        Assert.DoesNotContain(neededQuote, static line => line.ItemId == 43273);
    }

    [Fact]
    public void ALandedQuotePricesTheRowsAndSuggestsACap()
    {
        var quotes = new QuoteBook();
        var needed = Needed();
        quotes.Begin("quote", needed, 0);
        quotes.Land(PricesFor(needed), nowUtc);

        var model = Build(Inputs() with { SpendCap = 0 }, quotes, out _);

        Assert.Equal(PricingState.Ready, model.Pricing);
        Assert.Equal(RowCoverage.ToBuy, model.Rows[8].Decision.Coverage);
        Assert.Equal(10, model.Totals.ToBuy);
        Assert.Equal(SpendCapRule.Suggest(model.Totals.EstimatedSpend), model.SuggestedSpendCap);
        Assert.Equal(StartBlock.NeedsSpendCap, model.StartBlock);
        Assert.Equal(StartBlock.None, Build(Inputs() with { SpendCap = 500_000 }, quotes, out _).StartBlock);
    }

    [Fact]
    public void AutoShopperMissingMakesPricingUnavailable()
    {
        var model = Build(Inputs() with { Shopper = ShopperState.Missing, ShopperDryRun = true });

        Assert.Equal(PricingState.Unavailable, model.Pricing);
        Assert.False(model.ShopperDryRun);
        Assert.Equal(StartBlock.ShopperNeeded, model.StartBlock);
    }

    [Fact]
    public void DryRunMirrorsAnAvailableAutoShopper()
    {
        Assert.True(Build(Inputs() with { ShopperDryRun = true }).ShopperDryRun);
    }

    [Fact]
    public void ARunningRunBlocksStartFirst()
    {
        Assert.Equal(StartBlock.Running, Build(Inputs() with { Running = true }).StartBlock);
    }

    private static TodayModel Build(in TodayInputs inputs) => Build(inputs, new QuoteBook(), out _);

    private static TodayModel Build(in TodayInputs inputs, QuoteBook quotes, out ShopperQuoteLine[] neededQuote)
        => TodayBuilder.Build(inputs, quotes, labels, Revision, out neededQuote);

    private static TodayInputs Inputs() => new()
    {
        LoggedIn = true,
        HasCharacter = true,
        Character = CharacterFixtures.Standard(),
        InDuty = false,
        Snapshot = SupplyFixtures.Snapshot(SupplyFixtures.OpenDay),
        RequestsCurrent = true,
        Reading = false,
        OwnedBySlot = ImmutableArray.Create(new OwnedCount[SupplyClasses.Count]),
        Settings = new DecisionSettings(false, true, SupplyQualityPolicy.Automatic, 25, ClassMask.None),
        Shopper = ShopperState.Available,
        ShopperDryRun = false,
        SpendCap = 100_000,
        SetupReady = true,
        Running = false,
        NowUtc = nowUtc,
    };

    private static ShopperQuoteLine[] Needed()
    {
        Build(Inputs(), new QuoteBook(), out var needed);
        return needed;
    }

    private static ItemPrice[] PricesFor(ShopperQuoteLine[] lines)
    {
        var prices = new ItemPrice[lines.Length];
        for (var index = 0; index < lines.Length; index++)
        {
            prices[index] = Item(lines[index].ItemId, lines[index].Quantity, PerUnit(lines[index].Quantity, 1_000), None);
        }

        return prices;
    }
}
