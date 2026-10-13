namespace AutoGcSupply.Core.HandIn.Model;

internal enum RowMatchStatus : byte
{
    Matched,
    NothingPending,
    InvalidTarget,
    ListNotLoaded,
    OtherTab,
    NotListed,
    ClassMismatch,
    DuplicateRow,
    RequestChanged,
    AgentMissing,
    Closed,
    NotEnoughInBags,
    AgentDisagrees,
}
