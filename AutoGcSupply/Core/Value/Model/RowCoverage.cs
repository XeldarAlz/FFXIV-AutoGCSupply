namespace AutoGcSupply.Core.Value.Model;

public enum RowCoverage : byte
{
    Owned = 0,
    ToBuy = 1,
    Unpriced = 2,
    NoListings = 3,
    Delivered = 4,
    Excluded = 5,
    MaxLevel = 6,
}
