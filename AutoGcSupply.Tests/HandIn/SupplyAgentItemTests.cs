using AutoGcSupply.Core.HandIn.Model;
using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Requests.Model;

namespace AutoGcSupply.Tests.HandIn;

public sealed class SupplyAgentItemTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(255)]
    public void OpenDailyRequestWithOwnedHqItemCanBeDeliveredRegardlessOfAgentFlag(byte rawFlag)
    {
        var requests = SupplyFixtures.Snapshot(SupplyFixtures.OpenDay);
        Assert.True(requests.TryFind(SupplyClasses.Carpenter, out var request));
        Assert.True(SupplyAgentItem.TryCreate(request.ItemId, 0, request.Requested, request.Requested, rawFlag, requests, out var item));
        var target = new HandInTarget(request.ItemId, request.ClassJobId, request.Requested, ItemQuality.High, 200);
        var list = new SupplyListState(true, SupplyListTab.Supply,
            [new(0, request.ItemId, request.ClassJobId, request.Requested, request.Starred)], [item]);

        Assert.Equal(rawFlag, item.RawTurnInFlag);
        Assert.True(RowMatcher.Evaluate(list, target, new OwnedCount(0, request.Requested), 0).Found);
        Assert.True(list.RowStillOpen(target));
        Assert.Equal(RowMatchStatus.NotEnoughInBags, RowMatcher.Evaluate(list, target, new OwnedCount(request.Requested, 0), 0).Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void CompletedDailyRequestIsClosedEvenWithSpareItems(byte rawFlag)
    {
        var requests = SupplyFixtures.Snapshot(SupplyFixtures.Closed(SupplyFixtures.OpenDayCopy()));
        Assert.True(requests.TryFind(SupplyClasses.Carpenter, out var request));
        Assert.True(SupplyAgentItem.TryCreate(request.ItemId, 0, 99, request.Requested, rawFlag, requests, out var item));
        var target = new HandInTarget(request.ItemId, request.ClassJobId, request.Requested, ItemQuality.High, 200);
        var list = new SupplyListState(true, SupplyListTab.Supply,
            [new(0, request.ItemId, request.ClassJobId, request.Requested, request.Starred)], [item]);

        Assert.Equal(RowMatchStatus.Closed, RowMatcher.Evaluate(list, target, new OwnedCount(99, 99), 0).Status);
        Assert.False(list.RowStillOpen(target));
    }

    [Fact]
    public void CompletedRequestMayHaveItsDailyItemAndQuantityCleared()
    {
        var entries = SupplyFixtures.OpenDayCopy();
        var original = entries[0];
        entries[0] = original with { ItemId = 0, Requested = 0, RemainingToHandIn = 0 };
        var requests = SupplyFixtures.Snapshot(entries);

        Assert.True(SupplyAgentItem.TryCreate(original.ItemId, 0, 1, original.Requested, 1, requests, out var item));
        Assert.False(item.RequestOpen);
    }

    [Fact]
    public void RequiresLoadedMatchingDailyRequest()
    {
        var requests = SupplyFixtures.Snapshot(SupplyFixtures.OpenDay);
        Assert.True(requests.TryFind(SupplyClasses.Carpenter, out var request));
        Assert.False(SupplyAgentItem.TryCreate(request.ItemId, 0, 99, request.Requested, 0, RequestSnapshot.Unread(DateTime.UtcNow), out _));
        Assert.False(SupplyAgentItem.TryCreate(request.ItemId + 1, 0, 99, request.Requested, 0, requests, out _));
        Assert.False(SupplyAgentItem.TryCreate(request.ItemId, 0, 99, request.Requested + 1, 0, requests, out _));
        Assert.False(SupplyAgentItem.TryCreate(request.ItemId, 1, 99, request.Requested, 0, requests, out _));
        Assert.False(SupplyAgentItem.TryCreate(request.ItemId, -1, 99, request.Requested, 0, requests, out _));
        Assert.False(SupplyAgentItem.TryCreate(request.ItemId, 11, 99, request.Requested, 0, requests, out _));
    }

    [Theory]
    [InlineData(SupplyClasses.Miner)]
    [InlineData(SupplyClasses.Botanist)]
    [InlineData(SupplyClasses.Fisher)]
    public void ResolvesProvisioningByClassRatherThanSnapshotOrder(byte classJob)
    {
        var requests = SupplyFixtures.Snapshot(SupplyFixtures.OpenDay);
        Assert.True(requests.TryFind(classJob, out var request));
        Assert.True(SupplyAgentItem.TryCreate(request.ItemId, classJob - SupplyClasses.First, request.Requested, request.Requested, 1, requests, out var item));
        var target = new HandInTarget(request.ItemId, classJob, request.Requested, ItemQuality.Normal, 200);
        var list = new SupplyListState(true, SupplyListTab.Provisioning,
            [new(2, request.ItemId, classJob, request.Requested, request.Starred)], [item]);

        Assert.Equal(2, RowMatcher.Evaluate(list, target, new OwnedCount(request.Requested, 0), 0).RowIndex);
    }
}
