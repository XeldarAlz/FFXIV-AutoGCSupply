using AutoGcSupply.Core.Requests.Model;

namespace AutoGcSupply.Tests;

public sealed class DayKeyTests
{
    private static readonly DateTime ResetMoment = new(2026, 10, 13, 20, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void RollsOverAtExactlyTwentyHundredUtc()
    {
        var before = DayKey.For(ResetMoment.AddTicks(-1));
        var at = DayKey.For(ResetMoment);

        Assert.NotEqual(before, at);
        Assert.Equal(before.DayNumber + 1, at.DayNumber);
    }

    [Fact]
    public void DayLastsUntilTheNextReset()
    {
        var at = DayKey.For(ResetMoment);

        Assert.Equal(at, DayKey.For(ResetMoment.AddHours(4)));
        Assert.Equal(at, DayKey.For(ResetMoment.AddDays(1).AddTicks(-1)));
        Assert.NotEqual(at, DayKey.For(ResetMoment.AddDays(1)));
    }

    [Fact]
    public void MorningBelongsToThePreviousEveningsDay()
    {
        var morning = DayKey.For(new DateTime(2026, 10, 14, 8, 0, 0, DateTimeKind.Utc));

        Assert.Equal(DayKey.For(ResetMoment), morning);
        Assert.Equal("2026-10-13", morning.ToString());
    }

    [Fact]
    public void StartAndNextResetBracketTheDay()
    {
        var day = DayKey.For(new DateTime(2026, 10, 14, 3, 30, 0, DateTimeKind.Utc));

        Assert.Equal(ResetMoment, day.StartUtc);
        Assert.Equal(ResetMoment.AddDays(1), day.NextResetUtc);
        Assert.Equal(DateTimeKind.Utc, day.StartUtc.Kind);
    }

    [Fact]
    public void ResetMomentPointsAtTheFollowingReset()
    {
        Assert.Equal(ResetMoment.AddDays(1), DayKey.For(ResetMoment).NextResetUtc);
        Assert.Equal(ResetMoment, DayKey.For(ResetMoment.AddTicks(-1)).NextResetUtc);
    }

    [Fact]
    public void UntilResetCountsDownToTwentyHundred()
    {
        Assert.Equal(TimeSpan.FromMinutes(1), DayKey.UntilReset(ResetMoment.AddMinutes(-1)));
        Assert.Equal(TimeSpan.FromDays(1), DayKey.UntilReset(ResetMoment));
        Assert.Equal(TimeSpan.FromTicks(1), DayKey.UntilReset(ResetMoment.AddTicks(-1)));
    }

    [Fact]
    public void CrossesYearAndLeapDayBoundaries()
    {
        var newYearsEve = DayKey.For(new DateTime(2027, 12, 31, 21, 0, 0, DateTimeKind.Utc));
        var newYearsMorning = DayKey.For(new DateTime(2028, 1, 1, 19, 59, 59, DateTimeKind.Utc));
        var leapDay = DayKey.For(new DateTime(2028, 2, 29, 20, 0, 0, DateTimeKind.Utc));
        var dayAfterLeap = DayKey.For(new DateTime(2028, 3, 1, 20, 0, 0, DateTimeKind.Utc));

        Assert.Equal(newYearsEve, newYearsMorning);
        Assert.Equal("2027-12-31", newYearsEve.ToString());
        Assert.Equal(leapDay.DayNumber + 1, dayAfterLeap.DayNumber);
        Assert.Equal("2028-02-29", leapDay.ToString());
    }

    [Fact]
    public void UnspecifiedKindIsReadAsUtc()
    {
        var unspecified = new DateTime(2026, 10, 13, 20, 0, 0, DateTimeKind.Unspecified);

        Assert.Equal(DayKey.For(ResetMoment), DayKey.For(unspecified));
    }

    [Fact]
    public void RefusesLocalTime()
    {
        Assert.Throws<ArgumentException>(() => DayKey.For(new DateTime(2026, 10, 13, 20, 0, 0, DateTimeKind.Local)));
    }
}
