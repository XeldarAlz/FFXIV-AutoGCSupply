namespace AutoGcSupply.Core.Shopper.Model;

internal enum ShopperRefusal : byte
{
    None,
    Unknown,
    UnsupportedSchema,
    InvalidRequest,
    ConsentRequired,
    ConsentDenied,
    Busy,
    SetupNeeded,
    LoggedOut,
    JournalDamaged,
    NeedsReconciliation,
    InterruptedRun,
    ReadingUnavailable,
    BuyingUnavailable,
    ListLoading,
    PricesUnavailable,
    PlanFailed,
    NothingToBuy,
    AuthorizationFailed,
    JournalWriteFailed,
    CharacterChanged,
}
