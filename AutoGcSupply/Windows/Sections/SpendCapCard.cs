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

    private static readonly GilField.Buffer buffer = new();
    private static CachedText statusText;
    private static CachedText suggestionText;

    public static void Draw(GcRunController controller, TodayModel today)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(ImGui.GetContentRegionAvail().X, CardHeight * scale);
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var drawList = ImGui.GetWindowDrawList();
        var cap = today.SpendCap;
        var (accent, statusColor) = cap <= 0 ? (Styling.AccentRose, Styling.AccentRoseSoft)
            : cap >= today.Totals.EstimatedSpend ? (Styling.AccentMint, Styling.AccentMintSoft)
            : (Styling.AccentAmber, Styling.AccentAmberSoft);
        Paint.Glass(drawList, origin, end, Styling.CardRounding * scale, accent, 0.04f);

        var padX = PadX * scale;
        var midY = origin.Y + size.Y * 0.5f;
        var iconBox = IconBox * scale;
        var iconCenter = new Vector2(origin.X + padX + iconBox * 0.5f, midY);
        drawList.AddCircleFilled(iconCenter, iconBox * 0.5f, Paint.Col(Styling.WithAlpha(accent, 0.14f)));
        TextDraw.IconCentered(FontAwesomeIcon.Coins, iconCenter, Styling.Lighten(accent, 0.3f));

        var fieldWidth = FieldWidth * scale;
        var fieldX = end.X - padX - fieldWidth;
        ImGui.SetCursorScreenPos(new Vector2(fieldX, midY - ImGui.GetFrameHeight() * 0.5f));
        if (GilField.Draw(FieldId, buffer, cap, Configuration.MaximumRunSpendCap, Loc.T(L.Requests.SpendCapHint), FieldWidth, out var value, Loc.T(L.Requests.SpendCapHelp)))
        {
            controller.SetSpendCap(value);
        }

        var suggest = Loc.T(L.Requests.UseSuggested);
        var suggestWidth = PillButton.Width(suggest, FontAwesomeIcon.Magic);
        var suggestX = fieldX - ButtonGap * scale - suggestWidth;
        var showSuggest = today.SuggestedSpendCap > 0;
        if (showSuggest)
        {
            ImGui.SetCursorScreenPos(new Vector2(suggestX, midY - Layout.ActionPillHeight * scale * 0.5f));
            var help = suggestionText.Get(today.SuggestedSpendCap, static gil => Loc.T(L.Requests.UseSuggestedHelp, Formatting.Gil(gil)));
            if (PillButton.Draw("##agcs_today_suggest_cap", suggest, Styling.AccentGlow, PillButton.Emphasis.Tinted, FontAwesomeIcon.Magic,
                    height: Layout.ActionPillHeight, tooltip: help))
            {
                controller.UseSuggestedSpendCap();
            }
        }

        var contentX = iconCenter.X + iconBox * 0.5f + 14f * scale;
        var contentRight = (showSuggest ? suggestX : fieldX) - 14f * scale;
        var contentWidth = MathF.Max(0f, contentRight - contentX);
        var lineHeight = ImGui.GetTextLineHeight();
        float captionHeight;
        using (Fonts.PushCaption())
        {
            captionHeight = ImGui.GetTextLineHeight();
        }

        var top = midY - (lineHeight + LineGap * scale + captionHeight) * 0.5f;
        var title = Loc.T(L.Requests.SpendCap);
        TextDraw.At(TextDraw.Truncate(title, contentWidth), new Vector2(contentX, top), Styling.TextStrong);
        var status = statusText.Get(HashCode.Combine(today.Revision, cap), static _ => Status(Plugin.Instance.Controller.Today));
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

    private static string Status(TodayModel today)
    {
        if (today.SpendCap <= 0)
        {
            return Loc.T(L.Requests.SpendCapMissing);
        }

        var estimate = Formatting.Gil(today.Totals.EstimatedSpend);
        return today.SpendCap >= today.Totals.EstimatedSpend ? Loc.T(L.Requests.SpendCapCovers, estimate) : Loc.T(L.Requests.SpendCapBelow, estimate);
    }
}
