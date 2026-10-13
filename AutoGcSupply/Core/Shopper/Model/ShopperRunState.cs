namespace AutoGcSupply.Core.Shopper.Model;

internal enum ShopperRunState : byte
{
    Unknown,
    Idle,
    Preflight,
    FetchingPrices,
    Planning,
    Ready,
    Traveling,
    GoingToBoard,
    Searching,
    Inspecting,
    Guarding,
    PersistingIntent,
    Submitting,
    AwaitingResult,
    Reconciling,
    Paused,
    NeedsReconciliation,
    Completed,
    CompletedPartial,
    Stopped,
    Failed,
}
