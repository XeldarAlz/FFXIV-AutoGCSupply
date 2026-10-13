namespace AutoGcSupply.Core.HandIn.Model;

internal readonly record struct RowMatch(RowMatchStatus Status, int TargetIndex, int RowIndex)
{
    public const int NoTarget = -1;

    public bool Found => Status == RowMatchStatus.Matched;

    public static RowMatch Miss(RowMatchStatus status, int targetIndex) => new(status, targetIndex, SupplyListState.NoRow);
}
