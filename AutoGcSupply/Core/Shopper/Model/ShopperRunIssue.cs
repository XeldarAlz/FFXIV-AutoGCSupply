namespace AutoGcSupply.Core.Shopper.Model;

internal readonly record struct ShopperRunIssue(ShopperIssueReason Reason, uint ItemId);
