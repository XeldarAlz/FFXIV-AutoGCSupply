using AutoGcSupply.Core.Requests.Model;

namespace AutoGcSupply.Tests;

public sealed class TimersPassesTests
{
    private const ulong OtherContentId = 0x0040_0000_0000_0002;

    private static readonly DayKey today = DayKey.For(SupplyFixtures.ReadAtUtc);
    private static readonly DayKey yesterday = new(today.DayNumber - 1);

    [Fact]
    public void NoPassMeansNothingIsCurrent()
    {
        Assert.False(new TimersPasses().IsCurrent(SupplyFixtures.ContentId, today));
    }

    [Fact]
    public void APassTodayMakesThatCharacterCurrentForToday()
    {
        var passes = new TimersPasses();
        passes.Record(SupplyFixtures.ContentId, today);

        Assert.True(passes.IsCurrent(SupplyFixtures.ContentId, today));
        Assert.False(passes.IsCurrent(OtherContentId, today));
    }

    [Fact]
    public void YesterdaysPassIsNotCurrentAfterTheReset()
    {
        var passes = new TimersPasses();
        passes.Record(SupplyFixtures.ContentId, yesterday);

        Assert.False(passes.IsCurrent(SupplyFixtures.ContentId, today));

        passes.Record(SupplyFixtures.ContentId, today);

        Assert.True(passes.IsCurrent(SupplyFixtures.ContentId, today));
    }

    [Fact]
    public void ALoggedOutReadIsNeverCurrent()
    {
        var passes = new TimersPasses();
        passes.Record(0, today);

        Assert.False(passes.IsCurrent(0, today));
    }
}
