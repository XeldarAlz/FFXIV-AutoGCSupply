namespace AutoGcSupply.Core.Shopper.Model;

internal readonly record struct ShopperQuotePrice(bool Available, int Units, long Total, long UnitPrice, long HighestUnitPrice = 0);
