using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Requests.Model;
using FFXIVClientStructs.FFXIV.Client.Game;
using System.Collections.Immutable;

namespace AutoGcSupply.Core.Inventory;

internal static unsafe class BagCounter
{
    private static readonly InventoryType[] bags =
    [
        InventoryType.Inventory1,
        InventoryType.Inventory2,
        InventoryType.Inventory3,
        InventoryType.Inventory4,
    ];

    public static OwnedCount Count(uint itemId)
    {
        var manager = InventoryManager.Instance();
        var normal = 0;
        var high = 0;
        for (var bagIndex = 0; bagIndex < bags.Length; bagIndex++)
        {
            normal += manager->GetItemCountInContainer(itemId, bags[bagIndex], isHq: false);
            high += manager->GetItemCountInContainer(itemId, bags[bagIndex], isHq: true);
        }

        return new OwnedCount(normal, high);
    }

    public static OwnedCount[] CountBySlot(ImmutableArray<SupplyRequest> requests)
    {
        var counts = new OwnedCount[requests.Length];
        for (var slot = 0; slot < requests.Length; slot++)
        {
            var itemId = requests[slot].ItemId;
            counts[slot] = itemId == 0 ? default : Count(itemId);
        }

        return counts;
    }
}
