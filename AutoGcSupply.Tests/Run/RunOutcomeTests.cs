using AutoGcSupply.Core.HandIn.Model;
using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Core.Value.Model;
using static AutoGcSupply.Tests.Run.RunFixtures;

namespace AutoGcSupply.Tests.Run;

public sealed class RunOutcomeTests
{
    private static readonly int[] firstThreeRows = [0, 1, 2];
    private static readonly TimeSpan historyDuration = TimeSpan.FromMinutes(9);
    private static readonly RowDecision[] historyRows =
    [
        Owned(SupplyClasses.Carpenter, CarpenterItem, 1),
        Owned(SupplyClasses.Culinarian, CulinarianItem, 3),
    ];

    [Fact]
    public void HandInResultsTickTheRowsTheyBelongTo()
    {
        var progress = RunPlanRules.Initial([Owned(SupplyClasses.Carpenter, CarpenterItem, 1), Owned(SupplyClasses.Culinarian, CulinarianItem, 3), Owned(SupplyClasses.Miner, MinerItem, 10), Owned(SupplyClasses.Botanist, 5509, 20)]);
        HandInRowResult[] results =
        [
            new(HandInRowStatus.Delivered, RowMatchStatus.Matched, SealCapAction.DeliverWithLoss, 300, 120),
            HandInRowResult.Pending with { Status = HandInRowStatus.Delivering },
            new(HandInRowStatus.Skipped, RowMatchStatus.Matched, SealCapAction.Skip, 0, 0),
        ];

        var changed = HandInMirror.Apply(results, firstThreeRows, progress);

        Assert.Equal(3, changed);
        Assert.Equal(new RowProgress(RowRunState.Delivered, RowNote.HandedIn, 0, 120), progress[0]);
        Assert.Equal(RowProgress.Of(RowRunState.HandingIn, RowNote.HandingIn), progress[1]);
        Assert.Equal(RowProgress.Of(RowRunState.Skipped, RowNote.SealCap), progress[2]);
        Assert.Equal(RowProgress.Of(RowRunState.Waiting, RowNote.InBags), progress[3]);
    }

    [Theory]
    [InlineData((int)RowMatchStatus.NotEnoughInBags, (int)RowNote.NotInBags)]
    [InlineData((int)RowMatchStatus.Closed, (int)RowNote.AlreadyHandedIn)]
    [InlineData((int)RowMatchStatus.NotListed, (int)RowNote.NotOffered)]
    [InlineData((int)RowMatchStatus.RequestChanged, (int)RowNote.NotOffered)]
    public void SkippedRowsSayWhy(int match, int expected)
    {
        var progress = new[] { RowProgress.Of(RowRunState.Waiting, RowNote.InBags) };

        HandInMirror.Apply([new HandInRowResult(HandInRowStatus.Skipped, (RowMatchStatus)match, SealCapAction.Deliver, 0, 0)], [0], progress);

        Assert.Equal(RowProgress.Of(RowRunState.Skipped, (RowNote)expected), progress[0]);
    }

    [Fact]
    public void ARowBackToPendingLeavesTheHandInState()
    {
        var progress = new[] { RowProgress.Of(RowRunState.HandingIn, RowNote.HandingIn), RowProgress.Of(RowRunState.Bought, RowNote.Bought) };

        HandInMirror.Apply([HandInRowResult.Pending, HandInRowResult.Pending], [0, 1], progress);

        Assert.Equal(RowProgress.Of(RowRunState.Waiting, RowNote.InBags), progress[0]);
        Assert.Equal(RowProgress.Of(RowRunState.Bought, RowNote.Bought), progress[1]);
    }

    [Fact]
    public void EveryRowDeliveredCompletesTheRun()
    {
        var progress = Progress(RowRunState.Delivered, RowRunState.Delivered);

        Assert.Equal(new RunVerdict(RunEnd.Completed, NeedsPlayerReason.None), RunOutcomeRule.AfterHandIn(Result(HandInOutcome.Completed), progress, dryRun: false));
    }

    [Fact]
    public void ARowLeftBehindMakesTheRunPartial()
    {
        var progress = Progress(RowRunState.Delivered, RowRunState.NotBought, RowRunState.Skipped);

        Assert.Equal(new RunVerdict(RunEnd.CompletedPartial, NeedsPlayerReason.None), RunOutcomeRule.AfterHandIn(Result(HandInOutcome.Partial), progress, dryRun: false));
    }

    [Fact]
    public void WouldDeliverCountsOnlyInADryRun()
    {
        var progress = Progress(RowRunState.WouldDeliver, RowRunState.Delivered);

        Assert.Equal(RunEnd.Completed, RunOutcomeRule.Completion(progress, dryRun: true).End);
        Assert.Equal(RunEnd.CompletedPartial, RunOutcomeRule.Completion(progress, dryRun: false).End);
    }

    [Fact]
    public void APausedHandInNeedsThePlayer()
    {
        var result = Result(HandInOutcome.Paused) with { PauseReason = HandInPauseReason.UnexpectedPrompt, Detail = "Trade?" };

        Assert.Equal(RunVerdict.NeedsPlayer(NeedsPlayerReason.HandInPaused), RunOutcomeRule.AfterHandIn(result, Progress(RowRunState.Delivered, RowRunState.Waiting), dryRun: false));
    }

    [Fact]
    public void AnUnconfirmedDeliveryOutweighsThePause()
    {
        var unconfirmedRow = Progress(RowRunState.Delivered, RowRunState.Unconfirmed);
        var notConfirmed = Result(HandInOutcome.Paused) with { PauseReason = HandInPauseReason.DeliveryNotConfirmed };

        Assert.Equal(RunVerdict.NeedsPlayer(NeedsPlayerReason.DeliveryUnconfirmed), RunOutcomeRule.AfterHandIn(Result(HandInOutcome.Paused), unconfirmedRow, dryRun: false));
        Assert.Equal(RunVerdict.NeedsPlayer(NeedsPlayerReason.DeliveryUnconfirmed), RunOutcomeRule.AfterHandIn(notConfirmed, Progress(RowRunState.Waiting), dryRun: false));
    }

    [Fact]
    public void ACancelledHandInIsAStoppedRun()
    {
        Assert.Equal(RunEnd.Stopped, RunOutcomeRule.AfterHandIn(Result(HandInOutcome.Cancelled), Progress(RowRunState.Waiting), dryRun: false).End);
    }

    [Fact]
    public void TheSummaryAddsUpTheRowsThatCounted()
    {
        RowDecision[] authorized =
        [
            ToBuy(SupplyClasses.Carpenter, CarpenterItem, 1, ItemQuality.High, 1, 10_000, seals: 840, experience: 360_000),
            Owned(SupplyClasses.Culinarian, CulinarianItem, 3, seals: 410, experience: 175_000),
            Owned(SupplyClasses.Miner, MinerItem, 10, seals: 300, experience: 90_000),
        ];
        RowProgress[] progress =
        [
            new(RowRunState.Delivered, RowNote.HandedIn, 1, 140),
            new(RowRunState.WouldDeliver, RowNote.WouldHandIn, 0, 0),
            new(RowRunState.Skipped, RowNote.NotInBags, 0, 0),
        ];
        var duration = TimeSpan.FromMinutes(12);

        var real = RunOutcomeRule.Summarize(authorized, progress, 8_000, drySpend: false, duration, dryRun: false);
        var dry = RunOutcomeRule.Summarize(authorized, progress, 8_000, drySpend: true, duration, dryRun: true);

        Assert.Equal(new RunSummary(1, 3, 360_000, 840, 140, 8_000, false, duration, false), real);
        Assert.Equal(new RunSummary(2, 3, 535_000, 1_250, 140, 8_000, true, duration, true), dry);
    }

    [Fact]
    public void HistoryKeepsARealRunAsItWas()
    {
        var shown = RunOutcomeRule.Summarize(historyRows, Progress(RowRunState.Delivered, RowRunState.Skipped), 8_000, drySpend: false, historyDuration, dryRun: false);

        Assert.Equal(shown, RunOutcomeRule.ForHistory(historyRows, Progress(RowRunState.Delivered, RowRunState.Skipped), shown));
    }

    [Fact]
    public void HistoryKeepsARunThatDidNothingRealAsDry()
    {
        var progress = Progress(RowRunState.WouldDeliver, RowRunState.WouldDeliver);
        var shown = RunOutcomeRule.Summarize(historyRows, progress, 8_000, drySpend: true, historyDuration, dryRun: true);

        var recorded = RunOutcomeRule.ForHistory(historyRows, progress, shown);

        Assert.True(recorded.DryRun);
        Assert.Equal(8_000, recorded.GilSpent);
        Assert.Equal(2, recorded.Delivered);
    }

    [Fact]
    public void RealGilSpentBeforeADryHandInIsRecordedAsReal()
    {
        var progress = Progress(RowRunState.WouldDeliver, RowRunState.WouldDeliver);
        var shown = RunOutcomeRule.Summarize(historyRows, progress, 8_000, drySpend: false, historyDuration, dryRun: true);

        var recorded = RunOutcomeRule.ForHistory(historyRows, progress, shown);

        Assert.Equal(new RunSummary(0, 2, 0, 0, 0, 8_000, false, historyDuration, false), recorded);
    }

    [Fact]
    public void ARealDeliveryBeforeADryHandInIsRecordedAsReal()
    {
        var progress = Progress(RowRunState.Delivered, RowRunState.WouldDeliver);
        var shown = RunOutcomeRule.Summarize(historyRows, progress, 0, drySpend: false, historyDuration, dryRun: true);

        var recorded = RunOutcomeRule.ForHistory(historyRows, progress, shown);

        Assert.False(recorded.DryRun);
        Assert.Equal(1, recorded.Delivered);
        Assert.Equal(400, recorded.Seals);
        Assert.Equal(0, recorded.GilSpent);
    }

    private static HandInResult Result(HandInOutcome outcome) => new(outcome, HandInPauseReason.None, string.Empty, RewardWindowMismatch.None, [], 0, 0, 0);
}
