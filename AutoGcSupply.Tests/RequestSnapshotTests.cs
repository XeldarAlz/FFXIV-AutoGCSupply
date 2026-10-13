using AutoGcSupply.Core.Requests.Model;

namespace AutoGcSupply.Tests;

public sealed class RequestSnapshotTests
{
    [Fact]
    public void CreateStampsCharacterDayAndReadTime()
    {
        var snapshot = SupplyFixtures.Snapshot(SupplyFixtures.OpenDay);

        Assert.Equal(SupplyFixtures.ContentId, snapshot.ContentId);
        Assert.Equal(SupplyFixtures.ReadAtUtc, snapshot.ReadAtUtc);
        Assert.Equal(DayKey.For(SupplyFixtures.ReadAtUtc), snapshot.Day);
        Assert.Equal(SnapshotLoadState.Loaded, snapshot.LoadState);
        Assert.Equal(SnapshotParser.EntryCount, snapshot.Requests.Length);
    }

    [Fact]
    public void OpenDayIsNotDone()
    {
        var snapshot = SupplyFixtures.Snapshot(SupplyFixtures.OpenDay);

        Assert.True(snapshot.Loaded);
        Assert.Equal(11, snapshot.OpenCount);
        Assert.False(snapshot.AllDone);
    }

    [Fact]
    public void LoadedDayWithNothingOpenIsDone()
    {
        var snapshot = SupplyFixtures.Snapshot(SupplyFixtures.Closed(SupplyFixtures.OpenDayCopy()));

        Assert.Equal(0, snapshot.OpenCount);
        Assert.True(snapshot.AllDone);
    }

    [Fact]
    public void NeverCompleteFromUnloadedData()
    {
        var allZero = RequestSnapshot.Create(
            SupplyFixtures.ContentId,
            SupplyFixtures.ReadAtUtc,
            SnapshotParser.Parse(new byte[SnapshotParser.TotalBytes]),
            SupplyFixtures.AnyItemExists);

        Assert.Equal(SnapshotLoadState.NotLoaded, allZero.LoadState);
        Assert.Equal(0, allZero.OpenCount);
        Assert.False(allZero.AllDone);
        Assert.False(RequestSnapshot.Unread(SupplyFixtures.ReadAtUtc).AllDone);
        Assert.False(default(RequestSnapshot).AllDone);
    }

    [Fact]
    public void InvalidDataIsNeverDone()
    {
        var entries = SupplyFixtures.Closed(SupplyFixtures.OpenDayCopy());
        entries[0] = entries[0] with { ClassJobId = SupplyClasses.Fisher };

        var snapshot = SupplyFixtures.Snapshot(entries);

        Assert.Equal(SnapshotLoadState.Invalid, snapshot.LoadState);
        Assert.False(snapshot.AllDone);
    }

    [Fact]
    public void PartlyDeliveredDayCountsOnlyOpenRows()
    {
        var entries = SupplyFixtures.OpenDayCopy();
        entries[0] = entries[0] with { RemainingToHandIn = 0 };
        entries[9] = entries[9] with { RemainingToHandIn = 0 };

        var snapshot = SupplyFixtures.Snapshot(entries);

        Assert.Equal(9, snapshot.OpenCount);
        Assert.False(snapshot.AllDone);
    }

    [Fact]
    public void UnreadSnapshotHasNoRequests()
    {
        var snapshot = RequestSnapshot.Unread(SupplyFixtures.ReadAtUtc);

        Assert.Equal(0ul, snapshot.ContentId);
        Assert.Equal(SnapshotLoadState.NotLoaded, snapshot.LoadState);
        Assert.True(snapshot.Requests.IsEmpty);
    }

    [Fact]
    public void FindsARequestByClass()
    {
        var snapshot = SupplyFixtures.Snapshot(SupplyFixtures.OpenDay);

        Assert.True(snapshot.TryFind(SupplyClasses.Blacksmith, out var blacksmith));
        Assert.Equal(43263u, blacksmith.ItemId);
        Assert.Equal(7, blacksmith.Slot);
        Assert.False(RequestSnapshot.Unread(SupplyFixtures.ReadAtUtc).TryFind(SupplyClasses.Blacksmith, out _));
    }
}
