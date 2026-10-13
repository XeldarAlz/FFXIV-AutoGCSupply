namespace AutoGcSupply.Core.Shopper.Model;

internal static class ShopperNames<TEnum> where TEnum : struct, Enum
{
    private static readonly string[] names = Enum.GetNames<TEnum>();
    private static readonly TEnum[] values = Enum.GetValues<TEnum>();

    public static bool TryParse(string? name, out TEnum value)
    {
        for (var index = 0; index < names.Length; index++)
        {
            if (string.Equals(names[index], name, StringComparison.OrdinalIgnoreCase))
            {
                value = values[index];
                return true;
            }
        }

        value = default;
        return false;
    }

    public static TEnum ParseOr(string? name, TEnum unrecognized)
    {
        return TryParse(name, out var value) ? value : unrecognized;
    }

    public static TEnum ParseOptional(string? name, TEnum absent, TEnum unrecognized)
    {
        if (name is null)
        {
            return absent;
        }

        return ParseOr(name, unrecognized);
    }
}
