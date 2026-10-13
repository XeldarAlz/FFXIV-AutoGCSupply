using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Requests.Model;

namespace AutoGcSupply.Core.Value.Model;

public static class RewardMath
{
    public const int HighQualitySupplyFactor = 2;

    private const int NormalFactor = 1;
    private const long ExperienceBlock = 1_000;

    public static int Factor(SupplyMissionKind kind, ItemQuality quality)
        => kind == SupplyMissionKind.Supply && quality == ItemQuality.High ? HighQualitySupplyFactor : NormalFactor;

    public static long Experience(in SupplyRequest request, ItemQuality quality)
        => (long)request.ExperienceReward * Factor(request.Kind, quality);

    public static long Seals(in SupplyRequest request, ItemQuality quality)
        => (long)request.SealReward * Factor(request.Kind, quality);

    public static double LevelShare(long experience, int experienceToNext)
        => experienceToNext <= 0 ? 0d : (double)experience / experienceToNext;

    public static bool TryGilPerThousandExperience(long gil, long experience, out long gilPerThousand)
    {
        if (gil < 0 || experience <= 0)
        {
            gilPerThousand = 0;
            return false;
        }

        var rounded = ((Int128)gil * ExperienceBlock + experience / 2) / experience;
        if (rounded > long.MaxValue)
        {
            gilPerThousand = 0;
            return false;
        }

        gilPerThousand = (long)rounded;
        return true;
    }

    public static long SealsLost(long currentSeals, long sealCap, long reward)
    {
        var room = Math.Max(0, sealCap - currentSeals);
        return Math.Max(0, reward - room);
    }
}
