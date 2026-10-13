using AutoGcSupply.Core.Localization;
using AutoGcSupply.Core.Tasks;
using AutoGcSupply.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoGcSupply.Windows.Sections;

internal static class DryRunTile
{
    private const string TileId = "##agcs_dry_run_tile";
    private const string TrackId = "##agcs_dry_run_track";
    private const float Rounding = 14f;
    private const float PadX = 14f;
    private const float SwitchGap = 10f;
    private const float LineGap = 2f;
    private const float MaximumRowShare = 0.4f;

    public static float Width(float rowWidth)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var textWidth = TextDraw.Measure(Loc.T(L.Automate.DryRun)).X;
        using (Fonts.PushCaption())
        {
            textWidth = MathF.Max(textWidth, TextDraw.Measure(Loc.T(L.Automate.DryRunOnCaption)).X);
            textWidth = MathF.Max(textWidth, TextDraw.Measure(Loc.T(L.Automate.DryRunOffCaption)).X);
        }

        var natural = (PadX * 2f + SwitchGap) * scale + ToggleSwitch.Size().X + textWidth;
        return MathF.Min(natural, rowWidth * MaximumRowShare);
    }

    public static void Draw(GcRunController controller, bool dryRun, float width, float height)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(width, height);
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var hit = Hit.Area(TileId, size);
        if (hit.Clicked)
        {
            controller.OpenAutoShopper();
        }

        var hover = Motion.Hover(Motion.Key(TileId), hit.Hovered);
        var accent = dryRun ? Styling.AccentBlue : Styling.AccentAmber;
        Paint.Glass(ImGui.GetWindowDrawList(), origin, end, Rounding * scale, accent, 0.08f, hover);

        var padX = PadX * scale;
        var midY = origin.Y + height * 0.5f;
        var switchSize = ToggleSwitch.Size();
        ToggleSwitch.DrawTrack(TrackId, new Vector2(origin.X + padX, midY - switchSize.Y * 0.5f), dryRun, hit.Hovered);

        var textX = origin.X + padX + switchSize.X + SwitchGap * scale;
        var textWidth = MathF.Max(0f, end.X - padX - textX);
        var titleHeight = ImGui.GetTextLineHeight();
        using (Fonts.PushCaption())
        {
            var captionHeight = ImGui.GetTextLineHeight();
            var top = midY - (titleHeight + LineGap * scale + captionHeight) * 0.5f;
            var caption = Loc.T(dryRun ? L.Automate.DryRunOnCaption : L.Automate.DryRunOffCaption);
            TextDraw.At(TextDraw.Truncate(caption, textWidth), new Vector2(textX, top + titleHeight + LineGap * scale), dryRun ? Styling.AccentBlueSoft : Styling.AccentAmberSoft);
        }

        var titleTop = midY - (titleHeight + LineGap * scale + CaptionHeight()) * 0.5f;
        TextDraw.At(TextDraw.Truncate(Loc.T(L.Automate.DryRun), textWidth), new Vector2(textX, titleTop), Styling.TextStrong);
        if (hit.Hovered)
        {
            using (Tooltip.Begin())
            {
                Tooltip.Text(Loc.T(dryRun ? L.Requests.DryRunOnHelp : L.Requests.DryRunOffHelp));
                Tooltip.Text(Loc.T(L.Automate.DryRunTileHelp), Styling.TextDim);
            }
        }
    }

    private static float CaptionHeight()
    {
        using (Fonts.PushCaption())
        {
            return ImGui.GetTextLineHeight();
        }
    }
}
