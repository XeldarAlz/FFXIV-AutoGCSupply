using AutoGcSupply.Core.Localization;
using AutoGcSupply.Core.Tasks;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoGcSupply.Windows.Sections;

internal static class SpendCapCard
{
    private const float CardHeight = 64f;
    private const float PadX = 18f;
    private const float IconBox = 34f;
    private const float FieldWidth = 150f;
    private const float ButtonGap = 10f;
    private const float LineGap = 3f;
    private const string FieldId = "##agcs_today_spend_cap";
    private const long CapMissing = 0;
    private const long CapCovers = 1;
    private const long CapBelow = 2;
    private const long VerdictCount = 3;

    private static readonly GilField.Buffer buffer = new();
    private static CachedText statusText;
    private static CachedText suggestionText;

    public static void Draw(GcRunController controller, TodayModel today)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var width = ImGui.GetContentRegionAvail().X;
        var suggest = Loc.T(L.Requests.UseSuggested);
        var suggestWidth = PillButton.Width(suggest, FontAwesomeIcon.Magic);
        var showSuggest = today.SuggestedSpendCap > 0;
        var title = Loc.T(L.Requests.SpendCap);
        var status = Status(today.SpendCap, today.Totals.EstimatedSpend);
        var textWidth = TextDraw.Measure(title).X;
        using (Fonts.PushCaption()) textWidth = MathF.Max(textWidth, TextDraw.Measure(status).X);
        var stacked = width < (PadX * 2f + IconBox + 28f + FieldWidth) * scale + textWidth
            + (showSuggest ? suggestWidth + ButtonGap * scale : 0f);
        var wrapSuggest = stacked && showSuggest && width < (PadX * 2f + FieldWidth + ButtonGap) * scale + suggestWidth;
        var size = new Vector2(width, (stacked ? wrapSuggest ? 148f : 108f : CardHeight) * scale);
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var drawList = ImGui.GetWindowDrawList();
        var cap = today.SpendCap;
        var (accent, statusColor) = cap <= 0 ? (Styling.AccentRose, Styling.AccentRoseSoft)
            : cap >= today.Totals.EstimatedSpend ? (Styling.AccentMint, Styling.AccentMintSoft)
            : (Styling.AccentAmber, Styling.AccentAmberSoft);
        Paint.Glass(drawList, origin, end, Styling.CardRounding * scale, accent, 0.04f);

        var padX = PadX * scale;
        var midY = origin.Y + CardHeight * scale * 0.5f;
        var iconBox = IconBox * scale;
        var iconCenter = new Vector2(origin.X + padX + iconBox * 0.5f, midY);
        drawList.AddCircleFilled(iconCenter, iconBox * 0.5f, Paint.Col(Styling.WithAlpha(accent, 0.14f)));
        TextDraw.IconCentered(FontAwesomeIcon.Coins, iconCenter, Styling.Lighten(accent, 0.3f));

        var fieldWidth = FieldWidth * scale;
        var fieldX = stacked ? origin.X + padX : end.X - padX - fieldWidth;
        var controlMidY = stacked ? origin.Y + 80f * scale : midY;
        ImGui.SetCursorScreenPos(new Vector2(fieldX, controlMidY - ImGui.GetFrameHeight() * 0.5f));
        if (GilField.Draw(FieldId, buffer, cap, Configuration.MaximumRunSpendCap, Loc.T(L.Requests.SpendCapHint), FieldWidth, out var value, Loc.T(L.Requests.SpendCapHelp)))
        {
            controller.SetSpendCap(value);
        }

        var suggestX = stacked ? wrapSuggest ? fieldX : fieldX + fieldWidth + ButtonGap * scale
            : fieldX - ButtonGap * scale - suggestWidth;
        if (showSuggest)
        {
            var suggestMidY = wrapSuggest ? controlMidY + 40f * scale : controlMidY;
            ImGui.SetCursorScreenPos(new Vector2(suggestX, suggestMidY - Layout.ActionPillHeight * scale * 0.5f));
            var help = suggestionText.Get(today.SuggestedSpendCap, static gil => Loc.T(L.Requests.UseSuggestedHelp, Formatting.Gil(gil)));
            if (PillButton.Draw("##agcs_today_suggest_cap", suggest, Styling.AccentGlow, PillButton.Emphasis.Tinted, FontAwesomeIcon.Magic,
                    height: Layout.ActionPillHeight, tooltip: help, maxWidth: end.X - padX - suggestX))
            {
                controller.UseSuggestedSpendCap();
            }
        }

        var contentX = iconCenter.X + iconBox * 0.5f + 14f * scale;
        var contentRight = stacked ? end.X - padX : (showSuggest ? suggestX : fieldX) - 14f * scale;
        var contentWidth = MathF.Max(0f, contentRight - contentX);
        var lineHeight = ImGui.GetTextLineHeight();
        var captionHeight = TextDraw.CaptionLineHeight();

        var top = midY - (lineHeight + LineGap * scale + captionHeight) * 0.5f;
        TextDraw.At(TextDraw.Truncate(title, contentWidth), new Vector2(contentX, top), Styling.TextStrong);
        using (Fonts.PushCaption())
        {
            TextDraw.At(TextDraw.Truncate(status, contentWidth), new Vector2(contentX, top + lineHeight + LineGap * scale), statusColor);
        }

        if (Hit.HoveringRect(new Vector2(contentX, top), new Vector2(contentRight, top + lineHeight)))
        {
            Tooltip.Show(Loc.T(L.Requests.SpendCapHelp));
        }

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(size);
    }

    private static string Status(long cap, long estimate)
    {
        var verdict = cap <= 0 ? CapMissing : cap >= estimate ? CapCovers : CapBelow;
        var key = estimate * VerdictCount + verdict;
        if (statusText.TryGet(key, out var text))
        {
            return text;
        }

        var gil = Formatting.Gil(estimate);
        var built = verdict switch
        {
            CapMissing => Loc.T(L.Requests.SpendCapMissing),
            CapCovers => Loc.T(L.Requests.SpendCapCovers, gil),
            _ => Loc.T(L.Requests.SpendCapBelow, gil),
        };
        return statusText.Set(key, built);
    }
}
