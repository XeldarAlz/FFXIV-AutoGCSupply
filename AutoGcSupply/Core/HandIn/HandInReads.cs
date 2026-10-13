using AutoGcSupply.Core.HandIn.Model;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace AutoGcSupply.Core.HandIn;

internal static unsafe class HandInReads
{
    private static readonly InventoryType[] bags =
    [
        InventoryType.Inventory1,
        InventoryType.Inventory2,
        InventoryType.Inventory3,
        InventoryType.Inventory4,
    ];

    public static byte GrandCompany() => PlayerState.Instance()->GrandCompany;

    public static int Seals(byte grandCompany) => (int)InventoryManager.Instance()->GetCompanySeals(grandCompany);

    public static int SealCap()
    {
        var rank = PlayerState.Instance()->GetGrandCompanyRank();
        return (int)(Svc.Data.GetExcelSheet<GrandCompanyRank>().GetRowOrDefault(rank)?.MaxSeals ?? SealCapRule.UnknownCap);
    }

    public static BagStock CountInBags(uint itemId)
    {
        var inventory = InventoryManager.Instance();
        var normal = 0;
        var high = 0;
        for (var bagIndex = 0; bagIndex < bags.Length; bagIndex++)
        {
            normal += inventory->GetItemCountInContainer(itemId, bags[bagIndex]);
            high += inventory->GetItemCountInContainer(itemId, bags[bagIndex], true);
        }

        return new BagStock(normal, high);
    }

    public static string ItemName(uint itemId)
        => Svc.Data.GetExcelSheet<Item>().GetRowOrDefault(itemId)?.Name.ExtractText() is { Length: > 0 } name ? name : $"item {itemId}";

    public static string ClassName(byte classJobId)
        => Svc.Data.GetExcelSheet<ClassJob>().GetRowOrDefault(classJobId)?.Abbreviation.ExtractText() is { Length: > 0 } name ? name : $"class {classJobId}";

    public static string Describe(in HandInTarget target)
        => $"{ItemName(target.ItemId)} x{target.Requested} ({(target.Quality == HandInQuality.High ? "HQ" : "NQ")}) for {ClassName(target.ClassJobId)}";
}
