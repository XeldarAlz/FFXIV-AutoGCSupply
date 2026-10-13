using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Value.Model;

namespace AutoGcSupply.Core.HandIn.Model;

internal static class RewardWindowCheck
{
    public const int ItemNameIndex = 4;
    public const int SealRewardIndex = 9;
    public const int BonusAllowanceNumerator = 5;
    public const int BonusAllowanceDenominator = 2;

    private const int NoText = -1;
    private const string AnyText = "";

    public static RewardWindowJudgment Judge(in RewardWindowSample shown, string expectedItemName, in HandInTarget target, int sealsBefore, int sealCap)
    {
        var expected = PromptText.Normalize(expectedItemName);
        if (!shown.Readable || expected.Length == 0)
        {
            return RewardWindowJudgment.Refuses(RewardWindowVerdict.Unreadable);
        }

        if (FindText(shown.Texts, expected) == NoText)
        {
            return RewardWindowJudgment.Refuses(FindText(shown.Texts, AnyText) == NoText ? RewardWindowVerdict.Unreadable : RewardWindowVerdict.OtherItem);
        }

        return target.SealReward == 0 ? RewardWindowJudgment.Matches : JudgeSeals(shown, target, sealsBefore, sealCap);
    }

    public static RewardWindowMismatch Mismatch(RewardWindowVerdict verdict, in RewardWindowSample shown, string expectedItemName, in HandInTarget target, int sealsBefore, int sealCap)
    {
        var expected = PromptText.Normalize(expectedItemName);
        return new RewardWindowMismatch(
            verdict,
            ShownItemName(shown.Texts, expected),
            shown.TryFindInteger(SealRewardIndex, out var shownSeals) ? shownSeals : 0,
            expected,
            LowestSeals(target.SealReward, sealsBefore, sealCap),
            HighestSeals(target.SealReward));
    }

    public static bool CapHidesQuality(in HandInTarget target, int sealsBefore, int sealCap)
        => target.Quality == ItemQuality.High
        && target.SealReward > 0
        && LowestSeals(target.SealReward, sealsBefore, sealCap) <= NormalQualitySeals(target.SealReward);

    public static int NormalQualitySeals(int highQualitySeals) => highQualitySeals / RewardMath.HighQualitySupplyFactor;

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

    private static RewardWindowJudgment JudgeSeals(in RewardWindowSample shown, in HandInTarget target, int sealsBefore, int sealCap)
    {
        if (!shown.TryFindInteger(SealRewardIndex, out var shownSeals))
        {
            return RewardWindowJudgment.SealsNotLocated;
        }

        if (SealsFit(shownSeals, target.SealReward, sealsBefore, sealCap))
        {
            return RewardWindowJudgment.Matches;
        }

        return target.Quality == ItemQuality.High && LooksLikeNormalQuality(shownSeals, LowestSeals(target.SealReward, sealsBefore, sealCap))
            ? RewardWindowJudgment.Refuses(RewardWindowVerdict.OtherSeals)
            : RewardWindowJudgment.SealsNotLocated;
    }

    private static bool LooksLikeNormalQuality(int shownSeals, int lowestSeals)
        => shownSeals < lowestSeals && shownSeals >= LowestNormalQualitySeals(lowestSeals);

    private static int LowestNormalQualitySeals(int lowestSeals)
        => (int)(((long)lowestSeals + RewardMath.HighQualitySupplyFactor - 1) / RewardMath.HighQualitySupplyFactor);

    private static string ShownItemName(RewardWindowText[] texts, string expected)
    {
        var textPosition = FindText(texts, expected);
        if (textPosition == NoText)
        {
            textPosition = FindText(texts, AnyText);
        }

        return textPosition == NoText ? string.Empty : PromptText.Normalize(texts[textPosition].Text);
    }

    private static int FindText(RewardWindowText[] texts, string phrase)
    {
        var firstFound = NoText;
        for (var textPosition = 0; textPosition < texts.Length; textPosition++)
        {
            var text = PromptText.Normalize(texts[textPosition].Text);
            if (text.Length == 0 || !ContainsPhrase(text, phrase))
            {
                continue;
            }

            if (texts[textPosition].ValueIndex == ItemNameIndex)
            {
                return textPosition;
            }

            if (firstFound == NoText)
            {
                firstFound = textPosition;
            }
        }

        return firstFound;
    }

    private static bool ContainsPhrase(string text, string phrase)
    {
        if (phrase.Length == 0)
        {
            return true;
        }

        for (var start = text.IndexOf(phrase, StringComparison.OrdinalIgnoreCase); start >= 0; start = text.IndexOf(phrase, start + 1, StringComparison.OrdinalIgnoreCase))
        {
            if (SeparatesWords(text, start - 1) && SeparatesWords(text, start + phrase.Length))
            {
                return true;
            }
        }

        return false;
    }

    private static bool SeparatesWords(string text, int characterIndex)
        => characterIndex < 0 || characterIndex >= text.Length || !char.IsLetterOrDigit(text[characterIndex]);
}
