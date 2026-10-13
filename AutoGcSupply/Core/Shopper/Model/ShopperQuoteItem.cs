namespace AutoGcSupply.Core.Shopper.Model;

internal readonly record struct ShopperQuoteItem(uint ItemId, int Quantity, ShopperQuotePrice Normal, ShopperQuotePrice High);
