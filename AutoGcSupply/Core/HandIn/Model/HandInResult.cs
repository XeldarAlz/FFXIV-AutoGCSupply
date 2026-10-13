namespace AutoGcSupply.Core.HandIn.Model;

internal readonly record struct HandInResult(
    HandInOutcome Outcome,
    HandInPauseReason PauseReason,
    string Detail,
    RewardWindowMismatch RewardWindow,
    HandInRowResult[] Rows,
    int Delivered,
    int SealsGained,
    int SealsLost)
{
    public static HandInResult Running { get; } = new(HandInOutcome.Running, HandInPauseReason.None, string.Empty, RewardWindowMismatch.None, [], 0, 0, 0);

    public bool NeedsPlayer => Outcome == HandInOutcome.Paused;
}
