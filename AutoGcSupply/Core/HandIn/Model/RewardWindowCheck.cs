namespace AutoGcSupply.Core.HandIn.Model;

internal static class RewardWindowCheck
{
    public const int BonusAllowanceNumerator = 5;
    public const int BonusAllowanceDenominator = 2;

    public static RewardWindowVerdict Judge(in RewardWindowSample shown, string expectedItemName, in HandInTarget target, int sealsBefore, int sealCap)
    {
        if (!shown.Readable || PromptText.Normalize(shown.ItemName).Length == 0 || PromptText.Normalize(expectedItemName).Length == 0)
        {
            return RewardWindowVerdict.Unreadable;
        }

        if (!PromptText.Same(shown.ItemName, expectedItemName))
        {
            return RewardWindowVerdict.OtherItem;
        }

        if (target.SealReward == 0)
        {
            return RewardWindowVerdict.Matches;
        }

        return SealsFit(shown.Seals, target.SealReward, sealsBefore, sealCap) ? RewardWindowVerdict.Matches : RewardWindowVerdict.OtherSeals;
    }

    public static bool SealsFit(int shownSeals, int expectedSeals, int sealsBefore, int sealCap)
    {
        var lowest = LowestSeals(expectedSeals, sealsBefore, sealCap);
        var highest = HighestSeals(expectedSeals);
        return shownSeals >= lowest && shownSeals <= highest;
    }

    public static int LowestSeals(int expectedSeals, int sealsBefore, int sealCap)
    {
        if (sealCap == SealCapRule.UnknownCap)
        {
            return expectedSeals;
        }

        var room = Math.Max(0L, (long)sealCap - sealsBefore);
        return (int)Math.Min(expectedSeals, room);
    }

    public static int HighestSeals(int expectedSeals)
    {
        var highest = ((long)expectedSeals * BonusAllowanceNumerator + BonusAllowanceDenominator - 1) / BonusAllowanceDenominator;
        return (int)Math.Min(int.MaxValue, highest);
    }
}
