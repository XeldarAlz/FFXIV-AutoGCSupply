namespace AutoGcSupply.Core.Inventory.Model;

public readonly record struct OwnedCount(int Normal, int High)
{
    public int Of(ItemQuality quality) => quality == ItemQuality.High ? High : Normal;

    public bool Covers(ItemQuality quality, int requested) => Of(quality) >= requested;

    public int ShortfallOf(ItemQuality quality, int requested) => Math.Max(0, requested - Of(quality));
}
