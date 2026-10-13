using AutoGcSupply.Core.Requests.Model;
using System.Buffers.Binary;

namespace AutoGcSupply.Tests;

internal readonly record struct EntryFixture(
    byte ClassJobId,
    uint ItemId,
    uint ExperienceReward,
    uint SealReward,
    byte Requested,
    byte RemainingToHandIn,
    bool Starred = false,
    byte NoHighQualityFlag = 0);

internal static class SupplyFixtures
{
    public static readonly DateTime ReadAtUtc = new(2026, 10, 13, 21, 15, 0, DateTimeKind.Utc);

    public const ulong ContentId = 0x0040_0000_1234_5678;

    public static readonly Func<uint, bool> AnyItemExists = static _ => true;

    public static readonly EntryFixture[] OpenDay =
    [
        new(SupplyClasses.Carpenter, 43273, 180_000, 420, 1, 1),
        new(SupplyClasses.Culinarian, 44098, 175_000, 410, 3, 3, Starred: true),
        new(SupplyClasses.Alchemist, 44157, 176_000, 412, 3, 3),
        new(SupplyClasses.Armorer, 43269, 181_000, 421, 1, 1),
        new(SupplyClasses.Leatherworker, 43316, 182_000, 422, 1, 1),
        new(SupplyClasses.Goldsmith, 43341, 183_000, 423, 1, 1),
        new(SupplyClasses.Weaver, 43314, 184_000, 424, 1, 1),
        new(SupplyClasses.Blacksmith, 43263, 185_000, 425, 1, 1),
        new(SupplyClasses.Miner, 5106, 90_000, 300, 10, 10),
        new(SupplyClasses.Botanist, 5509, 91_000, 301, 20, 20, Starred: true),
        new(SupplyClasses.Fisher, 4925, 92_000, 302, 5, 5),
    ];

    public static byte[] Bytes(ReadOnlySpan<EntryFixture> entries)
    {
        var raw = new byte[SnapshotParser.TotalBytes];
        for (var slot = 0; slot < entries.Length; slot++)
        {
            Write(raw, slot, entries[slot]);
        }

        return raw;
    }

    public static void Write(byte[] raw, int slot, EntryFixture entry)
    {
        var span = raw.AsSpan(slot * SnapshotParser.EntrySize, SnapshotParser.EntrySize);
        BinaryPrimitives.WriteUInt32LittleEndian(span[SnapshotParser.ItemIdOffset..], entry.ItemId);
        BinaryPrimitives.WriteUInt32LittleEndian(span[SnapshotParser.ExperienceRewardOffset..], entry.ExperienceReward);
        BinaryPrimitives.WriteUInt32LittleEndian(span[SnapshotParser.SealRewardOffset..], entry.SealReward);
        span[SnapshotParser.StarredOffset] = entry.Starred ? (byte)1 : (byte)0;
        span[SnapshotParser.NoHighQualityOffset] = entry.NoHighQualityFlag;
        span[SnapshotParser.RequestedOffset] = entry.Requested;
        span[SnapshotParser.RemainingToHandInOffset] = entry.RemainingToHandIn;
        span[SnapshotParser.ClassJobIdOffset] = entry.ClassJobId;
    }

    public static EntryFixture[] OpenDayCopy() => (EntryFixture[])OpenDay.Clone();

    public static EntryFixture[] Closed(EntryFixture[] entries)
    {
        for (var index = 0; index < entries.Length; index++)
        {
            entries[index] = entries[index] with { RemainingToHandIn = 0 };
        }

        return entries;
    }

    public static SupplyRequest[] Requests(ReadOnlySpan<EntryFixture> entries) => SnapshotParser.Parse(Bytes(entries));

    public static RequestSnapshot Snapshot(ReadOnlySpan<EntryFixture> entries)
        => RequestSnapshot.Create(ContentId, ReadAtUtc, Requests(entries), AnyItemExists);
}
