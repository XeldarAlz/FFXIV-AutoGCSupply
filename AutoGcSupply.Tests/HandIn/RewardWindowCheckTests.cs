using AutoGcSupply.Core.HandIn.Model;
using AutoGcSupply.Core.Inventory.Model;

namespace AutoGcSupply.Tests.HandIn;

public sealed class RewardWindowCheckTests
{
    private const string MapleLumber = "Maple Lumber";
    private const int NormalSeals = 300;
    private const int HighSeals = 600;

    private static readonly HandInTarget highQualityRow = new(5_361, 8, 1, ItemQuality.High, HighSeals);
    private static readonly HandInTarget normalQualityRow = new(5_361, 8, 1, ItemQuality.Normal, NormalSeals);

    [Fact]
    public void MatchesAWindowForTheRowsItemAndSeals()
    {
        var shown = new RewardWindowSample(true, MapleLumber, HighSeals);

        Assert.Equal(RewardWindowVerdict.Matches, RewardWindowCheck.Judge(shown, MapleLumber, highQualityRow));
    }

    [Theory]
    [InlineData("maple lumber")]
    [InlineData("  Maple\nLumber ")]
    [InlineData("Ma­ple Lumber")]
    [InlineData("Maple Lumber ")]
    public void MatchesTheItemNameAcrossLayoutAndGlyphs(string shownName)
    {
        var shown = new RewardWindowSample(true, shownName, NormalSeals);

        Assert.Equal(RewardWindowVerdict.Matches, RewardWindowCheck.Judge(shown, MapleLumber, normalQualityRow));
    }

    [Fact]
    public void RefusesAWindowForAnotherItem()
    {
        var shown = new RewardWindowSample(true, "Spruce Lumber", HighSeals);

        Assert.Equal(RewardWindowVerdict.OtherItem, RewardWindowCheck.Judge(shown, MapleLumber, highQualityRow));
    }

    [Fact]
    public void ReportsTheOtherItemBeforeTheSeals()
    {
        var shown = new RewardWindowSample(true, "Spruce Lumber", NormalSeals);

        Assert.Equal(RewardWindowVerdict.OtherItem, RewardWindowCheck.Judge(shown, MapleLumber, highQualityRow));
    }

    [Fact]
    public void RefusesNormalQualitySealsForAHighQualityRow()
    {
        var shown = new RewardWindowSample(true, MapleLumber, NormalSeals);

        Assert.Equal(RewardWindowVerdict.OtherSeals, RewardWindowCheck.Judge(shown, MapleLumber, highQualityRow));
    }

    [Fact]
    public void RefusesHighQualitySealsForANormalQualityRow()
    {
        var shown = new RewardWindowSample(true, MapleLumber, HighSeals);

        Assert.Equal(RewardWindowVerdict.OtherSeals, RewardWindowCheck.Judge(shown, MapleLumber, normalQualityRow));
    }

    [Fact]
    public void CannotJudgeAWindowItCouldNotRead()
    {
        Assert.Equal(RewardWindowVerdict.Unreadable, RewardWindowCheck.Judge(RewardWindowSample.Unreadable, MapleLumber, highQualityRow));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("")]
    public void CannotJudgeAWindowWithoutAnItemName(string shownName)
    {
        var shown = new RewardWindowSample(true, shownName, HighSeals);

        Assert.Equal(RewardWindowVerdict.Unreadable, RewardWindowCheck.Judge(shown, MapleLumber, highQualityRow));
    }

    [Fact]
    public void CannotJudgeWithoutTheItemsOwnName()
    {
        var shown = new RewardWindowSample(true, MapleLumber, HighSeals);

        Assert.Equal(RewardWindowVerdict.Unreadable, RewardWindowCheck.Judge(shown, string.Empty, highQualityRow));
    }
}
