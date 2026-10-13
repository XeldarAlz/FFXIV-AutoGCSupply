using AutoGcSupply.Core.Requests.Model;

namespace AutoGcSupply.Tests;

public sealed class SnapshotValidatorTests
{
    [Fact]
    public void FullDayIsLoaded()
    {
        Assert.Equal(SnapshotLoadState.Loaded, Validate(SupplyFixtures.OpenDay));
    }

    [Fact]
    public void AllZeroMemoryIsNotLoaded()
    {
        var requests = SnapshotParser.Parse(new byte[SnapshotParser.TotalBytes]);

        Assert.Equal(SnapshotLoadState.NotLoaded, SnapshotValidator.Validate(requests, SupplyFixtures.AnyItemExists));
    }

    [Fact]
    public void OpenEntryWithItemZeroIsNotLoaded()
    {
        var entries = SupplyFixtures.OpenDayCopy();
        entries[6] = entries[6] with { ItemId = 0 };

        Assert.Equal(SnapshotLoadState.NotLoaded, Validate(entries));
    }

    [Fact]
    public void BlankEntryAmongFilledOnesIsNotLoaded()
    {
        var entries = SupplyFixtures.OpenDayCopy();
        entries[10] = default;

        Assert.Equal(SnapshotLoadState.NotLoaded, Validate(entries));
    }

    [Fact]
    public void ClassesWithoutAnyItemAreNotLoaded()
    {
        var entries = SupplyFixtures.Closed(SupplyFixtures.OpenDayCopy());
        for (var index = 0; index < entries.Length; index++)
        {
            entries[index] = entries[index] with { ItemId = 0 };
        }

        Assert.Equal(SnapshotLoadState.NotLoaded, Validate(entries));
    }

    [Fact]
    public void EveryRequestDeliveredIsStillLoaded()
    {
        Assert.Equal(SnapshotLoadState.Loaded, Validate(SupplyFixtures.Closed(SupplyFixtures.OpenDayCopy())));
    }

    [Fact]
    public void ClosedEntryWithItemZeroIsLoaded()
    {
        var entries = SupplyFixtures.OpenDayCopy();
        entries[2] = entries[2] with { ItemId = 0, RemainingToHandIn = 0 };

        Assert.Equal(SnapshotLoadState.Loaded, Validate(entries));
    }

    [Fact]
    public void DuplicateClassIsInvalid()
    {
        var entries = SupplyFixtures.OpenDayCopy();
        entries[1] = entries[1] with { ClassJobId = SupplyClasses.Carpenter };

        Assert.Equal(SnapshotLoadState.Invalid, Validate(entries));
    }

    [Theory]
    [InlineData(7)]
    [InlineData(19)]
    [InlineData(1)]
    public void ClassOutsideTheElevenIsInvalid(byte classJobId)
    {
        var entries = SupplyFixtures.OpenDayCopy();
        entries[0] = entries[0] with { ClassJobId = classJobId };

        Assert.Equal(SnapshotLoadState.Invalid, Validate(entries));
    }

    [Theory]
    [InlineData(0, SnapshotLoadState.Invalid)]
    [InlineData(1, SnapshotLoadState.Loaded)]
    [InlineData(99, SnapshotLoadState.Loaded)]
    [InlineData(100, SnapshotLoadState.Invalid)]
    [InlineData(255, SnapshotLoadState.Invalid)]
    public void OpenEntryNeedsOneToNinetyNineRequested(byte requested, SnapshotLoadState expected)
    {
        var entries = SupplyFixtures.OpenDayCopy();
        entries[8] = entries[8] with { Requested = requested };

        Assert.Equal(expected, Validate(entries));
    }

    [Fact]
    public void ClosedEntryIsNotHeldToTheRequestedRange()
    {
        var entries = SupplyFixtures.OpenDayCopy();
        entries[8] = entries[8] with { Requested = 0, RemainingToHandIn = 0 };

        Assert.Equal(SnapshotLoadState.Loaded, Validate(entries));
    }

    [Fact]
    public void UnknownItemIsInvalid()
    {
        var requests = SupplyFixtures.Requests(SupplyFixtures.OpenDay);

        var loadState = SnapshotValidator.Validate(requests, static itemId => itemId != 44157);

        Assert.Equal(SnapshotLoadState.Invalid, loadState);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(12)]
    public void AnythingButElevenEntriesIsInvalid(int count)
    {
        var requests = new SupplyRequest[count];
        var source = SupplyFixtures.Requests(SupplyFixtures.OpenDay);
        for (var index = 0; index < count; index++)
        {
            requests[index] = source[index % source.Length];
        }

        Assert.Equal(SnapshotLoadState.Invalid, SnapshotValidator.Validate(requests, SupplyFixtures.AnyItemExists));
    }

    [Fact]
    public void NotLoadedWinsOverAnInvalidClass()
    {
        var entries = SupplyFixtures.OpenDayCopy();
        entries[0] = entries[0] with { ClassJobId = 30 };
        entries[1] = entries[1] with { ItemId = 0 };

        Assert.Equal(SnapshotLoadState.NotLoaded, Validate(entries));
    }

    private static SnapshotLoadState Validate(ReadOnlySpan<EntryFixture> entries)
        => SnapshotValidator.Validate(SupplyFixtures.Requests(entries), SupplyFixtures.AnyItemExists);
}
