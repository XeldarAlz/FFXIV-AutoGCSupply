using AutoGcSupply.Core.HandIn.Model;

namespace AutoGcSupply.Core.Tasks.Model;

internal static class HandInMirror
{
    public static int Apply(ReadOnlySpan<HandInRowResult> results, ReadOnlySpan<int> rowOfTarget, Span<RowProgress> progress)
    {
        var changed = 0;
        for (var targetIndex = 0; targetIndex < results.Length; targetIndex++)
        {
            var rowIndex = rowOfTarget[targetIndex];
            var next = Next(results[targetIndex], progress[rowIndex]);
            if (next == progress[rowIndex])
            {
                continue;
            }

            progress[rowIndex] = next;
            changed++;
        }

        return changed;
    }

    private static RowProgress Next(in HandInRowResult result, in RowProgress current) => result.Status switch
    {
        HandInRowStatus.Delivering => current.With(RowRunState.HandingIn, RowNote.HandingIn),
        HandInRowStatus.Delivered => current.With(RowRunState.Delivered, RowNote.HandedIn) with { SealsLost = result.SealsLost },
        HandInRowStatus.Skipped => current.With(RowRunState.Skipped, SkipNote(result)),
        HandInRowStatus.Unconfirmed => current.With(RowRunState.Unconfirmed, RowNote.Unconfirmed),
        _ => current.State == RowRunState.HandingIn ? current.With(RowRunState.Waiting, RowNote.InBags) : current,
    };

    private static RowNote SkipNote(in HandInRowResult result)
    {
        if (result.SealCap == SealCapAction.Skip)
        {
            return RowNote.SealCap;
        }

        return result.Match switch
        {
            RowMatchStatus.NotEnoughInBags => RowNote.NotInBags,
            RowMatchStatus.Closed => RowNote.AlreadyHandedIn,
            _ => RowNote.NotOffered,
        };
    }
}
