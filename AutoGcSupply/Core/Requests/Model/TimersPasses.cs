namespace AutoGcSupply.Core.Requests.Model;

public sealed class TimersPasses
{
    private const ulong NoCharacter = 0;

    private readonly Dictionary<ulong, DayKey> lastPassDays = [];

    public void Record(ulong contentId, DayKey day)
    {
        if (contentId == NoCharacter)
        {
            return;
        }

        lastPassDays[contentId] = day;
    }

    public bool IsCurrent(ulong contentId, DayKey day)
        => contentId != NoCharacter && lastPassDays.TryGetValue(contentId, out var passedOn) && passedOn == day;
}
