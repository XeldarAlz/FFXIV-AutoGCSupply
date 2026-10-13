using AutoGcSupply.Core.HandIn.Model;
using AutoGcSupply.Core.Inventory.Model;

namespace AutoGcSupply.Tests.HandIn;

public sealed class RewardWindowCheckTests
{
    private const string MapleLumber = "Maple Lumber";
    private const int NormalSeals = 300;
    private const int HighSeals = 600;
    private const int NoSealsYet = 0;
    private const int RankCap = 90_000;
    private const int OtherTextIndex = 6;
    private const int ExperienceIndex = 10;
    private const int Experience = 46_350;
    private const int UnreadableInteger = -1;

    private static readonly HandInTarget highQualityRow = new(5_361, 8, 1, ItemQuality.High, HighSeals);
    private static readonly HandInTarget normalQualityRow = new(5_361, 8, 1, ItemQuality.Normal, NormalSeals);

    [Fact]
    public void BaseSealsRequireIndependentHighQualityVerification()
    {
        var shown = Window(MapleLumber, NormalSeals);
        Assert.Equal(RewardWindowVerdict.OtherSeals, RewardWindowCheck.Judge(shown, MapleLumber, highQualityRow, NoSealsYet, RankCap, qualityVerified: false).Verdict);
        var judgment = RewardWindowCheck.Judge(shown, MapleLumber, highQualityRow, NoSealsYet, RankCap, qualityVerified: true);
        Assert.Equal(RewardWindowVerdict.Matches, judgment.Verdict);
        Assert.True(judgment.BaseSealsShown);
        Assert.False(judgment.SealsUnverified);
    }

    [Fact]
    public void VerifiedQualityNeverAllowsTheWrongItem()
    {
        var judgment = RewardWindowCheck.Judge(Window("Spruce Lumber", NormalSeals), MapleLumber, highQualityRow, NoSealsYet, RankCap, qualityVerified: true);
        Assert.Equal(RewardWindowVerdict.OtherItem, judgment.Verdict);
    }

    [Fact]
    public void MatchesAWindowForTheRowsItemAndSeals()
    {
        Assert.Equal(RewardWindowJudgment.Matches, JudgeWithoutCap(Window(MapleLumber, HighSeals), highQualityRow));
    }

    [Theory]
    [InlineData("maple lumber")]
    [InlineData("  Maple\nLumber ")]
    [InlineData("Ma­ple Lumber")]
    [InlineData("Maple Lumber ")]
    public void MatchesTheItemNameAcrossLayoutAndGlyphs(string shownName)
    {
        Assert.Equal(RewardWindowJudgment.Matches, JudgeWithoutCap(Window(shownName, NormalSeals), normalQualityRow));
    }

    [Theory]
    [InlineData("Maple Lumber x3")]
    [InlineData("Maple Lumber ×3")]
    [InlineData("3 Maple Lumber")]
    [InlineData("Maple Lumber (3)")]
    [InlineData("Maple Lumber")]
    [InlineData("Maple Lumber x3")]
    public void MatchesTheItemNameWithAQuantityOrAGlyphAroundIt(string shownName)
    {
        Assert.Equal(RewardWindowJudgment.Matches, JudgeWithoutCap(Window(shownName, HighSeals), highQualityRow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(7)]
    [InlineData(31)]
    public void FindsTheItemNameAtAnotherIndex(int valueIndex)
    {
        var shown = Window([new RewardWindowText(valueIndex, MapleLumber)], [new RewardWindowInteger(RewardWindowCheck.SealRewardIndex, HighSeals)]);

        Assert.Equal(RewardWindowJudgment.Matches, JudgeWithoutCap(shown, highQualityRow));
    }

    [Fact]
    public void FindsTheItemNameWhenTheNameIndexHoldsOtherText()
    {
        var shown = Window(
            [new RewardWindowText(RewardWindowCheck.ItemNameIndex, "Deliver"), new RewardWindowText(OtherTextIndex, MapleLumber)],
            [new RewardWindowInteger(RewardWindowCheck.SealRewardIndex, HighSeals)]);

        Assert.Equal(RewardWindowJudgment.Matches, JudgeWithoutCap(shown, highQualityRow));
    }

    [Theory]
    [InlineData("Maple Lumberjack")]
    [InlineData("Redmaple Lumber")]
    [InlineData("Maple Lumber2")]
    [InlineData("Maple")]
    public void RefusesATextThatHoldsTheItemNameOnlyInsideOtherWords(string shownName)
    {
        Assert.Equal(RewardWindowVerdict.OtherItem, JudgeWithoutCap(Window(shownName, HighSeals), highQualityRow).Verdict);
    }

    [Fact]
    public void RefusesAWindowForAnotherItem()
    {
        Assert.Equal(RewardWindowJudgment.Refuses(RewardWindowVerdict.OtherItem), JudgeWithoutCap(Window("Spruce Lumber", HighSeals), highQualityRow));
    }

    [Fact]
    public void RefusesAWindowWhereNoTextShowsTheItem()
    {
        var shown = Window(
            [new RewardWindowText(1, "Grand Company Seals"), new RewardWindowText(RewardWindowCheck.ItemNameIndex, "Spruce Lumber"), new RewardWindowText(OtherTextIndex, "Deliver")],
            [new RewardWindowInteger(RewardWindowCheck.SealRewardIndex, HighSeals)]);

        Assert.Equal(RewardWindowVerdict.OtherItem, JudgeWithoutCap(shown, highQualityRow).Verdict);
    }

    [Fact]
    public void ReportsTheOtherItemBeforeTheSeals()
    {
        Assert.Equal(RewardWindowVerdict.OtherItem, JudgeWithoutCap(Window("Spruce Lumber", NormalSeals), highQualityRow).Verdict);
    }

    [Fact]
    public void ReportsTheOtherItemWhenTheSealsCannotBeLocated()
    {
        var shown = Window([new RewardWindowText(RewardWindowCheck.ItemNameIndex, "Spruce Lumber")], []);

        Assert.Equal(RewardWindowJudgment.Refuses(RewardWindowVerdict.OtherItem), JudgeWithoutCap(shown, highQualityRow));
    }

    [Theory]
    [InlineData(NormalSeals)]
    [InlineData(450)]
    [InlineData(HighSeals - 1)]
    public void RefusesNormalQualitySealsForAHighQualityRow(int shownSeals)
    {
        Assert.Equal(RewardWindowJudgment.Refuses(RewardWindowVerdict.OtherSeals), JudgeWithoutCap(Window(MapleLumber, shownSeals), highQualityRow));
    }

    [Theory]
    [InlineData(HighSeals)]
    [InlineData(690)]
    [InlineData(1_200)]
    [InlineData(1_380)]
    [InlineData(1_500)]
    public void AcceptsTheStarAndSealBonusesOnTopOfTheExpectedSeals(int shownSeals)
    {
        Assert.Equal(RewardWindowJudgment.Matches, JudgeWithoutCap(Window(MapleLumber, shownSeals), highQualityRow));
    }

    [Theory]
    [InlineData(NormalSeals)]
    [InlineData(450)]
    [InlineData(750)]
    public void AcceptsAnySealsInTheRangeOfANormalQualityRow(int shownSeals)
    {
        Assert.Equal(RewardWindowJudgment.Matches, JudgeWithoutCap(Window(MapleLumber, shownSeals), normalQualityRow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(150)]
    [InlineData(751)]
    [InlineData(Experience)]
    public void LeavesSealsOutsideTheRangeOfANormalQualityRowUnverified(int shownSeals)
    {
        Assert.Equal(RewardWindowJudgment.SealsNotLocated, JudgeWithoutCap(Window(MapleLumber, shownSeals), normalQualityRow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(NormalSeals - 1)]
    [InlineData(1_501)]
    [InlineData(Experience)]
    [InlineData(108_000)]
    [InlineData(999_999)]
    [InlineData(UnreadableInteger)]
    public void LeavesUnrelatedSealValuesUnverified(int shownSeals)
    {
        Assert.Equal(RewardWindowJudgment.SealsNotLocated, JudgeWithoutCap(Window(MapleLumber, shownSeals), highQualityRow));
    }

    [Fact]
    public void LeavesTheSealsUnverifiedWhenTheSealIndexIsMissing()
    {
        var shown = Window(
            [new RewardWindowText(RewardWindowCheck.ItemNameIndex, MapleLumber)],
            [new RewardWindowInteger(RewardWindowCheck.SealRewardIndex - 1, NormalSeals), new RewardWindowInteger(ExperienceIndex, Experience)]);

        Assert.Equal(RewardWindowJudgment.SealsNotLocated, JudgeWithoutCap(shown, highQualityRow));
    }

    [Fact]
    public void LeavesTheSealsUnverifiedWhenTheSealIndexHoldsText()
    {
        var shown = Window(
            [new RewardWindowText(RewardWindowCheck.ItemNameIndex, MapleLumber), new RewardWindowText(RewardWindowCheck.SealRewardIndex, "300")],
            []);

        Assert.Equal(RewardWindowJudgment.SealsNotLocated, JudgeWithoutCap(shown, highQualityRow));
    }

    [Fact]
    public void AcceptsSealsTheCapCutShort()
    {
        Assert.Equal(RewardWindowJudgment.Matches, RewardWindowCheck.Judge(Window(MapleLumber, 200), MapleLumber, highQualityRow, RankCap - 200, RankCap));
    }

    [Fact]
    public void StillRefusesNormalQualitySealsWhenTheCapLeavesRoom()
    {
        Assert.Equal(RewardWindowVerdict.OtherSeals, RewardWindowCheck.Judge(Window(MapleLumber, NormalSeals), MapleLumber, highQualityRow, RankCap - 5_000, RankCap).Verdict);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(399)]
    public void MeasuresNormalQualitySealsFromTheFloorTheCapLeaves(int shownSeals)
    {
        Assert.Equal(RewardWindowVerdict.OtherSeals, RewardWindowCheck.Judge(Window(MapleLumber, shownSeals), MapleLumber, highQualityRow, RankCap - 400, RankCap).Verdict);
    }

    [Fact]
    public void PassesNormalQualitySealsOnceTheCapLeavesNoMoreThanThem()
    {
        Assert.Equal(RewardWindowJudgment.Matches, RewardWindowCheck.Judge(Window(MapleLumber, NormalSeals), MapleLumber, highQualityRow, RankCap - NormalSeals, RankCap));
    }

    [Theory]
    [InlineData(RankCap - NormalSeals)]
    [InlineData(RankCap - 1)]
    [InlineData(RankCap)]
    public void SaysTheCapHidesTheQualityWhenItLeavesNoMoreThanNormalQualitySeals(int sealsBefore)
    {
        Assert.True(RewardWindowCheck.CapHidesQuality(highQualityRow, sealsBefore, RankCap));
    }

    [Theory]
    [InlineData(NoSealsYet)]
    [InlineData(RankCap - HighSeals)]
    [InlineData(RankCap - NormalSeals - 1)]
    public void SaysTheSealsTellTheQualityWhileTheCapLeavesMoreThanNormalQualitySeals(int sealsBefore)
    {
        Assert.False(RewardWindowCheck.CapHidesQuality(highQualityRow, sealsBefore, RankCap));
    }

    [Fact]
    public void NeverSaysTheCapHidesTheQualityOfANormalQualityRow()
    {
        Assert.False(RewardWindowCheck.CapHidesQuality(normalQualityRow, RankCap, RankCap));
    }

    [Fact]
    public void NeverSaysTheCapHidesTheQualityWithoutAKnownCap()
    {
        Assert.False(RewardWindowCheck.CapHidesQuality(highQualityRow, RankCap, SealCapRule.UnknownCap));
    }

    [Fact]
    public void NeverSaysTheCapHidesTheQualityOfARowWithoutSeals()
    {
        Assert.False(RewardWindowCheck.CapHidesQuality(highQualityRow with { SealReward = 0 }, RankCap, RankCap));
    }

    [Fact]
    public void HalvesHighQualitySealsForNormalQuality()
    {
        Assert.Equal(NormalSeals, RewardWindowCheck.NormalQualitySeals(HighSeals));
    }

    [Fact]
    public void ChecksOnlyTheItemWhenTheRowsSealsAreUnknown()
    {
        var unknownSealsRow = highQualityRow with { SealReward = 0 };

        Assert.Equal(RewardWindowJudgment.Matches, JudgeWithoutCap(Window(MapleLumber, 12_345), unknownSealsRow));
    }

    [Fact]
    public void ChecksOnlyTheItemWhenTheRowsSealsAreUnknownAndTheWindowHoldsNoSeals()
    {
        var unknownSealsRow = highQualityRow with { SealReward = 0 };
        var shown = Window([new RewardWindowText(RewardWindowCheck.ItemNameIndex, MapleLumber)], []);

        Assert.Equal(RewardWindowJudgment.Matches, JudgeWithoutCap(shown, unknownSealsRow));
    }

    [Theory]
    [InlineData(300, 750)]
    [InlineData(1, 3)]
    [InlineData(0, 0)]
    public void AllowsUpToTwoAndAHalfTimesTheExpectedSeals(int expected, int highest)
    {
        Assert.Equal(highest, RewardWindowCheck.HighestSeals(expected));
    }

    [Fact]
    public void DescribesAWindowForAnotherItemWithBothNamesAndTheSealRange()
    {
        var shown = Window(" Spru­ce Lumber ", HighSeals);

        var mismatch = RewardWindowCheck.Mismatch(RewardWindowVerdict.OtherItem, shown, MapleLumber, highQualityRow, NoSealsYet, RankCap);

        Assert.Equal(new RewardWindowMismatch(RewardWindowVerdict.OtherItem, "Spruce Lumber", HighSeals, MapleLumber, HighSeals, 1_500), mismatch);
    }

    [Fact]
    public void DescribesAnotherItemWithTheFirstTextWhenTheNameIndexIsEmpty()
    {
        var shown = Window(
            [new RewardWindowText(1, " "), new RewardWindowText(2, "Spruce Lumber"), new RewardWindowText(RewardWindowCheck.ItemNameIndex, ""), new RewardWindowText(OtherTextIndex, "Deliver")],
            []);

        var mismatch = RewardWindowCheck.Mismatch(RewardWindowVerdict.OtherItem, shown, MapleLumber, highQualityRow, NoSealsYet, SealCapRule.UnknownCap);

        Assert.Equal(new RewardWindowMismatch(RewardWindowVerdict.OtherItem, "Spruce Lumber", 0, MapleLumber, HighSeals, 1_500), mismatch);
    }

    [Fact]
    public void DescribesOtherSealsWithTheFloorTheCapLeaves()
    {
        var mismatch = RewardWindowCheck.Mismatch(RewardWindowVerdict.OtherSeals, Window(MapleLumber, 100), MapleLumber, highQualityRow, RankCap - 200, RankCap);

        Assert.Equal(new RewardWindowMismatch(RewardWindowVerdict.OtherSeals, MapleLumber, 100, MapleLumber, 200, 1_500), mismatch);
    }

    [Fact]
    public void DescribesOtherSealsWithTheTextThatShowsTheItem()
    {
        var shown = Window(
            [new RewardWindowText(2, "Maple Lumber x3"), new RewardWindowText(RewardWindowCheck.ItemNameIndex, "Deliver")],
            [new RewardWindowInteger(RewardWindowCheck.SealRewardIndex, NormalSeals)]);

        var mismatch = RewardWindowCheck.Mismatch(RewardWindowVerdict.OtherSeals, shown, MapleLumber, highQualityRow, NoSealsYet, SealCapRule.UnknownCap);

        Assert.Equal("Maple Lumber x3", mismatch.ShownItemName);
    }

    [Fact]
    public void PrefersTheNameIndexWhenSeveralTextsShowTheItem()
    {
        var shown = Window(
            [new RewardWindowText(2, "Maple Lumber x3"), new RewardWindowText(RewardWindowCheck.ItemNameIndex, "Maple Lumber ")],
            [new RewardWindowInteger(RewardWindowCheck.SealRewardIndex, NormalSeals)]);

        var mismatch = RewardWindowCheck.Mismatch(RewardWindowVerdict.OtherSeals, shown, MapleLumber, highQualityRow, NoSealsYet, SealCapRule.UnknownCap);

        Assert.Equal(MapleLumber, mismatch.ShownItemName);
    }

    [Fact]
    public void DescribesAnUnreadableWindowWithoutShownValues()
    {
        var mismatch = RewardWindowCheck.Mismatch(RewardWindowVerdict.Unreadable, RewardWindowSample.Unreadable, MapleLumber, normalQualityRow, NoSealsYet, SealCapRule.UnknownCap);

        Assert.Equal(new RewardWindowMismatch(RewardWindowVerdict.Unreadable, string.Empty, 0, MapleLumber, NormalSeals, 750), mismatch);
    }

    [Fact]
    public void CannotJudgeAWindowItCouldNotRead()
    {
        Assert.Equal(RewardWindowJudgment.Refuses(RewardWindowVerdict.Unreadable), JudgeWithoutCap(RewardWindowSample.Unreadable, highQualityRow));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("")]
    public void CannotJudgeAWindowWithoutAnItemName(string shownName)
    {
        Assert.Equal(RewardWindowVerdict.Unreadable, JudgeWithoutCap(Window(shownName, HighSeals), highQualityRow).Verdict);
    }

    [Fact]
    public void CannotJudgeAWindowWithoutTexts()
    {
        var shown = Window([], [new RewardWindowInteger(RewardWindowCheck.SealRewardIndex, HighSeals), new RewardWindowInteger(ExperienceIndex, Experience)]);

        Assert.Equal(RewardWindowJudgment.Refuses(RewardWindowVerdict.Unreadable), JudgeWithoutCap(shown, highQualityRow));
    }

    [Fact]
    public void CannotJudgeWithoutTheItemsOwnName()
    {
        Assert.Equal(RewardWindowVerdict.Unreadable, RewardWindowCheck.Judge(Window(MapleLumber, HighSeals), string.Empty, highQualityRow, NoSealsYet, SealCapRule.UnknownCap).Verdict);
    }

    private static RewardWindowJudgment JudgeWithoutCap(in RewardWindowSample shown, in HandInTarget target)
        => RewardWindowCheck.Judge(shown, MapleLumber, target, NoSealsYet, SealCapRule.UnknownCap);

    private static RewardWindowSample Window(string itemName, int seals)
        => Window([new RewardWindowText(RewardWindowCheck.ItemNameIndex, itemName)], [new RewardWindowInteger(RewardWindowCheck.SealRewardIndex, seals)]);

    private static RewardWindowSample Window(RewardWindowText[] texts, RewardWindowInteger[] integers) => new(true, texts, integers);
}
