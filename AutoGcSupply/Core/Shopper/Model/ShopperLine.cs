namespace AutoGcSupply.Core.Shopper.Model;

internal readonly record struct ShopperLine(uint ItemId, ShopperQuality Quality, int Quantity, long MaxUnitPrice, int ExcessAllowance);
