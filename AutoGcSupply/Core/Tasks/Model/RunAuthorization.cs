using AutoGcSupply.Core.Character.Model;
using AutoGcSupply.Core.HandIn.Model;
using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Value.Model;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace AutoGcSupply.Core.Tasks.Model;

internal readonly record struct RunAuthorization(
    Guid RunId,
    ulong ContentId,
    uint WorldId,
    DayKey Day,
    DateTime StartedAtUtc,
    long RunCap,
    int TolerancePercent,
    SealCapPolicy SealCapPolicy,
    ImmutableArray<RowDecision> Rows)
{
    private const string ShopperLabelPrefix = "GC supply ";

    public string ShopperLabel => ShopperLabelPrefix + Day;

    public bool AllowsPurchases(DayKey readDay, DateTime nowUtc) => readDay == Day && DayKey.For(nowUtc) == Day;

    public static RunAuthorization Freeze(
        Guid runId,
        in CharacterState character,
        DayKey day,
        DateTime startedAtUtc,
        long runCap,
        int tolerancePercent,
        SealCapPolicy sealCapPolicy,
        ReadOnlySpan<RowDecision> decisions)
    {
        var included = 0;
        for (var index = 0; index < decisions.Length; index++)
        {
            if (decisions[index].Included)
            {
                included++;
            }
        }

        var rows = new RowDecision[included];
        var rowIndex = 0;
        for (var index = 0; index < decisions.Length; index++)
        {
            if (decisions[index].Included)
            {
                rows[rowIndex++] = decisions[index];
            }
        }

        return new RunAuthorization(
            runId,
            character.ContentId,
            character.CurrentWorldId,
            day,
            startedAtUtc,
            runCap,
            tolerancePercent,
            sealCapPolicy,
            ImmutableCollectionsMarshal.AsImmutableArray(rows));
    }
}
