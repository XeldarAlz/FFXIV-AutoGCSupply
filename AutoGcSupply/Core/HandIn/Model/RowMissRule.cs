namespace AutoGcSupply.Core.HandIn.Model;

internal static class RowMissRule
{
    public static RowMissAction Decide(RowMatchStatus status, bool withinLoadWait, bool listSettling) => status switch
    {
        RowMatchStatus.ListNotLoaded => withinLoadWait ? RowMissAction.Retry : RowMissAction.Halt,
        RowMatchStatus.NotListed
            or RowMatchStatus.ClassMismatch
            or RowMatchStatus.DuplicateRow
            or RowMatchStatus.RequestChanged
            or RowMatchStatus.AgentMissing
            or RowMatchStatus.Closed
            or RowMatchStatus.NotEnoughInBags
            or RowMatchStatus.AgentDisagrees => listSettling ? RowMissAction.Retry : RowMissAction.Skip,
        _ => RowMissAction.Skip,
    };
}
