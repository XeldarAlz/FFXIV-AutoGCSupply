using AutoGcSupply.Core.HandIn.Model;
using AutoGcSupply.Core.Inventory.Model;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace AutoGcSupply.Core.HandIn;

internal static unsafe class HandInReads
{
    public static byte GrandCompany() => PlayerState.Instance()->GrandCompany;

    public static int Seals(byte grandCompany) => (int)InventoryManager.Instance()->GetCompanySeals(grandCompany);

    public static string ItemName(uint itemId)
        => Svc.Data.GetExcelSheet<Item>().GetRowOrDefault(itemId)?.Name.ExtractText() is { Length: > 0 } name ? name : $"item {itemId}";

    public static string ClassName(byte classJobId)
        => Svc.Data.GetExcelSheet<ClassJob>().GetRowOrDefault(classJobId)?.Abbreviation.ExtractText() is { Length: > 0 } name ? name : $"class {classJobId}";

    public static string Describe(in HandInTarget target)
        => $"{ItemName(target.ItemId)} x{target.Requested} ({(target.Quality == ItemQuality.High ? "HQ" : "NQ")}) for {ClassName(target.ClassJobId)}";
}
