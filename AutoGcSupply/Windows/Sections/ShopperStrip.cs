using AutoGcSupply.Core.Localization;
using AutoGcSupply.Core.Tasks;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoGcSupply.Windows.Sections;

internal static class ShopperStrip
{
    private const float LineHeight = 26f;
    private const float DotRadius = 3.5f;
    private const float DotGap = 10f;
    private const float LabelGap = 8f;
    private const float ItemGap = 10f;
    private const float PricingGap = 18f;
    private const float SpinnerRadius = 5f;
    private const float SpinnerThickness = 2f;
    private const float PillHeight = 24f;

    public static bool Draw(GcRunController controller, TodayModel today)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var height = LineHeight * scale;
        var midY = origin.Y + height * 0.5f;
        var drawList = ImGui.GetWindowDrawList();
        var accent = today.Shopper switch
        {
            ShopperState.Available => Styling.AccentMint,
            ShopperState.Outdated or ShopperState.Newer => Styling.AccentAmber,
            _ => Styling.AccentRose,
        };

        var radius = DotRadius * scale;
        Paint.Dot(drawList, new Vector2(origin.X + radius + 1f * scale, midY), radius, accent);
        var x = origin.X + radius * 2f + DotGap * scale;
        var label = Loc.T(L.Requests.ShopperLabel);
        var labelSize = TextDraw.SmallCapsSize(label);
        TextDraw.SmallCaps(label, new Vector2(x, midY - labelSize.Y * 0.5f), Styling.TextMuted);
        x += labelSize.X + LabelGap * scale;

        var pricingWidth = PricingWidth(today.Pricing);
        var right = origin.X + width - (pricingWidth > 0f ? pricingWidth + PricingGap * scale : 0f);
        var openPlugins = today.Shopper switch
        {
            ShopperState.Available => DrawAvailable(controller, today.ShopperDryRun, x, right, midY),
            ShopperState.Outdated => DrawOutdated(x, right, midY),
            ShopperState.Newer => DrawNewer(x, right, midY),
            _ => DrawMissing(x, right, midY),
        };

        if (pricingWidth > 0f)
        {
            DrawPricing(controller, today.Pricing, origin.X + width - pricingWidth, midY);
        }

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, height));
        return openPlugins;
    }

    private static bool DrawMissing(float x, float right, float midY)
    {
        var label = Loc.T(L.Requests.ShopperInstall);
        var clicked = Pill("##agcs_shopper_install", label, Styling.AccentRose, PillButton.Emphasis.Tinted, FontAwesomeIcon.Plug, x, midY, null);
        var textX = x + PillButton.Width(label, FontAwesomeIcon.Plug) + ItemGap * ImGuiHelpers.GlobalScale;
        Caption(Loc.T(L.Requests.ShopperMissing), textX, right, midY, Styling.TextDim);
        return clicked;
    }

    private static bool DrawOutdated(float x, float right, float midY)
    {
        var label = Loc.T(L.Requests.ShopperUpdate);
        var clicked = Pill("##agcs_shopper_update", label, Styling.AccentAmber, PillButton.Emphasis.Tinted, FontAwesomeIcon.Plug, x, midY, Loc.T(L.Requests.ShopperUpdateHelp));
        var textX = x + PillButton.Width(label, FontAwesomeIcon.Plug) + ItemGap * ImGuiHelpers.GlobalScale;
        Caption(Loc.T(L.Requests.ShopperOutdated), textX, right, midY, Styling.AccentAmberSoft);
        return clicked;
    }

    private static bool DrawNewer(float x, float right, float midY)
    {
        Caption(Loc.T(L.Requests.ShopperNewer), x, right, midY, Styling.AccentAmberSoft);
        return false;
    }

    private static bool DrawAvailable(GcRunController controller, bool dryRun, float x, float right, float midY)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var drawList = ImGui.GetWindowDrawList();
        var chip = Loc.T(dryRun ? L.Requests.DryRunOn : L.Requests.DryRunOff);
        var chipColor = dryRun ? Styling.AccentBlue : Styling.AccentAmber;
        var chipWidth = Badge.DrawLeft(drawList, chip, chipColor, x, midY);
        if (Hit.HoveringRect(new Vector2(x, midY - LineHeight * scale * 0.5f), new Vector2(x + chipWidth, midY + LineHeight * scale * 0.5f)))
        {
            Tooltip.Show(Loc.T(dryRun ? L.Requests.DryRunOnHelp : L.Requests.DryRunOffHelp));
        }

        x += chipWidth + ItemGap * scale;
        var change = Loc.T(L.Requests.ChangeInShopper);
        if (Pill("##agcs_shopper_change", change, Styling.AccentGlow, PillButton.Emphasis.Ghost, FontAwesomeIcon.ExternalLinkAlt, x, midY, null))
        {
            controller.OpenAutoShopper();
        }

        x += PillButton.Width(change, FontAwesomeIcon.ExternalLinkAlt) + ItemGap * scale;
        Caption(Loc.T(L.Requests.ShopperReady), x, right, midY, Styling.TextDim);
        return false;
    }

    private static float PricingWidth(PricingState pricing)
    {
        var scale = ImGuiHelpers.GlobalScale;
        using (Fonts.PushCaption())
        {
            return pricing switch
            {
                PricingState.Pending => (SpinnerRadius * 2f + 8f) * scale + TextDraw.Measure(Loc.T(L.Requests.PricingPending)).X,
                PricingState.Ready => (DotRadius * 2f + 8f) * scale + TextDraw.Measure(Loc.T(L.Requests.PricingReady)).X,
                PricingState.Failed => TextDraw.Measure(Loc.T(L.Requests.PricingFailed)).X + ItemGap * scale + PillButton.Width(Loc.T(L.Requests.PricingRetry), FontAwesomeIcon.Redo),
                _ => 0f,
            };
        }
    }

    private static void DrawPricing(GcRunController controller, PricingState pricing, float x, float midY)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var drawList = ImGui.GetWindowDrawList();
        var start = x;
        switch (pricing)
        {
            case PricingState.Pending:
            {
                var radius = SpinnerRadius * scale;
                var center = new Vector2(x + radius, midY);
                ProgressRing.Track(center, radius, SpinnerThickness * scale, Styling.WithAlpha(Styling.BorderDim, 0.7f));
                ProgressRing.Sweep(center, radius, SpinnerThickness * scale, Styling.AccentBlue, Styling.PulseCalm, MathF.PI * 0.9f, 1f);
                x += radius * 2f + 8f * scale;
                Caption(Loc.T(L.Requests.PricingPending), x, float.MaxValue, midY, Styling.AccentBlueSoft);
                HoverHelp(start, midY, L.Requests.PricingPendingHelp);
                return;
            }
            case PricingState.Ready:
            {
                var radius = DotRadius * scale;
                Paint.Dot(drawList, new Vector2(x + radius, midY), radius, Styling.AccentMint, 0.12f);
                x += radius * 2f + 8f * scale;
                Caption(Loc.T(L.Requests.PricingReady), x, float.MaxValue, midY, Styling.TextDim);
                HoverHelp(start, midY, L.Requests.PricingReadyHelp);
                return;
            }
            case PricingState.Failed:
            {
                var failed = Loc.T(L.Requests.PricingFailed);
                Caption(failed, x, float.MaxValue, midY, Styling.AccentRoseSoft);
                HoverHelp(start, midY, L.Requests.PricingFailedHelp);
                using (Fonts.PushCaption())
                {
                    x += TextDraw.Measure(failed).X + ItemGap * scale;
                }

                if (Pill("##agcs_pricing_retry", Loc.T(L.Requests.PricingRetry), Styling.AccentRose, PillButton.Emphasis.Ghost, FontAwesomeIcon.Redo, x, midY, null))
                {
                    controller.RetryPrices();
                }

                return;
            }
        }
    }

    private static void HoverHelp(float x, float midY, LocString help)
    {
        var half = LineHeight * ImGuiHelpers.GlobalScale * 0.5f;
        if (Hit.HoveringRect(new Vector2(x, midY - half), new Vector2(ImGui.GetWindowPos().X + ImGui.GetWindowWidth(), midY + half)))
        {
            Tooltip.Show(Loc.T(help));
        }
    }

    private static bool Pill(string id, string label, Vector4 accent, PillButton.Emphasis emphasis, FontAwesomeIcon icon, float x, float midY, string? tooltip)
    {
        ImGui.SetCursorScreenPos(new Vector2(x, midY - PillHeight * ImGuiHelpers.GlobalScale * 0.5f));
        return PillButton.Draw(id, label, accent, emphasis, icon, height: PillHeight, tooltip: tooltip);
    }

    private static void Caption(string text, float x, float right, float midY, Vector4 color)
    {
        using (Fonts.PushCaption())
        {
            var shown = TextDraw.Truncate(text, right - x);
            if (shown.Length == 0)
            {
                return;
            }

            TextDraw.At(shown, new Vector2(x, midY - ImGui.GetTextLineHeight() * 0.5f), color);
        }
    }
}
