namespace AutoGcSupply.Core.HandIn.Model;

internal readonly record struct SupplyListState(bool Loaded, SupplyListTab Tab, SupplyListRow[] Rows, SupplyAgentItem[] AgentItems)
{
    public const int NoRow = -1;

    public static SupplyListState NotLoaded { get; } = new(false, SupplyListTab.Supply, [], []);

    public int CountRows(uint itemId, byte classJobId)
    {
        var count = 0;
        for (var rowPosition = 0; rowPosition < Rows.Length; rowPosition++)
        {
            if (Rows[rowPosition].ItemId == itemId && Rows[rowPosition].ClassJobId == classJobId)
            {
                count++;
            }
        }

        return count;
    }

    public int FindRow(uint itemId, byte classJobId)
    {
        for (var rowPosition = 0; rowPosition < Rows.Length; rowPosition++)
        {
            if (Rows[rowPosition].ItemId == itemId && Rows[rowPosition].ClassJobId == classJobId)
            {
                return rowPosition;
            }
        }

        return NoRow;
    }

    public bool ListsItem(uint itemId)
    {
        for (var rowPosition = 0; rowPosition < Rows.Length; rowPosition++)
        {
            if (Rows[rowPosition].ItemId == itemId)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryFindAgentItem(uint itemId, out SupplyAgentItem agentItem)
    {
        for (var itemPosition = 0; itemPosition < AgentItems.Length; itemPosition++)
        {
            if (AgentItems[itemPosition].ItemId != itemId)
            {
                continue;
            }

            agentItem = AgentItems[itemPosition];
            return true;
        }

        agentItem = default;
        return false;
    }

    public bool RowStillOpen(in HandInTarget target)
    {
        if (!Loaded || Tab != target.Tab)
        {
            return true;
        }

        if (FindRow(target.ItemId, target.ClassJobId) == NoRow)
        {
            return false;
        }

        return !TryFindAgentItem(target.ItemId, out var agentItem) || agentItem.TurnInAvailable;
    }
}
