namespace AutoGcSupply.Core.HandIn.Model;

internal readonly record struct HandInRowResult(HandInRowStatus Status, RowMatchStatus Match, SealCapAction SealCap, int SealsGained, int SealsLost)
{
    public static HandInRowResult Pending { get; } = new(HandInRowStatus.Pending, RowMatchStatus.Matched, SealCapAction.Deliver, 0, 0);
}
