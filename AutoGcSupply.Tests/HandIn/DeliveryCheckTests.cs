using AutoGcSupply.Core.HandIn.Model;
using AutoGcSupply.Core.Inventory.Model;

namespace AutoGcSupply.Tests.HandIn;

public sealed class DeliveryCheckTests
{
    private const int SealCap = 10_000;
    private const int Reward = 300;

    private static readonly HandInTarget singleItem = new(2_001, 8, 1, ItemQuality.High, Reward);
    private static readonly HandInTarget threeItems = new(2_003, 15, 3, ItemQuality.Normal, Reward);

    [Fact]
    public void ConfirmsWhenTheItemLeavesAndTheSealsRise()
    {
        var before = new DeliverySample(2, 5_000, true);
        var after = new DeliverySample(1, 5_300, false);

        Assert.Equal(DeliveryVerdict.Confirmed, DeliveryCheck.Judge(before, after, singleItem, SealCap, final: false));
    }

    [Fact]
    public void ConfirmsWhenTheItemLeavesAndTheRowCloses()
    {
        var before = new DeliverySample(3, 5_000, true);
        var after = new DeliverySample(0, 5_000, false);

        Assert.Equal(DeliveryVerdict.Confirmed, DeliveryCheck.Judge(before, after, threeItems, SealCap, final: false));
    }

    [Fact]
    public void WaitsWhileNothingHasChangedYet()
    {
        var before = new DeliverySample(2, 5_000, true);

        Assert.Equal(DeliveryVerdict.NotYet, DeliveryCheck.Judge(before, before, singleItem, SealCap, final: false));
    }

    [Fact]
    public void ContradictsWhenNothingChangedByTheEnd()
    {
        var before = new DeliverySample(2, 5_000, true);

        Assert.Equal(DeliveryVerdict.Contradicted, DeliveryCheck.Judge(before, before, singleItem, SealCap, final: true));
    }

    [Theory]
    [InlineData(3, 5_000)]
    [InlineData(2, 4_900)]
    public void ContradictsAtOnceWhenStateMovesTheWrongWay(int ownedAfter, int sealsAfter)
    {
        var before = new DeliverySample(2, 5_000, true);
        var after = new DeliverySample(ownedAfter, sealsAfter, true);

        Assert.Equal(DeliveryVerdict.Contradicted, DeliveryCheck.Judge(before, after, singleItem, SealCap, final: false));
    }

    [Fact]
    public void WaitsForTheRewardWhenOnlyTheItemLeft()
    {
        var before = new DeliverySample(2, 5_000, true);
        var after = new DeliverySample(1, 5_000, true);

        Assert.Equal(DeliveryVerdict.NotYet, DeliveryCheck.Judge(before, after, singleItem, SealCap, final: false));
        Assert.Equal(DeliveryVerdict.Contradicted, DeliveryCheck.Judge(before, after, singleItem, SealCap, final: true));
    }

    [Fact]
    public void NeedsTheWholeRequestedAmountToLeave()
    {
        var before = new DeliverySample(3, 5_000, true);
        var after = new DeliverySample(2, 5_300, true);

        Assert.Equal(DeliveryVerdict.NotYet, DeliveryCheck.Judge(before, after, threeItems, SealCap, final: false));
        Assert.Equal(DeliveryVerdict.Contradicted, DeliveryCheck.Judge(before, after, threeItems, SealCap, final: true));
    }

    [Fact]
    public void ContradictsSealsThatRoseWithoutTheItemLeaving()
    {
        var before = new DeliverySample(2, 5_000, true);
        var after = new DeliverySample(2, 5_300, false);

        Assert.Equal(DeliveryVerdict.Contradicted, DeliveryCheck.Judge(before, after, singleItem, SealCap, final: true));
    }

    [Fact]
    public void ConfirmsAtTheCapWhenTheSealsCannotRise()
    {
        var before = new DeliverySample(2, SealCap, true);
        var after = new DeliverySample(1, SealCap, true);

        Assert.Equal(DeliveryVerdict.Confirmed, DeliveryCheck.Judge(before, after, singleItem, SealCap, final: false));
    }

    [Fact]
    public void ConfirmsWhenTheSealsStopAtTheCap()
    {
        var before = new DeliverySample(2, SealCap - 100, true);
        var after = new DeliverySample(1, SealCap, true);

        Assert.Equal(DeliveryVerdict.Confirmed, DeliveryCheck.Judge(before, after, singleItem, SealCap, final: false));
    }

    [Fact]
    public void DoesNotExcuseFlatSealsWhenTheCapIsUnknown()
    {
        var before = new DeliverySample(2, SealCap, true);
        var after = new DeliverySample(1, SealCap, true);

        Assert.Equal(DeliveryVerdict.Contradicted, DeliveryCheck.Judge(before, after, singleItem, SealCapRule.UnknownCap, final: true));
    }
}
