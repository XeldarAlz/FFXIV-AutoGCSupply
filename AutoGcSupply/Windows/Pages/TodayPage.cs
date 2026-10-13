using AutoGcSupply.Core.Localization;
using AutoGcSupply.Windows.Components;
using Dalamud.Interface;

namespace AutoGcSupply.Windows.Pages;

internal sealed class TodayPage
{
    public void Draw()
    {
        PageHeader.Draw(Loc.T(L.Today.Title), Loc.T(L.Today.Subtitle));
        EmptyState.Draw(FontAwesomeIcon.ClipboardList, Loc.T(L.Today.Empty));
    }
}
