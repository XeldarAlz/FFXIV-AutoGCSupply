using AutoGcSupply.Core.HandIn.Model;
using AutoGcSupply.Core.Inventory.Model;

namespace AutoGcSupply.Tests.HandIn;

public sealed class SupplyListStateTests
{
    private const uint CarpenterItem = 2_001;
    private const uint MinerItem = 2_004;

    private static readonly HandInTarget carpenterTarget = new(CarpenterItem, 8, 1, ItemQuality.High, 200);
    private static readonly SupplyListRow carpenterRow = new(0, CarpenterItem, 8, 1, false);

    [Fact]
    public void ARowStaysOpenWhileListedAndAvailable()
    {
        var list = new SupplyListState(true, SupplyListTab.Supply, [carpenterRow], [new(CarpenterItem, 0, 1, 1, true)]);

        Assert.True(list.RowStillOpen(carpenterTarget));
    }

    [Fact]
    public void ARowClosesWhenTheGameMarksItDone()
    {
        var list = new SupplyListState(true, SupplyListTab.Supply, [carpenterRow], [new(CarpenterItem, 0, 0, 1, false)]);

        Assert.False(list.RowStillOpen(carpenterTarget));
    }

    [Fact]
    public void ARowClosesWhenItLeavesTheList()
    {
        var list = new SupplyListState(true, SupplyListTab.Supply, [], [new(CarpenterItem, 0, 0, 1, true)]);

        Assert.False(list.RowStillOpen(carpenterTarget));
    }

    [Fact]
    public void AListThatCannotBeReadIsNoEvidenceOfAClosedRow()
    {
        var provisioningTab = new SupplyListState(true, SupplyListTab.Provisioning, [new(0, MinerItem, 16, 10, false)], []);

        Assert.True(SupplyListState.NotLoaded.RowStillOpen(carpenterTarget));
        Assert.True(provisioningTab.RowStillOpen(carpenterTarget));
    }
}
