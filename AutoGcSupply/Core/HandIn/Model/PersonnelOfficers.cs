using System.Numerics;

namespace AutoGcSupply.Core.HandIn.Model;

internal static class PersonnelOfficers
{
    private const byte Maelstrom = 1;
    private const byte TwinAdder = 2;
    private const byte ImmortalFlames = 3;

    private const uint LimsaLominsaUpperDecks = 128;
    private const uint NewGridania = 132;
    private const uint UldahStepsOfNald = 130;

    private static readonly PersonnelOfficer[] officers =
    [
        new(Maelstrom, LimsaLominsaUpperDecks, 1002388, new Vector3(94.02f, 40.28f, 74.48f)),
        new(TwinAdder, NewGridania, 1002394, new Vector3(-68.68f, -0.50f, -8.47f)),
        new(ImmortalFlames, UldahStepsOfNald, 1002391, new Vector3(-142.83f, 4.10f, -106.31f)),
    ];

    public static ReadOnlySpan<PersonnelOfficer> All => officers;

    public static bool TryFor(byte grandCompany, out PersonnelOfficer officer)
    {
        for (var officerIndex = 0; officerIndex < officers.Length; officerIndex++)
        {
            if (officers[officerIndex].GrandCompany != grandCompany)
            {
                continue;
            }

            officer = officers[officerIndex];
            return true;
        }

        officer = default;
        return false;
    }
}
