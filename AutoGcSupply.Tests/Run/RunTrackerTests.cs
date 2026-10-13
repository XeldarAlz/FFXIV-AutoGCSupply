using AutoGcSupply.Core.Tasks.Model;

namespace AutoGcSupply.Tests.Run;

public sealed class RunTrackerTests
{
    [Fact]
    public void TheModelIsRebuiltOnlyWhenSomethingChanged()
    {
        var tracker = new RunTracker();
        tracker.Begin(new RunRow[2], "Reading");
        var first = tracker.Current;

        tracker.SetStep(RunStep.Reading, "Reading");
        tracker.SetShopper(string.Empty, 0);
        tracker.SetDryRun(false);
        tracker.SetRow(0, default);

        Assert.Same(first, tracker.Current);

        tracker.SetRow(1, new RunRow(16, RowRunState.Buying, "0 of 10 bought"));
        var second = tracker.Current;

        Assert.NotSame(first, second);
        Assert.True(second.Revision > first.Revision);
        Assert.Equal(new RunRow(16, RowRunState.Buying, "0 of 10 bought"), second.Rows[1]);
        Assert.Equal(default, first.Rows[1]);
    }

    [Fact]
    public void FinishingKeepsTheOutcomeUntilReset()
    {
        var tracker = new RunTracker();
        tracker.Begin(new RunRow[1], "Reading");
        tracker.NeedPlayer(NeedsPlayerReason.ShopperConsent, "Allow it");
        var summary = new RunSummary(0, 1, 0, 0, 0, 0, false, TimeSpan.FromSeconds(5), false);

        tracker.SetStep(RunStep.Buying, "Buying");
        tracker.Finish(RunEnd.NeedsPlayer, summary, "Waiting for you");
        var finished = tracker.Current;

        Assert.Equal(RunStep.Done, finished.Step);
        Assert.Equal(RunStep.Buying, finished.LastStep);
        Assert.Equal(RunEnd.NeedsPlayer, finished.End);
        Assert.Equal(NeedsPlayerReason.ShopperConsent, finished.NeedsPlayer);
        Assert.Equal("Allow it", finished.NeedsPlayerDetail);
        Assert.Equal(summary, finished.Summary);

        tracker.Reset();
        var idle = tracker.Current;

        Assert.Equal(RunStep.Idle, idle.Step);
        Assert.Equal(RunStep.Idle, idle.LastStep);
        Assert.Equal(RunEnd.None, idle.End);
        Assert.Empty(idle.Rows);
        Assert.True(idle.Revision > finished.Revision);
    }

    [Fact]
    public void TheLastStepFollowsTheRunUntilItEnds()
    {
        var tracker = new RunTracker();
        tracker.Begin(new RunRow[1], "Reading");

        Assert.Equal(RunStep.Reading, tracker.Current.LastStep);

        tracker.SetStep(RunStep.HandingIn, "Handing in");
        tracker.Finish(RunEnd.Failed, default, "Failed");

        Assert.Equal(RunStep.HandingIn, tracker.Current.LastStep);
        Assert.Equal(RunStep.Done, tracker.Current.Step);
    }
}
