using AutoGcSupply.Core.HandIn.Model;
using AutoGcSupply.Core.Inventory.Model;
using ECommons;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoGcSupply.Core.HandIn;

internal static unsafe class RequestHandover
{
    public const string AddonName = "Request";
    public const string ContextAddonName = "ContextIconMenu";
    private const int ClickThrottleMs = 600;

    public static bool IsOpen() => TryGet(out _);

    public static RequestWindowState Read()
    {
        var agent = AgentNpcTrade.Instance();
        if (!TryGet(out var addon) || agent == null || !agent->IsAgentActive() || agent->AddonId != addon->Id)
        {
            return RequestWindowState.Unreadable;
        }

        ref var requests = ref UIState.Instance()->NpcTrade.Requests;
        if (requests.Count > requests.Items.Length) return RequestWindowState.Unreadable;
        var slots = new RequestSlot[requests.Count];
        for (var index = 0; index < slots.Length; index++)
        {
            ref var item = ref requests.Items[index];
            slots[index] = new RequestSlot(item.ItemId, item.RequiredQuantity, item.WantHQ);
        }

        return new RequestWindowState(true, slots, ReadOffered(), addon->HandOverButton != null && addon->HandOverButton->IsEnabled);
    }

    public static RequestStack[] ReadOffered()
    {
        var inventory = InventoryManager.Instance()->GetInventoryContainer(InventoryType.HandIn);
        if (inventory == null || !inventory->IsLoaded || inventory->Size is < 0 or > 5) return [];
        var result = new List<RequestStack>();
        for (var slot = 0; slot < inventory->Size; slot++)
        {
            var item = inventory->GetInventorySlot(slot);
            if (item == null || item->GetBaseItemId() == 0) continue;
            // HandIn contains symbolic links to bag items. The local quantity is the
            // amount offered, while the virtual accessors resolve the item and quality.
            result.Add(new RequestStack(item->GetBaseItemId(), item->Quantity,
                item->IsHighQuality() ? ItemQuality.High : ItemQuality.Normal));
        }

        return result.ToArray();
    }

    public static bool Fill(in HandInTarget target)
    {
        if (RequestWindowCheck.Judge(Read(), target) != RequestWindowVerdict.NeedsItems
            || !EzThrottler.Throttle("AutoGcSupply.HandIn.FillRequest", ClickThrottleMs)) return false;

        var agent = AgentNpcTrade.Instance();
        if (agent == null || !agent->IsAgentActive()) return false;
        if (agent->SelectedTurnInSlot < 0) agent->SelectTurnInSlot(0);
        if (agent->SelectedTurnInSlot != 0 || agent->SelectedTurnInSlotItemOptions <= 0
            || agent->SelectedTurnInSlotItemOptions > agent->SelectedTurnInSlotItemOptionValues.Length) return false;

        Span<RequestOption> options = stackalloc RequestOption[agent->SelectedTurnInSlotItemOptions];
        options.Clear();
        for (var index = 0; index < options.Length; index++)
        {
            var item = agent->SelectedTurnInSlotItemOptionValues[index].Value;
            if (item == null) continue;
            var inventory = item->GetInventoryType();
            options[index] = new RequestOption(index, item->GetBaseItemId(), item->IsHighQuality() ? ItemQuality.High : ItemQuality.Normal,
                inventory is InventoryType.Inventory1 or InventoryType.Inventory2 or InventoryType.Inventory3 or InventoryType.Inventory4);
        }

        var chosen = RequestWindowCheck.FindOption(options, target);
        if (chosen < 0) return false;
        var result = new AtkValue();
        var values = stackalloc AtkValue[4];
        new Span<AtkValue>(values, 4).Clear();
        values[0].SetInt(0);
        values[1].SetInt(chosen);
        RunLog.Info($"Hand-in: filling Request slot 0 with option {chosen}: {HandInReads.Describe(target)}");
        agent->ReceiveEvent(&result, values, 4, 1);
        return true;
    }

    public static bool Submit(in HandInTarget target)
    {
        if (RequestWindowCheck.Judge(Read(), target) != RequestWindowVerdict.Ready || !TryGet(out var addon)
            || !EzThrottler.Throttle("AutoGcSupply.HandIn.SubmitRequest", ClickThrottleMs)) return false;

        RunLog.Info($"Hand-in: pressing Hand Over for {HandInReads.Describe(target)}");
        new AddonMaster.Request(addon).HandOver();
        return true;
    }

    public static bool Cancel()
    {
        if (!TryGet(out var addon) || addon->CancelButton == null || !addon->CancelButton->IsEnabled
            || !EzThrottler.Throttle("AutoGcSupply.HandIn.CancelRequest", ClickThrottleMs)) return false;
        new AddonMaster.Request(addon).Cancel();
        return true;
    }

    public static string Describe(in RequestWindowState state)
    {
        var inventory = InventoryManager.Instance()->GetInventoryContainer(InventoryType.HandIn);
        var inventoryState = inventory == null ? "missing" : $"size {inventory->Size}, loaded {inventory->IsLoaded}";
        var agent = AgentNpcTrade.Instance();
        var selection = agent == null ? "no agent" : $"slot {agent->SelectedTurnInSlot}, {agent->SelectedTurnInSlotItemOptions} options";
        return $"readable {state.Readable}, slots [{string.Join(", ", state.Slots.Select(x => $"{x.ItemId} x{x.Quantity}"))}], "
            + $"offered [{string.Join(", ", state.Offered.Select(x => $"{x.ItemId} x{x.Quantity} {x.Quality}"))}], Hand Over enabled {state.HandOverEnabled}; "
            + $"HandIn inventory {inventoryState}; selection {selection}";
    }

    private static bool TryGet(out AddonRequest* addon)
        => GenericHelpers.TryGetAddonByName(AddonName, out addon) && GenericHelpers.IsAddonReady(&addon->AtkUnitBase);
}
