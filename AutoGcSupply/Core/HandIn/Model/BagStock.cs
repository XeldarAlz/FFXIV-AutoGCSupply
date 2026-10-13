namespace AutoGcSupply.Core.HandIn.Model;

internal readonly record struct BagStock(int Normal, int High)
{
    public int Of(HandInQuality quality) => quality == HandInQuality.High ? High : Normal;
}
