namespace AutoGcSupply.Core.HandIn.Model;

internal static class RowMatcher
{
    public static RowMatch Evaluate(in SupplyListState list, in HandInTarget target, BagStock stock, int targetIndex)
    {
        if (!target.IsValid)
        {
            return RowMatch.Miss(RowMatchStatus.InvalidTarget, targetIndex);
        }

        if (!list.Loaded)
        {
            return RowMatch.Miss(RowMatchStatus.ListNotLoaded, targetIndex);
        }

        if (target.Tab != list.Tab)
        {
            return RowMatch.Miss(RowMatchStatus.OtherTab, targetIndex);
        }

        var matchingRows = list.CountRows(target.ItemId, target.ClassJobId);
        if (matchingRows == 0)
        {
            return RowMatch.Miss(list.ListsItem(target.ItemId) ? RowMatchStatus.ClassMismatch : RowMatchStatus.NotListed, targetIndex);
        }

        if (matchingRows > 1)
        {
            return RowMatch.Miss(RowMatchStatus.DuplicateRow, targetIndex);
        }

        var row = list.Rows[list.FindRow(target.ItemId, target.ClassJobId)];
        if (row.Requested != target.Requested)
        {
            return RowMatch.Miss(RowMatchStatus.RequestChanged, targetIndex);
        }

        if (!list.TryFindAgentItem(target.ItemId, out var agentItem))
        {
            return RowMatch.Miss(RowMatchStatus.AgentMissing, targetIndex);
        }

        if (!agentItem.TurnInAvailable)
        {
            return RowMatch.Miss(RowMatchStatus.Closed, targetIndex);
        }

        if (stock.Of(target.Quality) < target.Requested)
        {
            return RowMatch.Miss(RowMatchStatus.NotEnoughInBags, targetIndex);
        }

        if (agentItem.NumPossessed < target.Requested)
        {
            return RowMatch.Miss(RowMatchStatus.AgentDisagrees, targetIndex);
        }

        return new RowMatch(RowMatchStatus.Matched, targetIndex, row.RowIndex);
    }

    public static RowMatch FindNext(in SupplyListState list, ReadOnlySpan<HandInTarget> targets, ReadOnlySpan<HandInRowResult> rows, ReadOnlySpan<BagStock> stocks)
    {
        if (!list.Loaded)
        {
            return RowMatch.Miss(RowMatchStatus.ListNotLoaded, RowMatch.NoTarget);
        }

        var firstMiss = RowMatch.Miss(RowMatchStatus.NothingPending, RowMatch.NoTarget);
        for (var targetIndex = 0; targetIndex < targets.Length; targetIndex++)
        {
            if (rows[targetIndex].Status != HandInRowStatus.Pending || targets[targetIndex].Tab != list.Tab)
            {
                continue;
            }

            var match = Evaluate(list, targets[targetIndex], stocks[targetIndex], targetIndex);
            if (match.Found)
            {
                return match;
            }

            if (firstMiss.Status == RowMatchStatus.NothingPending)
            {
                firstMiss = match;
            }
        }

        return firstMiss;
    }
}
