using AutoGcSupply.Core.Inventory.Model;

namespace AutoGcSupply.Core.Value.Model;

public readonly record struct QualityChoice(RowCoverage Coverage, ItemQuality Quality, int BuyQuantity, long UnitPrice, long EstimatedTotal);
