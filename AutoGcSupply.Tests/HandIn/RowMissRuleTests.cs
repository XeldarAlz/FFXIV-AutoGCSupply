using AutoGcSupply.Core.HandIn.Model;

namespace AutoGcSupply.Tests.HandIn;

public sealed class RowMissRuleTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WaitsForAListThatHasNotLoadedYet(bool listSettling)
    {
        Assert.Equal(RowMissAction.Retry, RowMissRule.Decide(RowMatchStatus.ListNotLoaded, withinLoadWait: true, listSettling));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HaltsWhenTheListNeverLoadsInsteadOfSkippingItsRows(bool listSettling)
    {
        Assert.Equal(RowMissAction.Halt, RowMissRule.Decide(RowMatchStatus.ListNotLoaded, withinLoadWait: false, listSettling));
    }

    [Theory]
    [InlineData((int)RowMatchStatus.NotListed)]
    [InlineData((int)RowMatchStatus.ClassMismatch)]
    [InlineData((int)RowMatchStatus.DuplicateRow)]
    [InlineData((int)RowMatchStatus.RequestChanged)]
    [InlineData((int)RowMatchStatus.AgentMissing)]
    [InlineData((int)RowMatchStatus.Closed)]
    [InlineData((int)RowMatchStatus.NotEnoughInBags)]
    [InlineData((int)RowMatchStatus.AgentDisagrees)]
    public void RetriesAListMissWhileTheListSettlesAfterADelivery(int status)
    {
        Assert.Equal(RowMissAction.Retry, RowMissRule.Decide((RowMatchStatus)status, withinLoadWait: true, listSettling: true));
        Assert.Equal(RowMissAction.Retry, RowMissRule.Decide((RowMatchStatus)status, withinLoadWait: false, listSettling: true));
    }

    [Theory]
    [InlineData((int)RowMatchStatus.NotListed)]
    [InlineData((int)RowMatchStatus.ClassMismatch)]
    [InlineData((int)RowMatchStatus.DuplicateRow)]
    [InlineData((int)RowMatchStatus.RequestChanged)]
    [InlineData((int)RowMatchStatus.AgentMissing)]
    [InlineData((int)RowMatchStatus.Closed)]
    [InlineData((int)RowMatchStatus.NotEnoughInBags)]
    [InlineData((int)RowMatchStatus.AgentDisagrees)]
    public void SkipsAListMissOnceTheListHasSettled(int status)
    {
        Assert.Equal(RowMissAction.Skip, RowMissRule.Decide((RowMatchStatus)status, withinLoadWait: true, listSettling: false));
    }

    [Theory]
    [InlineData((int)RowMatchStatus.InvalidTarget)]
    [InlineData((int)RowMatchStatus.OtherTab)]
    [InlineData((int)RowMatchStatus.NothingPending)]
    public void NeverRetriesAMissTheListCannotChange(int status)
    {
        Assert.Equal(RowMissAction.Skip, RowMissRule.Decide((RowMatchStatus)status, withinLoadWait: true, listSettling: true));
    }
}
