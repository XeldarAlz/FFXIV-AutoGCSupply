using AutoGcSupply.Core.Requests.Model;

namespace AutoGcSupply.Tests;

public sealed class SnapshotParserTests
{
    [Fact]
    public void ParsesElevenEntriesInMemoryOrder()
    {
        var requests = SupplyFixtures.Requests(SupplyFixtures.OpenDay);

        Assert.Equal(SnapshotParser.EntryCount, requests.Length);
        for (var slot = 0; slot < requests.Length; slot++)
        {
            var expected = SupplyFixtures.OpenDay[slot];
            var request = requests[slot];
            Assert.Equal(slot, request.Slot);
            Assert.Equal(expected.ClassJobId, request.ClassJobId);
            Assert.Equal(expected.ItemId, request.ItemId);
            Assert.Equal(expected.ExperienceReward, request.ExperienceReward);
            Assert.Equal(expected.SealReward, request.SealReward);
            Assert.Equal(expected.Requested, request.Requested);
            Assert.Equal(expected.RemainingToHandIn, request.RemainingToHandIn);
            Assert.Equal(expected.Starred, request.Starred);
        }
    }

    [Fact]
    public void ReadsEveryFieldAtItsOffset()
    {
        var raw = new byte[SnapshotParser.TotalBytes];
        var entry = raw.AsSpan(4 * SnapshotParser.EntrySize, SnapshotParser.EntrySize);
        entry[0x00] = 0x78;
        entry[0x01] = 0x56;
        entry[0x02] = 0x34;
        entry[0x03] = 0x12;
        entry[0x04] = 0x01;
        entry[0x05] = 0x02;
        entry[0x06] = 0x03;
        entry[0x07] = 0x00;
        entry[0x08] = 0xA4;
        entry[0x09] = 0x01;
        entry[0x18] = 1;
        entry[0x19] = 7;
        entry[0x1A] = 20;
        entry[0x1B] = 20;
        entry[0x1C] = SupplyClasses.Botanist;

        var request = SnapshotParser.Parse(raw)[4];

        Assert.Equal(4, request.Slot);
        Assert.Equal(0x1234_5678u, request.ItemId);
        Assert.Equal(0x0003_0201u, request.ExperienceReward);
        Assert.Equal(420u, request.SealReward);
        Assert.True(request.Starred);
        Assert.Equal(7, request.NoHighQualityFlag);
        Assert.Equal(20, request.Requested);
        Assert.Equal(20, request.RemainingToHandIn);
        Assert.Equal(SupplyClasses.Botanist, request.ClassJobId);
        Assert.True(request.Open);
        Assert.Equal(SupplyMissionKind.Provisioning, request.Kind);
    }

    [Fact]
    public void IgnoresBytesOutsideTheKnownFields()
    {
        var clean = SupplyFixtures.Bytes(SupplyFixtures.OpenDay);
        var noisy = SupplyFixtures.Bytes(SupplyFixtures.OpenDay);
        for (var slot = 0; slot < SnapshotParser.EntryCount; slot++)
        {
            var entry = noisy.AsSpan(slot * SnapshotParser.EntrySize, SnapshotParser.EntrySize);
            entry[0x0C..0x18].Fill(0xFF);
            entry[0x1D..].Fill(0xEE);
        }

        Assert.Equal(SnapshotParser.Parse(clean), SnapshotParser.Parse(noisy));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public void StarredIsAnyNonZeroByte(byte value, bool starred)
    {
        var raw = SupplyFixtures.Bytes(SupplyFixtures.OpenDay);
        raw[SnapshotParser.StarredOffset] = value;

        Assert.Equal(starred, SnapshotParser.Parse(raw)[0].Starred);
    }

    [Fact]
    public void ClosedEntryIsNotOpen()
    {
        var entries = SupplyFixtures.OpenDayCopy();
        entries[3] = entries[3] with { RemainingToHandIn = 0 };

        var requests = SupplyFixtures.Requests(entries);

        Assert.False(requests[3].Open);
        Assert.True(requests[2].Open);
    }

    [Fact]
    public void AllZeroMemoryParsesIntoBlankEntries()
    {
        var requests = SnapshotParser.Parse(new byte[SnapshotParser.TotalBytes]);

        for (var slot = 0; slot < requests.Length; slot++)
        {
            Assert.Equal(new SupplyRequest((byte)slot, 0, 0, 0, 0, 0, 0, false), requests[slot]);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(SnapshotParser.TotalBytes - 1)]
    [InlineData(SnapshotParser.TotalBytes + 1)]
    public void RefusesAnythingButElevenEntries(int length)
    {
        Assert.Throws<ArgumentException>(() => SnapshotParser.Parse(new byte[length]));
    }

    [Fact]
    public void LayoutMatchesTheGameEntrySize()
    {
        Assert.Equal(0xA8, SnapshotParser.EntrySize);
        Assert.Equal(11 * 0xA8, SnapshotParser.TotalBytes);
    }
}
