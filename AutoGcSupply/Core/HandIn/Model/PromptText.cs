using System.Text;

namespace AutoGcSupply.Core.HandIn.Model;

internal static class PromptText
{
    private const char SoftHyphen = '\u00AD';
    private const char ZeroWidthSpace = '\u200B';
    private const char FirstGameGlyph = '\uE000';
    private const char LastGameGlyph = '\uF8FF';
    private const char Space = ' ';

    public static string Normalize(string text)
    {
        var builder = new StringBuilder(text.Length);
        var pendingSpace = false;
        for (var characterIndex = 0; characterIndex < text.Length; characterIndex++)
        {
            var character = text[characterIndex];
            if (character is SoftHyphen or ZeroWidthSpace or (>= FirstGameGlyph and <= LastGameGlyph))
            {
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(Space);
                pendingSpace = false;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    public static bool Same(string shown, string expected)
    {
        var normalizedExpected = Normalize(expected);
        return normalizedExpected.Length > 0
            && string.Equals(Normalize(shown), normalizedExpected, StringComparison.OrdinalIgnoreCase);
    }
}
