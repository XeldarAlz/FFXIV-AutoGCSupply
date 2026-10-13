using AutoGcSupply.Core.HandIn.Model;

namespace AutoGcSupply.Tests.HandIn;

public sealed class PromptTextTests
{
    private const string Expected = "Do you really want to trade a high-quality item?";

    [Theory]
    [InlineData("Do you really want to trade a high-quality item?")]
    [InlineData("  Do you really want to trade\na high-quality item?  ")]
    [InlineData("Do you really want to trade a high-qual\u00ADity item?")]
    [InlineData("do you really want to trade a HIGH-QUALITY item?")]
    public void MatchesTheSameTextAcrossLayoutDifferences(string shown)
    {
        Assert.True(PromptText.Same(shown, Expected));
    }

    [Theory]
    [InlineData("Do you really want to trade an item?")]
    [InlineData("")]
    public void RefusesOtherText(string shown)
    {
        Assert.False(PromptText.Same(shown, Expected));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NeverMatchesAnEmptyExpectation(string expected)
    {
        Assert.False(PromptText.Same(string.Empty, expected));
        Assert.False(PromptText.Same("   ", expected));
    }

    [Fact]
    public void NormalizeCollapsesWhitespaceAndDropsInvisibleCharacters()
    {
        Assert.Equal("Undertake supply missions.", PromptText.Normalize(" Undertake\r\n supply\u200B  missions. "));
    }
}
