using System.Globalization;

namespace AutoGcSupply.Core.Requests.Model;

public readonly record struct DayKey(int DayNumber)
{
    public static readonly TimeSpan ResetTimeOfDay = TimeSpan.FromHours(20);

    private const string LabelFormat = "yyyy-MM-dd";

    public DateTime StartUtc => DateOnly.FromDayNumber(DayNumber).ToDateTime(TimeOnly.FromTimeSpan(ResetTimeOfDay), DateTimeKind.Utc);

    public DateTime NextResetUtc => StartUtc.AddDays(1);

    public static DayKey For(DateTime utc)
    {
        if (utc.Kind == DateTimeKind.Local)
        {
            throw new ArgumentException("The day key needs a UTC time.", nameof(utc));
        }

        return new DayKey(DateOnly.FromDateTime(utc - ResetTimeOfDay).DayNumber);
    }

    public static TimeSpan UntilReset(DateTime utc) => For(utc).NextResetUtc - utc;

    public override string ToString() => DateOnly.FromDayNumber(DayNumber).ToString(LabelFormat, CultureInfo.InvariantCulture);
}
