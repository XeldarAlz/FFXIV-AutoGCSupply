namespace AutoGcSupply.Core.Shopper.Model;

internal readonly record struct ShopperRunLine(uint ItemId, ShopperQuality Quality, int Requested, int Acquired, int Missing);
