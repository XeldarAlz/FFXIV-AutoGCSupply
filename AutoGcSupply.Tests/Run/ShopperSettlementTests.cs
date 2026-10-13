using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Shopper.Model;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Core.Value.Model;
using System.Collections.Immutable;
using static AutoGcSupply.Tests.Run.RunFixtures;

namespace AutoGcSupply.Tests.Run;

public sealed class ShopperSettlementTests
{
    [Fact]
    public void ARunStillGoingKeepsBuying()
    {
        Assert.Equal(ShopperSettlement.Running, ShopperSettlementRule.Classify(RunView(false, ShopperRunOutcome.None), stopRequested: false));
    }

    [Theory]
    [InlineData((int)ShopperRunOutcome.Completed)]
    [InlineData((int)ShopperRunOutcome.CompletedPartial)]
    [InlineData((int)ShopperRunOutcome.Failed)]
    public void AFinishedRunGoesOnToTheHandIn(int outcome)
    {
        Assert.Equal(ShopperSettlement.Continue, ShopperSettlementRule.Classify(RunView(true, (ShopperRunOutcome)outcome), stopRequested: false));
    }

    [Fact]
    public void AnUncertainPurchaseNeedsThePlayer()
    {
        var view = RunView(true, ShopperRunOutcome.NeedsReconciliation) with { NeedsPlayer = true };

        Assert.Equal(ShopperSettlement.NeedsPlayer(NeedsPlayerReason.ShopperUncertainPurchase), ShopperSettlementRule.Classify(view, stopRequested: true));
    }

    [Fact]
    public void AnInterruptedRunNeedsThePlayer()
    {
        var view = RunView(true, ShopperRunOutcome.Stopped) with { NeedsPlayer = true };

        Assert.Equal(ShopperSettlement.NeedsPlayer(NeedsPlayerReason.ShopperInterrupted), ShopperSettlementRule.Classify(view, stopRequested: false));
    }

    [Fact]
    public void ARunStoppedOnRequestEndsStopped()
    {
        Assert.Equal(ShopperSettlement.Stopped, ShopperSettlementRule.Classify(RunView(true, ShopperRunOutcome.Stopped), stopRequested: true));
        Assert.Equal(ShopperSettlement.Stopped, ShopperSettlementRule.Classify(RunView(true, ShopperRunOutcome.Completed), stopRequested: true));
    }

    [Fact]
    public void ARunStoppedInsideAutoShopperTellsThePlayerInsteadOfEndingQuietly()
    {
        Assert.Equal(ShopperSettlement.NeedsPlayer(NeedsPlayerReason.ShopperStopped), ShopperSettlementRule.Classify(RunView(true, ShopperRunOutcome.Stopped), stopRequested: false));
    }

    [Fact]
    public void APreflightRefusalNeedsThePlayerUnlessNothingWasLeftToBuy()
    {
        var refused = RunView(true, ShopperRunOutcome.Stopped) with { Refusal = ShopperRefusal.PricesUnavailable };
        var nothingToBuy = RunView(true, ShopperRunOutcome.Stopped) with { Refusal = ShopperRefusal.NothingToBuy };

        Assert.Equal(ShopperSettlement.NeedsPlayer(NeedsPlayerReason.ShopperRefused), ShopperSettlementRule.Classify(refused, stopRequested: false));
        Assert.Equal(ShopperSettlement.Continue, ShopperSettlementRule.Classify(nothingToBuy, stopRequested: false));
    }

    [Fact]
    public void AnUnreadableOrForgottenRunIsNotSettled()
    {
        Assert.Equal(ShopperSettlement.Unreadable, ShopperSettlementRule.Classify(ShopperRunView.Failed(ShopperReply.Unavailable), stopRequested: false));
        Assert.Equal(ShopperSettlement.Unreadable, ShopperSettlementRule.Classify(ShopperRunView.UnknownRun("4e1a"), stopRequested: false));
    }

    [Theory]
    [InlineData((int)ShopperRefusal.ConsentRequired, (int)NeedsPlayerReason.ShopperConsent)]
    [InlineData((int)ShopperRefusal.ConsentDenied, (int)NeedsPlayerReason.ShopperConsent)]
    [InlineData((int)ShopperRefusal.NeedsReconciliation, (int)NeedsPlayerReason.ShopperUncertainPurchase)]
    [InlineData((int)ShopperRefusal.InterruptedRun, (int)NeedsPlayerReason.ShopperInterrupted)]
    [InlineData((int)ShopperRefusal.SetupNeeded, (int)NeedsPlayerReason.ShopperNotReady)]
    [InlineData((int)ShopperRefusal.JournalDamaged, (int)NeedsPlayerReason.ShopperNotReady)]
    [InlineData((int)ShopperRefusal.JournalWriteFailed, (int)NeedsPlayerReason.ShopperNotReady)]
    [InlineData((int)ShopperRefusal.Busy, (int)NeedsPlayerReason.ShopperNotReady)]
    [InlineData((int)ShopperRefusal.ReadingUnavailable, (int)NeedsPlayerReason.ShopperNotReady)]
    [InlineData((int)ShopperRefusal.BuyingUnavailable, (int)NeedsPlayerReason.ShopperNotReady)]
    [InlineData((int)ShopperRefusal.InvalidRequest, (int)NeedsPlayerReason.ShopperRefused)]
    [InlineData((int)ShopperRefusal.LoggedOut, (int)NeedsPlayerReason.ShopperRefused)]
    [InlineData((int)ShopperRefusal.PricesUnavailable, (int)NeedsPlayerReason.ShopperRefused)]
    public void RefusalsNameWhatThePlayerHasToDo(int refusal, int expected)
    {
        Assert.Equal((NeedsPlayerReason)expected, ShopperSettlementRule.ReasonForRefusal((ShopperRefusal)refusal));
    }

    [Theory]
    [InlineData((int)ShopperRefusal.ConsentRequired, (int)NeedsPlayerReason.ShopperConsent)]
    [InlineData((int)ShopperRefusal.ConsentDenied, (int)NeedsPlayerReason.ShopperConsent)]
    [InlineData((int)ShopperRefusal.JournalDamaged, (int)NeedsPlayerReason.ShopperNotReady)]
    [InlineData((int)ShopperRefusal.InterruptedRun, (int)NeedsPlayerReason.ShopperInterrupted)]
    public void APreflightRefusalIsRoutedLikeAStartRefusal(int refusal, int expected)
    {
        var view = RunView(true, ShopperRunOutcome.Stopped) with { Refusal = (ShopperRefusal)refusal };

        Assert.Equal(ShopperSettlement.NeedsPlayer((NeedsPlayerReason)expected), ShopperSettlementRule.Classify(view, stopRequested: false));
    }

    [Theory]
    [InlineData((int)NeedsPlayerReason.ShopperConsent, true)]
    [InlineData((int)NeedsPlayerReason.ShopperUncertainPurchase, true)]
    [InlineData((int)NeedsPlayerReason.ShopperInterrupted, true)]
    [InlineData((int)NeedsPlayerReason.ShopperNotReady, true)]
    [InlineData((int)NeedsPlayerReason.ShopperStopped, true)]
    [InlineData((int)NeedsPlayerReason.ShopperRefused, false)]
    [InlineData((int)NeedsPlayerReason.HandInPaused, false)]
    [InlineData((int)NeedsPlayerReason.DeliveryUnconfirmed, false)]
    public void OnlyWhatIsSolvedInAutoShopperOffersToOpenIt(int reason, bool owned)
    {
        Assert.Equal(owned, ShopperSettlementRule.OwnedByShopper((NeedsPlayerReason)reason));
    }

    [Fact]
    public void BoughtUnitsFillTheRowsOfALineInOrder()
    {
        RowDecision[] plan =
        [
            ToBuy(SupplyClasses.Culinarian, CulinarianItem, 3, ItemQuality.Normal, 3, 1_250),
            ToBuy(SupplyClasses.Alchemist, CulinarianItem, 2, ItemQuality.Normal, 2, 1_250),
            ToBuy(SupplyClasses.Weaver, CulinarianItem, 1, ItemQuality.High, 1, 4_000),
            Owned(SupplyClasses.Miner, MinerItem, 10),
        ];
        var progress = RunPlanRules.Initial(plan);
        var view = RunView(false, ShopperRunOutcome.None, new ShopperRunLine(CulinarianItem, ShopperQuality.NormalQuality, 5, 4, 1), new ShopperRunLine(CulinarianItem, ShopperQuality.HighQuality, 1, 0, 1));

        var changed = BuyProgress.Apply(plan, progress, view);

        Assert.Equal(3, changed);
        Assert.Equal(new RowProgress(RowRunState.Bought, RowNote.Bought, 3, 0), progress[0]);
        Assert.Equal(new RowProgress(RowRunState.Buying, RowNote.Buying, 1, 0), progress[1]);
        Assert.Equal(new RowProgress(RowRunState.Buying, RowNote.Buying, 0, 0), progress[2]);
        Assert.Equal(RowProgress.Of(RowRunState.Waiting, RowNote.InBags), progress[3]);
        Assert.Equal(0, BuyProgress.Apply(plan, progress, view));
    }

    [Fact]
    public void AnEndedRunLeavesShortRowsNotBoughtWithTheirIssue()
    {
        RowDecision[] plan =
        [
            ToBuy(SupplyClasses.Culinarian, CulinarianItem, 3, ItemQuality.Normal, 3, 1_250),
            ToBuy(SupplyClasses.Miner, MinerItem, 10, ItemQuality.Normal, 10, 263),
            ToBuy(SupplyClasses.Botanist, 5509, 20, ItemQuality.Normal, 20, 50),
        ];
        var progress = RunPlanRules.Initial(plan);
        var view = RunView(
            true,
            ShopperRunOutcome.CompletedPartial,
            new ShopperRunLine(CulinarianItem, ShopperQuality.NormalQuality, 3, 3, 0),
            new ShopperRunLine(MinerItem, ShopperQuality.NormalQuality, 10, 4, 6),
            new ShopperRunLine(5509, ShopperQuality.NormalQuality, 20, 0, 20)) with
        {
            Issues = ImmutableArray.Create(new ShopperRunIssue(ShopperIssueReason.BudgetReached, 0), new ShopperRunIssue(ShopperIssueReason.PriceCapExceeded, MinerItem)),
        };

        BuyProgress.Apply(plan, progress, view);

        Assert.Equal(RowRunState.Bought, progress[0].State);
        Assert.Equal(new RowProgress(RowRunState.NotBought, RowNote.PriceCapExceeded, 4, 0), progress[1]);
        Assert.Equal(new RowProgress(RowRunState.NotBought, RowNote.BudgetReached, 0, 0), progress[2]);
    }

    [Theory]
    [InlineData((int)ShopperIssueReason.NoStock, (int)RowNote.NoStock)]
    [InlineData((int)ShopperIssueReason.InventoryFull, (int)RowNote.InventoryFull)]
    [InlineData((int)ShopperIssueReason.InsufficientGil, (int)RowNote.InsufficientGil)]
    [InlineData((int)ShopperIssueReason.NotMarketable, (int)RowNote.NotMarketable)]
    [InlineData((int)ShopperIssueReason.TravelFailed, (int)RowNote.ShopperIssue)]
    public void IssuesNameWhyARowWasNotBought(int reason, int expected)
    {
        Assert.Equal((RowNote)expected, BuyProgress.NoteFor(ImmutableArray.Create(new ShopperRunIssue((ShopperIssueReason)reason, MinerItem)), MinerItem));
        Assert.Equal(RowNote.ShopperIssue, BuyProgress.NoteFor(ImmutableArray.Create(new ShopperRunIssue((ShopperIssueReason)reason, CulinarianItem)), MinerItem));
    }
}
