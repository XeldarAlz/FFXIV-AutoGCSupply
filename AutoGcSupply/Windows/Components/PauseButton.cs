using AutoGcSupply.Core.Localization;
using AutoGcSupply.Core.Tasks;
using Dalamud.Interface;

namespace AutoGcSupply.Windows.Components;

internal static class PauseButton
{
    public static bool Draw(PauseReason reason, bool canPause, float width = 0f) => reason switch
    {
        PauseReason.InContent => HeroButton.Draw(FontAwesomeIcon.Play, Loc.T(L.Dock.ResumeCaps), null, Styling.AccentMint, false, Loc.T(L.Dock.InContent), width),
        PauseReason.Manual    => HeroButton.Draw(FontAwesomeIcon.Play, Loc.T(L.Dock.ResumeCaps), null, Styling.AccentMint, true, null, width),
        _                     => HeroButton.Draw(FontAwesomeIcon.Pause, Loc.T(L.Dock.PauseCaps), null, Styling.AccentAmber, canPause, Loc.T(L.Automate.PauseInShopper), width),
    };
}
