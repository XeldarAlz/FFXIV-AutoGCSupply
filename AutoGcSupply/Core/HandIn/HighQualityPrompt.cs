using Dalamud.Game;
using ECommons.DalamudServices;
using Lumina.Excel.Sheets;

namespace AutoGcSupply.Core.HandIn;

internal static class HighQualityPrompt
{
    private const uint TradeHighQualityItemRow = 102434;

    private static readonly LanguageCachedText text = new(Resolve);

    public static string Text() => text.Get();

    private static string Resolve(ClientLanguage language)
    {
        var resolved = Svc.Data.GetExcelSheet<Addon>(language).GetRowOrDefault(TradeHighQualityItemRow)?.Text.ExtractText() ?? string.Empty;
        if (resolved.Length == 0)
        {
            RunLog.Warning($"Hand-in: Addon row {TradeHighQualityItemRow} is empty ({language}); no high-quality confirmation will be accepted");
        }

        return resolved;
    }
}
