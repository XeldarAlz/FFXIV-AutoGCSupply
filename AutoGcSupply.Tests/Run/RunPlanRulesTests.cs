using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Core.Value.Model;
using static AutoGcSupply.Tests.PriceFixtures;
using static AutoGcSupply.Tests.Run.RunFixtures;

namespace AutoGcSupply.Tests.Run;

public sealed class RunPlanRulesTests
{
    private static readonly DateTime readAtUtc = new(2026, 10, 13, 21, 15, 0, DateTimeKind.Utc);

    [Fact]
    public void RowsStartFromWhatTheirCoveragePromised()
    {
        RowDecision[] rows =
        [
            Owned(SupplyClasses.Carpenter, CarpenterItem, 1),
            ToBuy(SupplyClasses.Culinarian, CulinarianItem, 3, ItemQuality.Normal, 3, 1_250),
            Row(SupplyClasses.Miner, MinerItem, 10, RowCoverage.Unpriced, ItemQuality.Normal, 10, 0, 0, 0, 0, 0),
            Row(SupplyClasses.Botanist, 5509, 20, RowCoverage.NoListings, ItemQuality.Normal, 20, 0, 0, 0, 0, 0),
        ];

        var progress = RunPlanRules.Initial(rows);

        Assert.Equal(RowProgress.Of(RowRunState.Waiting, RowNote.InBags), progress[0]);
        Assert.Equal(RowProgress.Of(RowRunState.Waiting, RowNote.ToBuy), progress[1]);
        Assert.Equal(RowProgress.Of(RowRunState.NotBought, RowNote.NoPrice), progress[2]);
        Assert.Equal(RowProgress.Of(RowRunState.NotBought, RowNote.NoListings), progress[3]);
    }

    [Fact]
    public void UnpricedRowsNameAMissingAutoShopperInsteadOfAMissingPrice()
    {
        RowDecision[] rows =
        [
            Owned(SupplyClasses.Carpenter, CarpenterItem, 1),
            Row(SupplyClasses.Miner, MinerItem, 10, RowCoverage.Unpriced, ItemQuality.Normal, 10, 0, 0, 0, 0, 0),
            Row(SupplyClasses.Botanist, 5509, 20, RowCoverage.NoListings, ItemQuality.Normal, 20, 0, 0, 0, 0, 0),
        ];
        var progress = RunPlanRules.Initial(rows);

        var noted = RunPlanRules.NoteUnpriced(rows, progress, ShopperState.Missing);

        Assert.Equal(1, noted);
        Assert.Equal(RowProgress.Of(RowRunState.Waiting, RowNote.InBags), progress[0]);
        Assert.Equal(RowProgress.Of(RowRunState.NotBought, RowNote.ShopperMissing), progress[1]);
        Assert.Equal(RowProgress.Of(RowRunState.NotBought, RowNote.NoListings), progress[2]);
        Assert.Equal(0, RunPlanRules.NoteUnpriced(rows, progress, ShopperState.Missing));
    }

    [Fact]
    public void UnpricedRowsKeepNoPriceWhileAutoShopperIsThere()
    {
        RowDecision[] rows = [Row(SupplyClasses.Miner, MinerItem, 10, RowCoverage.Unpriced, ItemQuality.Normal, 10, 0, 0, 0, 0, 0)];
        var progress = RunPlanRules.Initial(rows);

        Assert.Equal(0, RunPlanRules.NoteUnpriced(rows, progress, ShopperState.Available));
        Assert.Equal(RowProgress.Of(RowRunState.NotBought, RowNote.NoPrice), progress[0]);
    }

    [Theory]
    [InlineData((int)ShopperState.Missing, (int)RowNote.ShopperMissing)]
    [InlineData((int)ShopperState.Outdated, (int)RowNote.ShopperOutdated)]
    [InlineData((int)ShopperState.Newer, (int)RowNote.ShopperNewer)]
    [InlineData((int)ShopperState.Available, (int)RowNote.NoPrice)]
    public void EachAutoShopperStateNamesItsOwnNote(int shopper, int note)
    {
        Assert.Equal((RowNote)note, RunPlanRules.ShopperNote((ShopperState)shopper));
    }

    [Fact]
    public void ReadingDropsRowsHandedInElsewhereOrChangedAndKeepsTheRest()
    {
        RowDecision[] plan =
        [
            Owned(SupplyClasses.Carpenter, CarpenterItem, 1),
            Owned(SupplyClasses.Culinarian, CulinarianItem, 3),
            Owned(SupplyClasses.Miner, MinerItem, 10),
            Owned(SupplyClasses.Botanist, 5509, 20),
        ];
        var progress = RunPlanRules.Initial(plan);
        progress[3] = RowProgress.Of(RowRunState.Delivered, RowNote.HandedIn);
        var snapshot = Snapshot(
            readAtUtc,
            Request(SupplyClasses.Carpenter, CarpenterItem, 1, 1),
            Request(SupplyClasses.Culinarian, CulinarianItem, 3, 0),
            Request(SupplyClasses.Miner, 9_999, 10, 10),
            Request(SupplyClasses.Botanist, 5509, 20, 0));

        var dropped = RunPlanRules.DropChanged(plan, progress, snapshot);

        Assert.Equal(2, dropped);
        Assert.Equal(new[] { true, false, false, false }, Included(plan));
        Assert.Equal(RowRunState.Waiting, progress[0].State);
        Assert.Equal(RowProgress.Of(RowRunState.Skipped, RowNote.AlreadyHandedIn), progress[1]);
        Assert.Equal(RowProgress.Of(RowRunState.Skipped, RowNote.RequestChanged), progress[2]);
        Assert.Equal(RowProgress.Of(RowRunState.Delivered, RowNote.HandedIn), progress[3]);
    }

    [Fact]
    public void ANewDayKeepsOnlyRequestsThatStillMatch()
    {
        RowDecision[] plan = [Owned(SupplyClasses.Carpenter, CarpenterItem, 1), Owned(SupplyClasses.Miner, MinerItem, 10)];
        var progress = RunPlanRules.Initial(plan);
        var nextDay = Snapshot(readAtUtc.AddDays(1), Request(SupplyClasses.Carpenter, CarpenterItem, 1, 1), Request(SupplyClasses.Miner, 5_432, 5, 5));

        RunPlanRules.DropChanged(plan, progress, nextDay);

        Assert.True(plan[0].Included);
        Assert.False(plan[1].Included);
        Assert.Equal(RowNote.RequestChanged, progress[1].Note);
    }

    [Fact]
    public void ARowInterruptedMidHandInCountsOnceTheGameShowsItClosed()
    {
        RowDecision[] plan = [Owned(SupplyClasses.Carpenter, CarpenterItem, 1), Owned(SupplyClasses.Miner, MinerItem, 10)];
        var progress = new[] { RowProgress.Of(RowRunState.HandingIn, RowNote.HandingIn), RowProgress.Of(RowRunState.HandingIn, RowNote.HandingIn) };
        var snapshot = Snapshot(readAtUtc, Request(SupplyClasses.Carpenter, CarpenterItem, 1, 0), Request(SupplyClasses.Miner, MinerItem, 10, 10));

        RunPlanRules.DropChanged(plan, progress, snapshot);

        Assert.Equal(RowProgress.Of(RowRunState.Delivered, RowNote.HandedIn), progress[0]);
        Assert.Equal(RowProgress.Of(RowRunState.Waiting, RowNote.InBags), progress[1]);
        Assert.True(plan[1].Included);
    }

    [Fact]
    public void AFreshQuoteOnlyLowersCapsOrClosesLinesWithoutListings()
    {
        RowDecision[] plan =
        [
            ToBuy(SupplyClasses.Culinarian, CulinarianItem, 3, ItemQuality.Normal, 3, 1_250),
            ToBuy(SupplyClasses.Alchemist, 44157, 3, ItemQuality.High, 3, 3_750),
            ToBuy(SupplyClasses.Miner, MinerItem, 10, ItemQuality.Normal, 10, 263),
            Owned(SupplyClasses.Botanist, 5509, 20),
        ];
        var progress = RunPlanRules.Initial(plan);
        ItemPrice[] prices =
        [
            Item(CulinarianItem, 3, PerUnit(3, 800), None),
            Item(44157, 3, PerUnit(3, 1_000), None),
            Item(MinerItem, 10, PerUnit(10, 400), None),
        ];

        var changed = RunPlanRules.ApplyFreshPrices(plan, progress, prices, 25);

        Assert.Equal(2, changed);
        Assert.Equal(1_000, plan[0].MaxUnitPrice);
        Assert.Equal(RowCoverage.NoListings, plan[1].Coverage);
        Assert.False(plan[1].NeedsPurchase);
        Assert.Equal(RowProgress.Of(RowRunState.NotBought, RowNote.NoListings), progress[1]);
        Assert.Equal(263, plan[2].MaxUnitPrice);
        Assert.True(plan[3].ReadyToHandIn);
    }

    [Fact]
    public void SkippedBuyingMarksOnlyRowsThatStillNeededAPurchase()
    {
        RowDecision[] plan =
        [
            ToBuy(SupplyClasses.Culinarian, CulinarianItem, 3, ItemQuality.Normal, 3, 1_250),
            Owned(SupplyClasses.Miner, MinerItem, 10),
            Row(SupplyClasses.Botanist, 5509, 20, RowCoverage.NoListings, ItemQuality.Normal, 20, 0, 0, 0, 0, 0),
        ];
        var progress = RunPlanRules.Initial(plan);

        var marked = RunPlanRules.MarkUnbought(plan, progress, RowNote.DayChanged);

        Assert.Equal(1, marked);
        Assert.Equal(RowProgress.Of(RowRunState.NotBought, RowNote.DayChanged), progress[0]);
        Assert.Equal(RowRunState.Waiting, progress[1].State);
        Assert.Equal(RowNote.NoListings, progress[2].Note);
        Assert.True(RunPlanRules.AnyPurchase(plan));
        Assert.False(RunPlanRules.AnyPurchase(plan.AsSpan(1)));
    }

    [Fact]
    public void RequestsReadByATimersPassTodayNeedNoOtherPass()
    {
        var open = Snapshot(readAtUtc, Request(SupplyClasses.Carpenter, CarpenterItem, 1, 1));

        Assert.False(RunPlanRules.NeedsTimersPass(currentToday: true, handInStarted: false, open));
        Assert.True(RunPlanRules.NeedsTimersPass(currentToday: false, handInStarted: false, open));
    }

    [Fact]
    public void EverythingClosedAfterAHandInIsTrustedWithoutAPass()
    {
        var allClosed = Snapshot(readAtUtc, Request(SupplyClasses.Carpenter, CarpenterItem, 1, 0), Request(SupplyClasses.Miner, MinerItem, 10, 0));
        var oneOpen = Snapshot(readAtUtc, Request(SupplyClasses.Carpenter, CarpenterItem, 1, 0), Request(SupplyClasses.Miner, MinerItem, 10, 10));

        Assert.False(RunPlanRules.NeedsTimersPass(currentToday: false, handInStarted: true, allClosed));
        Assert.True(RunPlanRules.NeedsTimersPass(currentToday: false, handInStarted: true, oneOpen));
        Assert.True(RunPlanRules.NeedsTimersPass(currentToday: false, handInStarted: false, allClosed));
    }

    [Fact]
    public void ReleasingAStoppedHandInPutsTheRowBackInTheBags()
    {
        var progress = new[] { RowProgress.Of(RowRunState.HandingIn, RowNote.HandingIn), RowProgress.Of(RowRunState.Delivered, RowNote.HandedIn) };

        Assert.Equal(1, RunPlanRules.ReleaseHandingIn(progress));
        Assert.Equal(RowProgress.Of(RowRunState.Waiting, RowNote.InBags), progress[0]);
        Assert.Equal(RowRunState.Delivered, progress[1].State);
    }

    private static bool[] Included(RowDecision[] plan)
    {
        var included = new bool[plan.Length];
        for (var index = 0; index < plan.Length; index++)
        {
            included[index] = plan[index].Included;
        }

        return included;
    }
}
