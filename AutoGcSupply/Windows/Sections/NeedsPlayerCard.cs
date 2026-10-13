using AutoGcSupply.Core.Localization;
using AutoGcSupply.Core.Tasks;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoGcSupply.Windows.Sections;

internal static class NeedsPlayerCard
{
    private const float PadX = 20f;
    private const float PadY = 16f;
    private const float LineGap = 6f;
    private const float ButtonGap = 8f;

    private struct Card
    {
        public Vector2 Origin;
        public float Width;
        public float Height;
        public float ButtonX;
        public float ButtonY;
    }

    public static void Draw(GcRunController controller, RunModel run)
    {
        var reason = run.NeedsPlayer;
        var shopperOwned = ShopperSettlementRule.OwnedByShopper(reason);
        var detail = run.NeedsPlayerDetail.Length > 0 ? run.NeedsPlayerDetail : null;
        var (title, body, facts) = reason switch
        {
            NeedsPlayerReason.ShopperConsent => (Loc.T(L.Running.ConsentTitle), Loc.T(L.Running.ConsentBody), detail),
            NeedsPlayerReason.ShopperUncertainPurchase => (Loc.T(L.Running.UncertainTitle), Loc.T(L.Running.UncertainBody), detail),
            NeedsPlayerReason.ShopperInterrupted => (Loc.T(L.Running.InterruptedTitle), Loc.T(L.Running.InterruptedBody), detail),
            NeedsPlayerReason.ShopperRefused or NeedsPlayerReason.ShopperNotReady => (Loc.T(L.Running.RefusedTitle), detail ?? Loc.T(L.Running.NeedsPlayerFallback), null),
            NeedsPlayerReason.HandInPaused => (Loc.T(L.Running.HandInPausedTitle), detail ?? Loc.T(L.Running.NeedsPlayerFallback), null),
            _ => (Loc.T(L.Running.UnconfirmedTitle), detail ?? Loc.T(L.Running.NeedsPlayerFallback), null),
        };

        var accent = reason is NeedsPlayerReason.DeliveryUnconfirmed or NeedsPlayerReason.ShopperUncertainPurchase ? Styling.AccentRose : Styling.AccentAmber;
        var resume = controller.Paused && controller.PauseReason != PauseReason.InContent;
        var stop = controller.Running;
        var buttons = (shopperOwned ? 1 : 0) + (resume ? 1 : 0) + (stop ? 1 : 0);
        var card = BeginCard(title, body, facts, accent, buttons);
        if (shopperOwned && ActionButton(ref card, "##agcs_needs_open_shopper", Loc.T(L.Running.OpenShopper), Styling.AccentGlow, PillButton.Emphasis.Filled, FontAwesomeIcon.ExternalLinkAlt))
        {
            controller.OpenAutoShopper();
        }

        if (resume && ActionButton(ref card, "##agcs_needs_resume", Loc.T(L.Common.Resume), Styling.AccentMint, PillButton.Emphasis.Filled, FontAwesomeIcon.Play))
        {
            controller.Resume();
        }

        if (stop && ActionButton(ref card, "##agcs_needs_stop", Loc.T(L.Common.StopRun), Styling.AccentRose, PillButton.Emphasis.Ghost, FontAwesomeIcon.Stop))
        {
            controller.Stop();
        }

        EndCard(card);
    }

    private static Card BeginCard(string title, string body, string? facts, Vector4 accent, int buttons)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var padX = PadX * scale;
        var padY = PadY * scale;
        var iconWidth = TextDraw.IconSize(FontAwesomeIcon.ExclamationTriangle).X + 10f * scale;
        var wrapWidth = MathF.Max(1f, width - padX * 2f - iconWidth);
        var titleHeight = ImGui.GetTextLineHeight();
        var bodyHeight = TextDraw.MeasureWrapped(body, wrapWidth).Y;
        var factsHeight = facts is null ? 0f : TextDraw.MeasureWrapped(facts, wrapWidth).Y + LineGap * scale;
        var buttonsHeight = buttons > 0 ? Layout.ActionPillHeight * scale + LineGap * 2f * scale : 0f;
        var height = padY * 2f + titleHeight + LineGap * scale + bodyHeight + factsHeight + buttonsHeight;
        var end = origin + new Vector2(width, height);
        var drawList = ImGui.GetWindowDrawList();
        Paint.Surface(drawList, origin, end, Styling.CardRounding * scale, Styling.Tint(Styling.WithAlpha(Styling.Surface1, 0.6f), accent, 0.10f), Styling.WithAlpha(accent, 0.45f), topLight: false);
        TextDraw.Icon(FontAwesomeIcon.ExclamationTriangle, new Vector2(origin.X + padX, origin.Y + padY), accent);

        var x = origin.X + padX + iconWidth;
        var y = origin.Y + padY;
        TextDraw.At(TextDraw.Truncate(title, wrapWidth), new Vector2(x, y), Styling.TextStrong);
        y += titleHeight + LineGap * scale;
        TextDraw.Wrapped(body, new Vector2(x, y), wrapWidth, Styling.TextSecondary);
        y += bodyHeight;
        if (facts is not null)
        {
            y += LineGap * scale;
            TextDraw.Wrapped(facts, new Vector2(x, y), wrapWidth, Styling.TextStrong);
            y += factsHeight - LineGap * scale;
        }

        return new Card
        {
            Origin = origin,
            Width = width,
            Height = height,
            ButtonX = x,
            ButtonY = y + LineGap * 2f * scale,
        };
    }

    private static bool ActionButton(ref Card card, string id, string label, Vector4 accent, PillButton.Emphasis emphasis, FontAwesomeIcon icon)
    {
        ImGui.SetCursorScreenPos(new Vector2(card.ButtonX, card.ButtonY));
        var clicked = PillButton.Draw(id, label, accent, emphasis, icon, true, Layout.ActionPillHeight);
        card.ButtonX += PillButton.Width(label, icon) + ButtonGap * ImGuiHelpers.GlobalScale;
        return clicked;
    }

    private static void EndCard(in Card card)
    {
        ImGui.SetCursorScreenPos(card.Origin);
        ImGui.Dummy(new Vector2(card.Width, card.Height));
    }
}
