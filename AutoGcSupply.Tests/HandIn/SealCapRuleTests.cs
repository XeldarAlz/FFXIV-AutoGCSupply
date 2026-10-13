using AutoGcSupply.Core.HandIn.Model;

namespace AutoGcSupply.Tests.HandIn;

public sealed class SealCapRuleTests
{
    private const int SealCap = 10_000;

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void DeliversWhileTheRewardFits(int policy)
    {
        var decision = SealCapRule.Evaluate(9_000, 1_000, SealCap, (SealCapPolicy)policy);

        Assert.Equal(SealCapAction.Deliver, decision.Action);
        Assert.Equal(0, decision.SealsOverCap);
    }

    [Fact]
    public void DeliversWithALossWhenTheSettingSaysDeliverAnyway()
    {
        var decision = SealCapRule.Evaluate(9_800, 500, SealCap, SealCapPolicy.DeliverAnyway);

        Assert.Equal(SealCapAction.DeliverWithLoss, decision.Action);
        Assert.Equal(300, decision.SealsOverCap);
    }

    [Fact]
    public void SkipsTheRowWhenTheSettingSaysSkip()
    {
        var decision = SealCapRule.Evaluate(9_800, 500, SealCap, SealCapPolicy.SkipRow);

        Assert.Equal(SealCapAction.Skip, decision.Action);
        Assert.Equal(300, decision.SealsOverCap);
    }

    [Fact]
    public void LosesTheWholeRewardAtTheCap()
    {
        var decision = SealCapRule.Evaluate(SealCap, 500, SealCap, SealCapPolicy.DeliverAnyway);

        Assert.Equal(SealCapAction.DeliverWithLoss, decision.Action);
        Assert.Equal(500, decision.SealsOverCap);
    }

    [Fact]
    public void DeliversWhenTheCapIsUnknown()
    {
        var decision = SealCapRule.Evaluate(85_000, 5_000, SealCapRule.UnknownCap, SealCapPolicy.SkipRow);

        Assert.Equal(SealCapAction.Deliver, decision.Action);
        Assert.False(SealCapRule.Overflows(85_000, 5_000, SealCapRule.UnknownCap));
    }

    [Theory]
    [InlineData(9_000, 500, 9_500)]
    [InlineData(9_800, 500, SealCap)]
    [InlineData(SealCap, 500, SealCap)]
    public void ProjectsTheSealsAfterADelivery(int current, int reward, int expected)
    {
        Assert.Equal(expected, SealCapRule.SealsAfter(current, reward, SealCap));
    }
}
