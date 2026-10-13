using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Tasks.Model;
using ECommons.DalamudServices;
using Lumina.Excel.Sheets;

namespace AutoGcSupply.Core.Tasks;

internal sealed class RowLabels : IRowLabels
{
    private readonly Dictionary<byte, string> classNames = [];
    private readonly Dictionary<uint, ItemLabel> items = [];

    public string ClassName(byte classJobId)
    {
        if (classNames.TryGetValue(classJobId, out var cached))
        {
            return cached;
        }

        var name = Svc.Data.GetExcelSheet<ClassJob>().GetRowOrDefault(classJobId)?.Name.ExtractText() ?? string.Empty;
        var label = name.Length == 0 ? SupplyClasses.Abbreviation(classJobId) : string.Concat(char.ToUpperInvariant(name[0]).ToString(), name.AsSpan(1));
        classNames[classJobId] = label;
        return label;
    }

    public string ItemName(uint itemId) => Item(itemId).Name;

    public uint ItemIconId(uint itemId) => Item(itemId).IconId;

    private ItemLabel Item(uint itemId)
    {
        if (items.TryGetValue(itemId, out var cached))
        {
            return cached;
        }

        var label = Svc.Data.GetExcelSheet<Item>().GetRowOrDefault(itemId) is { } item
            ? new ItemLabel(item.Name.ExtractText(), item.Icon)
            : new ItemLabel(string.Empty, 0);
        items[itemId] = label;
        return label;
    }

    private readonly record struct ItemLabel(string Name, uint IconId);
}
