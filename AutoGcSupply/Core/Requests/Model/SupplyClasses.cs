namespace AutoGcSupply.Core.Requests.Model;

public static class SupplyClasses
{
    public const byte Carpenter = 8;
    public const byte Blacksmith = 9;
    public const byte Armorer = 10;
    public const byte Goldsmith = 11;
    public const byte Leatherworker = 12;
    public const byte Weaver = 13;
    public const byte Alchemist = 14;
    public const byte Culinarian = 15;
    public const byte Miner = 16;
    public const byte Botanist = 17;
    public const byte Fisher = 18;

    public const byte First = Carpenter;
    public const byte Last = Fisher;
    public const int Count = Last - First + 1;
    public const int SupplyCount = Culinarian - First + 1;
    public const int ProvisioningCount = Last - Miner + 1;

    private const uint ClassIconBase = 62000;

    private static readonly string[] abbreviations = ["CRP", "BSM", "ARM", "GSM", "LTW", "WVR", "ALC", "CUL", "MIN", "BTN", "FSH"];

    public static ReadOnlySpan<byte> DisplayOrder => [Carpenter, Blacksmith, Armorer, Goldsmith, Leatherworker, Weaver, Alchemist, Culinarian, Miner, Botanist, Fisher];

    public static bool Contains(byte classJobId) => classJobId is >= First and <= Last;

    public static SupplyMissionKind KindOf(byte classJobId)
        => classJobId >= Miner ? SupplyMissionKind.Provisioning : SupplyMissionKind.Supply;

    public static int DisplayIndex(byte classJobId) => classJobId - First;

    public static uint IconId(byte classJobId) => ClassIconBase + classJobId;

    public static string Abbreviation(byte classJobId)
        => Contains(classJobId) ? abbreviations[DisplayIndex(classJobId)] : $"#{classJobId}";
}
