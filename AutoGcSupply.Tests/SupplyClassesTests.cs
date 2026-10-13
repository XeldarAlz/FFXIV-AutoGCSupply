using AutoGcSupply.Core.Requests.Model;

namespace AutoGcSupply.Tests;

public sealed class SupplyClassesTests
{
    [Fact]
    public void DisplayOrderRunsCarpenterToFisher()
    {
        var order = SupplyClasses.DisplayOrder;

        Assert.Equal(SupplyClasses.Count, order.Length);
        Assert.Equal(new byte[] { 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 }, order.ToArray());
        for (var index = 0; index < order.Length; index++)
        {
            Assert.Equal(index, SupplyClasses.DisplayIndex(order[index]));
        }
    }

    [Theory]
    [InlineData(SupplyClasses.Carpenter, SupplyMissionKind.Supply)]
    [InlineData(SupplyClasses.Culinarian, SupplyMissionKind.Supply)]
    [InlineData(SupplyClasses.Miner, SupplyMissionKind.Provisioning)]
    [InlineData(SupplyClasses.Botanist, SupplyMissionKind.Provisioning)]
    [InlineData(SupplyClasses.Fisher, SupplyMissionKind.Provisioning)]
    public void KindFollowsTheClass(byte classJobId, SupplyMissionKind kind)
    {
        Assert.Equal(kind, SupplyClasses.KindOf(classJobId));
    }

    [Fact]
    public void CountsSplitEightAndThree()
    {
        Assert.Equal(11, SupplyClasses.Count);
        Assert.Equal(8, SupplyClasses.SupplyCount);
        Assert.Equal(3, SupplyClasses.ProvisioningCount);
    }

    [Theory]
    [InlineData(7, false)]
    [InlineData(8, true)]
    [InlineData(18, true)]
    [InlineData(19, false)]
    public void ContainsOnlyTheElevenClasses(byte classJobId, bool contained)
    {
        Assert.Equal(contained, SupplyClasses.Contains(classJobId));
    }

    [Fact]
    public void IconIdAddsTheClassToTheIconBase()
    {
        Assert.Equal(62008u, SupplyClasses.IconId(SupplyClasses.Carpenter));
        Assert.Equal(62018u, SupplyClasses.IconId(SupplyClasses.Fisher));
    }

    [Fact]
    public void AbbreviationsMatchTheGame()
    {
        Assert.Equal("CRP", SupplyClasses.Abbreviation(SupplyClasses.Carpenter));
        Assert.Equal("GSM", SupplyClasses.Abbreviation(SupplyClasses.Goldsmith));
        Assert.Equal("CUL", SupplyClasses.Abbreviation(SupplyClasses.Culinarian));
        Assert.Equal("FSH", SupplyClasses.Abbreviation(SupplyClasses.Fisher));
        Assert.Equal("#0", SupplyClasses.Abbreviation(0));
    }

    [Fact]
    public void ClassMaskTogglesSingleClasses()
    {
        var mask = ClassMask.None.With(SupplyClasses.Armorer, true).With(SupplyClasses.Fisher, true);

        Assert.True(mask.Contains(SupplyClasses.Armorer));
        Assert.True(mask.Contains(SupplyClasses.Fisher));
        Assert.False(mask.Contains(SupplyClasses.Miner));
        Assert.False(mask.Contains(0));

        var cleared = mask.With(SupplyClasses.Armorer, false);

        Assert.False(cleared.Contains(SupplyClasses.Armorer));
        Assert.True(cleared.Contains(SupplyClasses.Fisher));
        Assert.Equal(mask, mask.With(SupplyClasses.Fisher, true));
    }

    [Fact]
    public void ClassMaskRefusesOtherClasses()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ClassMask.None.With(19, true));
    }
}
