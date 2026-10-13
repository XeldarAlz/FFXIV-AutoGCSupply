namespace AutoGcSupply.Core.Shopper.Model;

internal enum ShopperReturnHome : byte
{
    Unknown,
    NotNeeded,
    AlreadyHome,
    Underway,
    Arrived,
    Failed,
    Skipped,
}
