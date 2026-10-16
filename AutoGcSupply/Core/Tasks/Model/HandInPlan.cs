using AutoGcSupply.Core.HandIn.Model;
using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Value.Model;

namespace AutoGcSupply.Core.Tasks.Model;

internal readonly record struct HandInPlan(HandInTarget[] Targets, int[] RowOfTarget)
{
    public static HandInPlan Build(ReadOnlySpan<RowDecision> plan, Span<RowProgress> progress, ReadOnlySpan<OwnedCount> ownedByRow)
    {
        Span<int> targetRows = stackalloc int[plan.Length];
        var count = 0;
        for (var index = 0; index < plan.Length; index++)
        {
            var row = plan[index];
            if (!row.Included || progress[index].State == RowRunState.Delivered)
            {
                continue;
            }

            var free = ownedByRow[index].Of(row.ChosenQuality) - Claimed(plan, targetRows[..count], row);
            if (free >= row.Request.Requested)
            {
                targetRows[count++] = index;
                continue;
            }

            if (progress[index].State != RowRunState.NotBought)
            {
                progress[index] = progress[index].With(RowRunState.Skipped, RowNote.NotInBags);
            }
        }

        var targets = new HandInTarget[count];
        var rowOfTarget = new int[count];
        for (var targetIndex = 0; targetIndex < count; targetIndex++)
        {
            var row = plan[targetRows[targetIndex]];
            rowOfTarget[targetIndex] = targetRows[targetIndex];
            targets[targetIndex] = new HandInTarget(
                row.Request.ItemId,
                row.Request.ClassJobId,
                row.Request.Requested,
                row.ChosenQuality,
                (int)Math.Min(row.Seals, int.MaxValue));
        }

        return new HandInPlan(targets, rowOfTarget);
    }

    private static int Claimed(ReadOnlySpan<RowDecision> plan, ReadOnlySpan<int> targetRows, in RowDecision row)
    {
        var claimed = 0;
        for (var targetIndex = 0; targetIndex < targetRows.Length; targetIndex++)
        {
            var target = plan[targetRows[targetIndex]];
            if (target.Request.ItemId == row.Request.ItemId && target.ChosenQuality == row.ChosenQuality)
            {
                claimed += target.Request.Requested;
            }
        }

        return claimed;
    }
}
