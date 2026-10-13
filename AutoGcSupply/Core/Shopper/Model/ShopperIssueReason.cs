namespace AutoGcSupply.Core.Shopper.Model;

internal enum ShopperIssueReason : byte
{
    Unknown,
    PriceCapExceeded,
    NoStock,
    InventoryFull,
    InsufficientGil,
    TravelFailed,
    DependencyBusy,
    UncertainPurchase,
    UnsupportedGameVersion,
    BudgetReached,
    SearchFailed,
    RetriesExhausted,
    ListingRejected,
    UniqueOwned,
    BoardUnreachable,
    JournalWriteFailed,
    CharacterChanged,
    NotMarketable,
    CrystalLimit,
}
