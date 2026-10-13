using System.Buffers.Binary;

namespace AutoGcSupply.Core.Requests.Model;

public static class SnapshotParser
{
    public const int EntryCount = SupplyClasses.Count;
    public const int EntrySize = 0xA8;
    public const int TotalBytes = EntryCount * EntrySize;

    public const int ItemIdOffset = 0x00;
    public const int ExperienceRewardOffset = 0x04;
    public const int SealRewardOffset = 0x08;
    public const int StarredOffset = 0x18;
    public const int NoHighQualityOffset = 0x19;
    public const int RequestedOffset = 0x1A;
    public const int RemainingToHandInOffset = 0x1B;
    public const int ClassJobIdOffset = 0x1C;

    public static SupplyRequest[] Parse(ReadOnlySpan<byte> raw)
    {
        if (raw.Length != TotalBytes)
        {
            throw new ArgumentException($"Expected {TotalBytes} bytes of supply requests, got {raw.Length}.", nameof(raw));
        }

        var requests = new SupplyRequest[EntryCount];
        for (var slot = 0; slot < EntryCount; slot++)
        {
            requests[slot] = ParseEntry((byte)slot, raw.Slice(slot * EntrySize, EntrySize));
        }

        return requests;
    }

    private static SupplyRequest ParseEntry(byte slot, ReadOnlySpan<byte> entry)
    {
        return new SupplyRequest(
            slot,
            entry[ClassJobIdOffset],
            BinaryPrimitives.ReadUInt32LittleEndian(entry[ItemIdOffset..]),
            BinaryPrimitives.ReadUInt32LittleEndian(entry[ExperienceRewardOffset..]),
            BinaryPrimitives.ReadUInt32LittleEndian(entry[SealRewardOffset..]),
            entry[RequestedOffset],
            entry[RemainingToHandInOffset],
            entry[StarredOffset] != 0)
        {
            NoHighQualityFlag = entry[NoHighQualityOffset],
        };
    }
}
