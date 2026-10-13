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

    private static readonly HandInTarget highQualityRow = new(5_361, 8, 1, ItemQuality.High, HighSeals);
    private static readonly HandInTarget normalQualityRow = new(5_361, 8, 1, ItemQuality.Normal, NormalSeals);

    [Fact]
    public void MatchesAWindowForTheRowsItemAndSeals()
    {
        var shown = new RewardWindowSample(true, MapleLumber, HighSeals);

        Assert.Equal(RewardWindowVerdict.Matches, RewardWindowCheck.Judge(shown, MapleLumber, highQualityRow, NoSealsYet, SealCapRule.UnknownCap));
    }

    [Theory]
    [InlineData("maple lumber")]
    [InlineData("  Maple\nLumber ")]
    [InlineData("Ma­ple Lumber")]
    [InlineData("Maple Lumber ")]
    public void MatchesTheItemNameAcrossLayoutAndGlyphs(string shownName)
    {
        var shown = new RewardWindowSample(true, shownName, NormalSeals);

        Assert.Equal(RewardWindowVerdict.Matches, RewardWindowCheck.Judge(shown, MapleLumber, normalQualityRow, NoSealsYet, SealCapRule.UnknownCap));
    }

    [Fact]
    public void RefusesAWindowForAnotherItem()
    {
        var shown = new RewardWindowSample(true, "Spruce Lumber", HighSeals);

        Assert.Equal(RewardWindowVerdict.OtherItem, RewardWindowCheck.Judge(shown, MapleLumber, highQualityRow, NoSealsYet, SealCapRule.UnknownCap));
    }

    [Fact]
    public void ReportsTheOtherItemBeforeTheSeals()
    {
        var shown = new RewardWindowSample(true, "Spruce Lumber", NormalSeals);

        Assert.Equal(RewardWindowVerdict.OtherItem, RewardWindowCheck.Judge(shown, MapleLumber, highQualityRow, NoSealsYet, SealCapRule.UnknownCap));
    }

    [Fact]
    public void RefusesNormalQualitySealsForAHighQualityRow()
    {
        var shown = new RewardWindowSample(true, MapleLumber, NormalSeals);

        Assert.Equal(RewardWindowVerdict.OtherSeals, RewardWindowCheck.Judge(shown, MapleLumber, highQualityRow, NoSealsYet, SealCapRule.UnknownCap));
    }

    [Theory]
    [InlineData(HighSeals)]
    [InlineData(690)]
    [InlineData(1_200)]
    [InlineData(1_380)]
    [InlineData(1_500)]
    public void AcceptsTheStarAndSealBonusesOnTopOfTheExpectedSeals(int shownSeals)
    {
        var shown = new RewardWindowSample(true, MapleLumber, shownSeals);

        Assert.Equal(RewardWindowVerdict.Matches, RewardWindowCheck.Judge(shown, MapleLumber, highQualityRow, NoSealsYet, SealCapRule.UnknownCap));
    }

    [Fact]
    public void RefusesSealsAboveEveryKnownBonus()
    {
        var shown = new RewardWindowSample(true, MapleLumber, 1_501);

        Assert.Equal(RewardWindowVerdict.OtherSeals, RewardWindowCheck.Judge(shown, MapleLumber, highQualityRow, NoSealsYet, SealCapRule.UnknownCap));
    }

    [Fact]
    public void AcceptsSealsTheCapCutShort()
    {
        var shown = new RewardWindowSample(true, MapleLumber, 200);

        Assert.Equal(RewardWindowVerdict.Matches, RewardWindowCheck.Judge(shown, MapleLumber, highQualityRow, RankCap - 200, RankCap));
    }

    [Fact]
    public void StillRefusesNormalQualitySealsWhenTheCapLeavesRoom()
    {
        var shown = new RewardWindowSample(true, MapleLumber, NormalSeals);

        Assert.Equal(RewardWindowVerdict.OtherSeals, RewardWindowCheck.Judge(shown, MapleLumber, highQualityRow, RankCap - 5_000, RankCap));
    }

    [Fact]
    public void PassesNormalQualitySealsOnceTheCapLeavesNoMoreThanThem()
    {
        var shown = new RewardWindowSample(true, MapleLumber, NormalSeals);

        Assert.Equal(RewardWindowVerdict.Matches, RewardWindowCheck.Judge(shown, MapleLumber, highQualityRow, RankCap - NormalSeals, RankCap));
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
        var shown = new RewardWindowSample(true, MapleLumber, 12_345);

        Assert.Equal(RewardWindowVerdict.Matches, RewardWindowCheck.Judge(shown, MapleLumber, unknownSealsRow, NoSealsYet, SealCapRule.UnknownCap));
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
        var shown = new RewardWindowSample(true, "\uE03C Spru\u00ADce Lumber ", HighSeals);

        var mismatch = RewardWindowCheck.Mismatch(RewardWindowVerdict.OtherItem, shown, MapleLumber, highQualityRow, NoSealsYet, RankCap);

        Assert.Equal(new RewardWindowMismatch(RewardWindowVerdict.OtherItem, "Spruce Lumber", HighSeals, MapleLumber, HighSeals, 1_500), mismatch);
    }

    [Fact]
    public void DescribesOtherSealsWithTheFloorTheCapLeaves()
    {
        var shown = new RewardWindowSample(true, MapleLumber, 100);

        var mismatch = RewardWindowCheck.Mismatch(RewardWindowVerdict.OtherSeals, shown, MapleLumber, highQualityRow, RankCap - 200, RankCap);

        Assert.Equal(new RewardWindowMismatch(RewardWindowVerdict.OtherSeals, MapleLumber, 100, MapleLumber, 200, 1_500), mismatch);
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
        Assert.Equal(RewardWindowVerdict.Unreadable, RewardWindowCheck.Judge(RewardWindowSample.Unreadable, MapleLumber, highQualityRow, NoSealsYet, SealCapRule.UnknownCap));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("")]
    public void CannotJudgeAWindowWithoutAnItemName(string shownName)
    {
        var shown = new RewardWindowSample(true, shownName, HighSeals);

        Assert.Equal(RewardWindowVerdict.Unreadable, RewardWindowCheck.Judge(shown, MapleLumber, highQualityRow, NoSealsYet, SealCapRule.UnknownCap));
    }

    [Fact]
    public void CannotJudgeWithoutTheItemsOwnName()
    {
        var shown = new RewardWindowSample(true, MapleLumber, HighSeals);

        Assert.Equal(RewardWindowVerdict.Unreadable, RewardWindowCheck.Judge(shown, string.Empty, highQualityRow, NoSealsYet, SealCapRule.UnknownCap));
    }
}
