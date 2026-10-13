using Dalamud.Game;
using ECommons.DalamudServices;
using ECommons.ExcelServices.Sheets;

namespace AutoGcSupply.Core.HandIn;

internal static class OfficerMenuText
{
    private const string SheetName = "custom/000/ComDefGrandCompanyOfficer_00073";
    private const string SupplyOptionKeySuffix = "_A4_002";
    private const uint RecoveredSupplyOptionRow = 69;

    private static readonly LanguageCachedText supplyOption = new(ResolveSupplyOption);

    public static string SupplyOption() => supplyOption.Get();

    private static string ResolveSupplyOption(ClientLanguage language)
    {
        Lumina.Excel.ExcelSheet<QuestDialogueText> sheet;
        try
        {
            sheet = Svc.Data.GetExcelSheet<QuestDialogueText>(language, SheetName);
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, $"Hand-in: the officer's menu sheet {SheetName} could not be read ({language})");
            return string.Empty;
        }

        for (var rowIndex = 0; rowIndex < sheet.Count; rowIndex++)
        {
            var row = sheet.GetRowAt(rowIndex);
            if (!row.Key.ExtractText().EndsWith(SupplyOptionKeySuffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = row.Value.ExtractText().Trim();
            RunLog.Info($"Hand-in: the officer's supply option reads '{text}' (row {row.RowId}, {language})");
            return text;
        }

        if (sheet.GetRowOrDefault(RecoveredSupplyOptionRow) is { } fallback)
        {
            var text = fallback.Value.ExtractText().Trim();
            RunLog.Warning($"Hand-in: no key ending in {SupplyOptionKeySuffix} in {SheetName}; using row {RecoveredSupplyOptionRow}, '{text}' ({language})");
            return text;
        }

        RunLog.Warning($"Hand-in: the officer's supply option was not found in {SheetName} ({language})");
        return string.Empty;
    }
}
