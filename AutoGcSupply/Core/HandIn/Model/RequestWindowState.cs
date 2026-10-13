using AutoGcSupply.Core.Inventory.Model;

namespace AutoGcSupply.Core.HandIn.Model;

internal readonly record struct RequestSlot(uint ItemId, int Quantity, bool HighQualityRequired);

internal readonly record struct RequestStack(uint ItemId, int Quantity, ItemQuality Quality);

internal readonly record struct RequestOption(int Index, uint ItemId, ItemQuality Quality, bool InBags);

internal readonly record struct RequestWindowState(bool Readable, RequestSlot[] Slots, RequestStack[] Offered, bool HandOverEnabled)
{
    public static RequestWindowState Unreadable { get; } = new(false, [], [], false);
}

internal enum RequestWindowVerdict : byte
{
    Waiting,
    NeedsItems,
    Ready,
    OtherRequest,
    OtherItem,
    OtherQuality,
    OtherQuantity,
}
