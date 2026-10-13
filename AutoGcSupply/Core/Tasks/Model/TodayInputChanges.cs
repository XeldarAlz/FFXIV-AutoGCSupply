using AutoGcSupply.Core.Character.Model;
using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Requests.Model;

namespace AutoGcSupply.Core.Tasks.Model;

internal static class TodayInputChanges
{
    public static bool SameCharacter(in CharacterState left, in CharacterState right)
        => left with { Classes = default } == right with { Classes = default }
        && left.Classes.AsSpan().SequenceEqual(right.Classes.AsSpan());

    public static bool SameRequests(in RequestSnapshot left, in RequestSnapshot right)
        => left.ContentId == right.ContentId
        && left.Day == right.Day
        && left.LoadState == right.LoadState
        && left.Requests.AsSpan().SequenceEqual(right.Requests.AsSpan());

    public static bool SameCounts(ReadOnlySpan<OwnedCount> left, ReadOnlySpan<OwnedCount> right) => left.SequenceEqual(right);
}
