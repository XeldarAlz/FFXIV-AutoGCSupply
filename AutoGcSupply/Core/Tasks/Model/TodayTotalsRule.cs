using AutoGcSupply.Core.Shared;
using AutoGcSupply.Core.Value.Model;

namespace AutoGcSupply.Core.Tasks.Model;

internal static class TodayTotalsRule
{
    private const uint UnknownSealCap = 0;

    public static TodayTotals Sum(ReadOnlySpan<TodayRow> rows, uint seals, uint sealCap)
    {
        var selected = 0;
        var toBuy = 0;
        var toHandIn = 0;
        var spend = 0L;
        var experience = 0L;
        var sealTotal = 0L;
        for (var index = 0; index < rows.Length; index++)
        {
            var decision = rows[index].Decision;
            if (!decision.Included)
            {
                continue;
            }

            selected++;
            if (!decision.NeedsPurchase && !decision.ReadyToHandIn)
            {
                continue;
            }

            toHandIn++;
            if (decision.NeedsPurchase)
            {
                toBuy++;
                spend = Gil.SaturatingAdd(spend, decision.EstimatedTotal);
            }

            experience = Gil.SaturatingAdd(experience, decision.Experience);
            sealTotal = Gil.SaturatingAdd(sealTotal, decision.Seals);
        }

        var sealsLost = sealCap == UnknownSealCap ? 0 : RewardMath.SealsLost(seals, sealCap, sealTotal);
        return new TodayTotals(selected, toBuy, toHandIn, spend, experience, sealTotal, sealsLost);
    }
}
