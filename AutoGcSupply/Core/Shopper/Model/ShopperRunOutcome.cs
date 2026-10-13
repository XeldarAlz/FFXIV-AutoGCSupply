namespace AutoGcSupply.Core.Shopper.Model;

internal enum ShopperRunOutcome : byte
{
    None,
    Unknown,
    Completed,
    CompletedPartial,
    Stopped,
    Failed,
    NeedsReconciliation,
}
