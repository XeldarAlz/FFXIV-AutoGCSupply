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

        Assert.Equal(new RunVerdict(RunEnd.Completed, NeedsPlayerReason.None), RunOutcomeRule.AfterHandIn(Result(HandInOutcome.Completed), progress));
    }

    [Fact]
    public void ARowLeftBehindMakesTheRunPartial()
    {
        var progress = Progress(RowRunState.Delivered, RowRunState.NotBought, RowRunState.Skipped);

        Assert.Equal(new RunVerdict(RunEnd.CompletedPartial, NeedsPlayerReason.None), RunOutcomeRule.AfterHandIn(Result(HandInOutcome.Partial), progress));
    }

    [Fact]
    public void APausedHandInNeedsThePlayer()
    {
        var result = Result(HandInOutcome.Paused) with { PauseReason = HandInPauseReason.UnexpectedPrompt, Detail = "Trade?" };

        Assert.Equal(RunVerdict.NeedsPlayer(NeedsPlayerReason.HandInPaused), RunOutcomeRule.AfterHandIn(result, Progress(RowRunState.Delivered, RowRunState.Waiting)));
    }

    [Fact]
    public void AnUnconfirmedDeliveryOutweighsThePause()
    {
        var unconfirmedRow = Progress(RowRunState.Delivered, RowRunState.Unconfirmed);
        var notConfirmed = Result(HandInOutcome.Paused) with { PauseReason = HandInPauseReason.DeliveryNotConfirmed };

        Assert.Equal(RunVerdict.NeedsPlayer(NeedsPlayerReason.DeliveryUnconfirmed), RunOutcomeRule.AfterHandIn(Result(HandInOutcome.Paused), unconfirmedRow));
        Assert.Equal(RunVerdict.NeedsPlayer(NeedsPlayerReason.DeliveryUnconfirmed), RunOutcomeRule.AfterHandIn(notConfirmed, Progress(RowRunState.Waiting)));
    }

    [Fact]
    public void ACancelledHandInIsAStoppedRun()
    {
        Assert.Equal(RunEnd.Stopped, RunOutcomeRule.AfterHandIn(Result(HandInOutcome.Cancelled), Progress(RowRunState.Waiting)).End);
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
            new(RowRunState.NotBought, RowNote.NoStock, 0, 0),
            new(RowRunState.Skipped, RowNote.NotInBags, 0, 0),
        ];
        var duration = TimeSpan.FromMinutes(12);

        var summary = RunOutcomeRule.Summarize(authorized, progress, 8_000, duration);

        Assert.Equal(new RunSummary(1, 3, 360_000, 840, 140, 8_000, duration), summary);
    }

    private static HandInResult Result(HandInOutcome outcome) => new(outcome, HandInPauseReason.None, string.Empty, RewardWindowMismatch.None, [], 0, 0, 0);
}
