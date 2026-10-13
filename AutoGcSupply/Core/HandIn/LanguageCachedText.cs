using Dalamud.Game;
using ECommons.DalamudServices;

namespace AutoGcSupply.Core.HandIn;

internal sealed class LanguageCachedText(Func<ClientLanguage, string> resolve)
{
    private ClientLanguage? language;
    private string text = string.Empty;

    public string Get()
    {
        var current = Svc.ClientState.ClientLanguage;
        if (language == current)
        {
            return text;
        }

        text = resolve(current);
        language = current;
        return text;
    }
}
