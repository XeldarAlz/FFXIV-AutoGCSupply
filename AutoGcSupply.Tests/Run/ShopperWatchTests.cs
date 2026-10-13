using AutoGcSupply.Core.Shopper.Model;
using AutoGcSupply.Core.Tasks.Model;
using static AutoGcSupply.Tests.Run.RunFixtures;

namespace AutoGcSupply.Tests.Run;

public sealed class ShopperWatchTests
{
    private const long MinuteMs = 60_000;

    [Fact]
    public void ARunThatKeepsMovingNeverStalls()
    {
        var watch = new ShopperWatch(0);
        for (var minute = 0; minute <= 150; minute += 10)
        {
            watch.Observe(Status(revision: minute), minute * MinuteMs);
        }

        Assert.False(watch.Stalled(155 * MinuteMs));
    }

    [Fact]
    public void TwentyMinutesWithoutAChangeStall()
    {
        var watch = new ShopperWatch(0);
        watch.Observe(Status(revision: 5), 0);
        watch.Observe(Status(revision: 5), 19 * MinuteMs);

        Assert.False(watch.Stalled(20 * MinuteMs - 1));
        Assert.True(watch.Stalled(20 * MinuteMs));
    }

    [Fact]
    public void ANewStateOrPhaseCountsAsMovement()
    {
        var watch = new ShopperWatch(0);
        watch.Observe(Status(revision: 5), 0);
        watch.Observe(Status(revision: 5) with { State = ShopperRunState.Inspecting }, 15 * MinuteMs);
        watch.Observe(Status(revision: 5) with { State = ShopperRunState.Inspecting, Phase = ShopperRunPhase.Finishing }, 30 * MinuteMs);

        Assert.False(watch.Stalled(49 * MinuteMs));
        Assert.True(watch.Stalled(50 * MinuteMs));
    }

    [Fact]
    public void APausedRunDoesNotStallAndCountsFromItsLastPausedReply()
    {
        var watch = new ShopperWatch(0);
        watch.Observe(Status(revision: 5) with { Paused = true }, 0);
        watch.Observe(Status(revision: 5) with { Paused = true }, 60 * MinuteMs);

        Assert.False(watch.Stalled(90 * MinuteMs));

        watch.Observe(Status(revision: 5), 61 * MinuteMs);

        Assert.False(watch.Stalled(80 * MinuteMs - 1));
        Assert.True(watch.Stalled(80 * MinuteMs));
    }

    [Fact]
    public void ThreeHoursEndTheWaitWhateverHappens()
    {
        var watch = new ShopperWatch(10 * MinuteMs);
        watch.Observe(Status(revision: 1) with { Paused = true }, 180 * MinuteMs);

        Assert.False(watch.Overdue(190 * MinuteMs - 1));
        Assert.True(watch.Overdue(190 * MinuteMs));
    }

    private static ShopperRunView Status(long revision)
        => RunView(terminal: false, ShopperRunOutcome.None) with { Revision = revision, State = ShopperRunState.Searching, Phase = ShopperRunPhase.Shopping };
}
