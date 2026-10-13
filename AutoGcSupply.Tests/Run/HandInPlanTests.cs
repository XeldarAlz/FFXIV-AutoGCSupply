using AutoGcSupply.Core.HandIn.Model;
using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Core.Value.Model;
using static AutoGcSupply.Tests.Run.RunFixtures;

namespace AutoGcSupply.Tests.Run;

public sealed class HandInPlanTests
{
    [Fact]
    public void TargetsEveryRowTheBagsNowCoverInItsQuality()
    {
        RowDecision[] plan =
        [
            ToBuy(SupplyClasses.Carpenter, CarpenterItem, 1, ItemQuality.High, 1, 10_000, seals: 840),
            Owned(SupplyClasses.Culinarian, CulinarianItem, 3, ItemQuality.Normal, seals: 410),
            Owned(SupplyClasses.Miner, MinerItem, 10, ItemQuality.Normal, seals: 300),
        ];
        var progress = RunPlanRules.Initial(plan);
        progress[0] = progress[0].With(RowRunState.Bought, RowNote.Bought);
        OwnedCount[] owned = [new(5, 1), new(3, 0), new(9, 4)];

        var handIn = HandInPlan.Build(plan, progress, owned, purchasesWereDry: false);

        Assert.Equal(new HandInTarget[]
        {
            new(CarpenterItem, SupplyClasses.Carpenter, 1, ItemQuality.High, 840),
            new(CulinarianItem, SupplyClasses.Culinarian, 3, ItemQuality.Normal, 410),
        }, handIn.Targets);
        Assert.Equal(new[] { 0, 1 }, handIn.RowOfTarget);
        Assert.Equal(RowProgress.Of(RowRunState.Skipped, RowNote.NotInBags), progress[2]);
    }

    [Fact]
    public void RowsSharingAnItemShareTheBags()
    {
        RowDecision[] plan =
        [
            Owned(SupplyClasses.Culinarian, CulinarianItem, 3),
            Owned(SupplyClasses.Alchemist, CulinarianItem, 2),
        ];
        var progress = RunPlanRules.Initial(plan);

        var handIn = HandInPlan.Build(plan, progress, [new(4, 0), new(4, 0)], purchasesWereDry: false);

        Assert.Single(handIn.Targets);
        Assert.Equal(RowNote.NotInBags, progress[1].Note);
    }

    [Fact]
    public void DeliveredAndDroppedRowsAreLeftOut()
    {
        RowDecision[] plan =
        [
            Owned(SupplyClasses.Carpenter, CarpenterItem, 1),
            Owned(SupplyClasses.Miner, MinerItem, 10) with { Included = false },
        ];
        var progress = RunPlanRules.Initial(plan);
        progress[0] = RowProgress.Of(RowRunState.Delivered, RowNote.HandedIn);

        var handIn = HandInPlan.Build(plan, progress, [new(9, 9), new(99, 0)], purchasesWereDry: false);

        Assert.Empty(handIn.Targets);
        Assert.Equal(RowRunState.Delivered, progress[0].State);
        Assert.Equal(RowRunState.Waiting, progress[1].State);
    }

    [Fact]
    public void RowsNotBoughtKeepTheirReasonAndDryPurchasesWouldBeHandedIn()
    {
        RowDecision[] plan =
        [
            ToBuy(SupplyClasses.Culinarian, CulinarianItem, 3, ItemQuality.Normal, 3, 1_250),
            ToBuy(SupplyClasses.Miner, MinerItem, 10, ItemQuality.Normal, 10, 263),
        ];
        var progress = RunPlanRules.Initial(plan);
        progress[0] = progress[0].With(RowRunState.NotBought, RowNote.NoStock);
        progress[1] = progress[1].With(RowRunState.Bought, RowNote.Bought);

        var handIn = HandInPlan.Build(plan, progress, [default, default], purchasesWereDry: true);

        Assert.Empty(handIn.Targets);
        Assert.Equal(RowProgress.Of(RowRunState.NotBought, RowNote.NoStock), progress[0]);
        Assert.Equal(RowProgress.Of(RowRunState.WouldDeliver, RowNote.WouldHandIn), progress[1]);
    }

    [Fact]
    public void HighQualitySupplyCarriesTheDoubledSealReward()
    {
        var decisions = DecisionBuilder.Build(
            SupplyFixtures.Snapshot(SupplyFixtures.OpenDay),
            CharacterFixtures.Standard(),
            BagsWithOneHighQualityWeaverItem(),
            [],
            new DecisionSettings(false, true, SupplyQualityPolicy.Automatic, 25, ClassMask.None));
        var weaver = decisions[SupplyClasses.DisplayIndex(SupplyClasses.Weaver)];
        RowDecision[] plan = [weaver];

        var handIn = HandInPlan.Build(plan, RunPlanRules.Initial(plan), [new(0, 1)], purchasesWereDry: false);

        Assert.Equal(ItemQuality.High, weaver.ChosenQuality);
        Assert.Equal(848, Assert.Single(handIn.Targets).SealReward);
    }

    private static OwnedCount[] BagsWithOneHighQualityWeaverItem()
    {
        var owned = new OwnedCount[SupplyClasses.Count];
        owned[6] = new OwnedCount(0, 1);
        return owned;
    }
}
