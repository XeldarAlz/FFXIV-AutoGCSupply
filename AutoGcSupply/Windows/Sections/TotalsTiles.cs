using AutoGcSupply.Core.Localization;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Core.Value.Model;
using AutoGcSupply.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoGcSupply.Windows.Sections;

internal static class TotalsTiles
{
    private const int TileCount = 6;
    private const float TileGap = 8f;
    private const float MinimumTileWidth = 128f;
    private const string NoValue = "-";

    private static int textsRevision = -1;
    private static LanguageInfo? textsLanguage;
    private static string selected = string.Empty;
    private static string selectedSub = string.Empty;
    private static string toBuy = string.Empty;
    private static string toHandIn = string.Empty;
    private static string spend = string.Empty;
    private static string experience = string.Empty;
    private static string seals = string.Empty;
    private static string? sealsLost;

    public static void Draw(TodayModel today)
    {
        Refresh(today);
        var scale = ImGuiHelpers.GlobalScale;
        var gap = TileGap * scale;
        var width = ImGui.GetContentRegionAvail().X;
        var perRow = (width - gap * (TileCount - 1)) / TileCount >= MinimumTileWidth * scale ? TileCount : TileCount / 2;
        var tileWidth = (width - gap * (perRow - 1)) / perRow;

        StatTile.Draw(Loc.T(L.Requests.TileSelected), selected, selectedSub, Styling.AccentGlow, tileWidth);
        Next(1, perRow, gap);
        StatTile.Draw(Loc.T(L.Requests.TileToBuy), toBuy, null, Styling.AccentBlue, tileWidth);
        Next(2, perRow, gap);
        StatTile.Draw(Loc.T(L.Requests.TileToHandIn), toHandIn, null, Styling.AccentMint, tileWidth);
        Next(3, perRow, gap);
        StatTile.Draw(Loc.T(L.Requests.TileSpend), spend, null, Styling.AccentAmber, tileWidth);
        Next(4, perRow, gap);
        StatTile.Draw(Loc.T(L.Requests.TileExperience), experience, null, Styling.AccentNebula, tileWidth);
        Next(5, perRow, gap);
        var sealsOrigin = ImGui.GetCursorScreenPos();
        StatTile.Draw(Loc.T(L.Requests.TileSeals), seals, sealsLost, sealsLost is null ? Styling.AccentGlowSoft : Styling.AccentRose, tileWidth);
        if (sealsLost is not null && Hit.HoveringRect(sealsOrigin, sealsOrigin + new Vector2(tileWidth, Layout.StatTileHeight * scale)))
        {
            Tooltip.Show(Loc.T(L.Requests.TileSealsLostHelp));
        }
    }

    private static void Next(int index, int perRow, float gap)
    {
        if (index % perRow == 0)
        {
            return;
        }

        ImGui.SameLine(0f, gap);
    }

    private static void Refresh(TodayModel today)
    {
        if (today.Revision == textsRevision && ReferenceEquals(textsLanguage, Loc.Current))
        {
            return;
        }

        textsRevision = today.Revision;
        textsLanguage = Loc.Current;
        var totals = today.Totals;
        var culture = Loc.Culture;
        selected = totals.Selected.ToString("N0", culture);
        selectedSub = Loc.T(L.Requests.TileSelectedSub, OpenCount(today).ToString("N0", culture));
        toBuy = totals.ToBuy.ToString("N0", culture);
        toHandIn = totals.ToHandIn.ToString("N0", culture);
        spend = totals.ToBuy == 0 ? NoValue : Formatting.Gil(totals.EstimatedSpend);
        experience = totals.Experience.ToString("N0", culture);
        seals = totals.Seals.ToString("N0", culture);
        sealsLost = totals.SealsLost > 0 ? Loc.T(L.Requests.TileSealsLost, totals.SealsLost.ToString("N0", culture)) : null;
    }

    private static int OpenCount(TodayModel today)
    {
        var rows = today.Rows;
        var count = 0;
        for (var index = 0; index < rows.Length; index++)
        {
            if (rows[index].Decision.Coverage != RowCoverage.Delivered)
            {
                count++;
            }
        }

        return count;
    }
}
