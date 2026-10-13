using AutoGcSupply.Core.HandIn.Model;
using ECommons;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Text;

namespace AutoGcSupply.Core.HandIn;

internal static unsafe class SupplyListReader
{
    public const string AddonName = "GrandCompanySupplyList";

    private const int UnreadableValue = -1;
    private const int LoadedMarkerIndex = 0;
    private const int LoadedMarker = 2;
    private const int TabIndex = 5;
    private const int RowCountIndex = 6;
    private const int StarredBase = 25;
    private const int ClassIconBase = 65;
    private const int RequestedBase = 385;
    private const int ItemIdBase = 425;
    private const int MaxRows = 40;
    private const int ClassIconOffset = 62000;
    private const int LastClassIcon = ClassIconOffset + byte.MaxValue;
    private const int SupplyAndProvisioningEntries = 11;
    private const int MaxAgentEntries = 512;

    public static bool IsOpen() => TryGetList(out _);

    public static bool IsLoaded() => TryReadHeader(out _, out _) && AgentSupplyEntries() >= SupplyAndProvisioningEntries;

    public static bool ShowsTab(SupplyListTab tab) => IsLoaded() && TryReadHeader(out var shown, out _) && shown == tab;

    public static SupplyListState Read()
    {
        if (!TryReadHeader(out var tab, out var list))
        {
            return SupplyListState.NotLoaded;
        }

        var agentItems = ReadAgentItems();
        if (agentItems.Length < SupplyAndProvisioningEntries)
        {
            return SupplyListState.NotLoaded;
        }

        var unit = &list->AtkUnitBase;
        var values = unit->AtkValues;
        var valueCount = (int)unit->AtkValuesCount;
        var rowCount = Math.Clamp(ReadInteger(values[RowCountIndex]), 0, MaxRows);
        var readable = Math.Clamp(valueCount - ItemIdBase, 0, rowCount);
        var rows = new SupplyListRow[readable];
        for (var rowIndex = 0; rowIndex < readable; rowIndex++)
        {
            rows[rowIndex] = new SupplyListRow(
                rowIndex,
                (uint)Math.Max(0, ReadInteger(values[ItemIdBase + rowIndex])),
                ClassJobFromIcon(ReadInteger(values[ClassIconBase + rowIndex])),
                ReadInteger(values[RequestedBase + rowIndex]),
                ReadInteger(values[StarredBase + rowIndex]) > 0);
        }

        return new SupplyListState(true, tab, rows, agentItems);
    }

    public static string DescribeLayout(in SupplyListState state)
    {
        var builder = new StringBuilder();
        builder.Append($"{state.Tab} tab, {state.Rows.Length} rows");
        if (TryGetList(out var list))
        {
            builder.Append($", sort {list->SelectedSortBy}, filter {list->SelectedFilter}");
        }

        for (var rowPosition = 0; rowPosition < state.Rows.Length; rowPosition++)
        {
            var row = state.Rows[rowPosition];
            builder.Append($"; row {row.RowIndex}: {HandInReads.ItemName(row.ItemId)} ({row.ItemId}) {HandInReads.ClassName(row.ClassJobId)} x{row.Requested}{(row.Starred ? " starred" : string.Empty)}");
        }

        for (var itemPosition = 0; itemPosition < state.AgentItems.Length; itemPosition++)
        {
            var item = state.AgentItems[itemPosition];
            builder.Append($"; agent {item.Position}: {item.ItemId} possessed {item.NumPossessed}/{item.NumRequested}{(item.TurnInAvailable ? string.Empty : " closed")}");
        }

        return builder.ToString();
    }

    private static bool TryGetList(out AddonGrandCompanySupplyList* list)
        => GenericHelpers.TryGetAddonByName(AddonName, out list) && GenericHelpers.IsAddonReady(&list->AtkUnitBase);

    private static bool TryReadHeader(out SupplyListTab tab, out AddonGrandCompanySupplyList* list)
    {
        tab = SupplyListTab.Supply;
        if (!TryGetList(out list))
        {
            return false;
        }

        var unit = &list->AtkUnitBase;
        if (unit->AtkValues == null || unit->AtkValuesCount <= RowCountIndex)
        {
            return false;
        }

        if (ReadInteger(unit->AtkValues[LoadedMarkerIndex]) != LoadedMarker)
        {
            return false;
        }

        var shownTab = ReadInteger(unit->AtkValues[TabIndex]);
        if (shownTab is not ((int)SupplyListTab.Supply or (int)SupplyListTab.Provisioning) || list->SelectedTab != shownTab)
        {
            return false;
        }

        tab = (SupplyListTab)shownTab;
        return true;
    }

    private static int AgentSupplyEntries()
    {
        var agent = AgentGrandCompanySupply.Instance();
        return agent->IsAgentActive() && agent->ItemArray != null ? agent->NumItems : 0;
    }

    private static SupplyAgentItem[] ReadAgentItems()
    {
        var agent = AgentGrandCompanySupply.Instance();
        if (!agent->IsAgentActive() || agent->ItemArray == null)
        {
            return [];
        }

        var found = new SupplyAgentItem[SupplyAndProvisioningEntries];
        var foundCount = 0;
        var entries = Math.Min(agent->NumItems, MaxAgentEntries);
        for (var entryIndex = 0; entryIndex < entries && foundCount < found.Length; entryIndex++)
        {
            ref var entry = ref agent->ItemArray[entryIndex];
            if (entry.Position is < 0 or >= SupplyAndProvisioningEntries)
            {
                continue;
            }

            found[foundCount++] = new SupplyAgentItem(entry.ItemId, entry.Position, entry.NumPossessed, entry.NumRequested, entry.IsTurnInAvailable);
        }

        return foundCount == found.Length ? found : found[..foundCount];
    }

    private static byte ClassJobFromIcon(int icon)
        => icon is > ClassIconOffset and <= LastClassIcon ? (byte)(icon - ClassIconOffset) : (byte)0;

    private static int ReadInteger(in AtkValue value) => value.Type switch
    {
        AtkValueType.Int => value.Int,
        AtkValueType.UInt => (int)Math.Min(value.UInt, int.MaxValue),
        AtkValueType.Bool => value.Byte,
        _ => UnreadableValue,
    };
}
