using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Shopper.Model;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Core.Value.Model;
using System.Collections.Immutable;

namespace AutoGcSupply.Tests.Run;

internal sealed class FixedLabels : IRowLabels
{
    public string ClassName(byte classJobId) => $"class {classJobId}";

    public string ItemName(uint itemId) => $"item {itemId}";

    public uint ItemIconId(uint itemId) => itemId + 20_000;
}

internal static class RunFixtures
{
    public const uint CarpenterItem = 43273;
    public const uint CulinarianItem = 44098;
    public const uint MinerItem = 5106;

    public static RowDecision Owned(byte classJobId, uint itemId, byte requested, ItemQuality quality = ItemQuality.Normal, long seals = 400, long experience = 100_000)
        => Row(classJobId, itemId, requested, RowCoverage.Owned, quality, 0, 0, 0, 0, experience, seals);

    public static RowDecision ToBuy(byte classJobId, uint itemId, byte requested, ItemQuality quality, int buyQuantity, long maxUnitPrice, long estimatedTotal = 1_000, long seals = 400, long experience = 100_000)
        => Row(classJobId, itemId, requested, RowCoverage.ToBuy, quality, buyQuantity, maxUnitPrice, requested <= 1 ? 0 : requested, estimatedTotal, experience, seals);

    public static RowDecision Row(
        byte classJobId,
        uint itemId,
        byte requested,
        RowCoverage coverage,
        ItemQuality quality,
        int buyQuantity,
        long maxUnitPrice,
        int excessAllowance,
        long estimatedTotal,
        long experience,
        long seals,
        bool included = true)
    {
        return new RowDecision
        {
            Request = new SupplyRequest(0, classJobId, itemId, (uint)experience, (uint)seals, requested, requested, false),
            Included = included,
            Owned = default,
            Coverage = coverage,
            ChosenQuality = quality,
            BuyQuantity = buyQuantity,
            UnitPrice = maxUnitPrice,
            EstimatedTotal = estimatedTotal,
            MaxUnitPrice = maxUnitPrice,
            ExcessAllowance = excessAllowance,
            Experience = experience,
            Seals = seals,
            LevelShare = 0d,
            GilPerThousandExperience = 0,
        };
    }

    public static TodayRow Today(in RowDecision decision) => new(decision, "class", 90, "item", 1);

    public static TodayRow[] TodayRows(params RowDecision[] decisions)
    {
        var rows = new TodayRow[decisions.Length];
        for (var index = 0; index < decisions.Length; index++)
        {
            rows[index] = Today(decisions[index]);
        }

        return rows;
    }

    public static ShopperRunView RunView(bool terminal, ShopperRunOutcome outcome, params ShopperRunLine[] lines)
    {
        return ShopperRunView.Failed(ShopperReply.Valid) with
        {
            Known = true,
            RunId = "4e1a",
            Terminal = terminal,
            Outcome = outcome,
            Lines = ImmutableArray.Create(lines),
        };
    }

    public static SupplyRequest Request(byte classJobId, uint itemId, byte requested, byte remaining)
        => new(0, classJobId, itemId, 100_000, 400, requested, remaining, false);

    public static RequestSnapshot Snapshot(DateTime readAtUtc, params SupplyRequest[] requests)
        => new(SupplyFixtures.ContentId, DayKey.For(readAtUtc), readAtUtc, SnapshotLoadState.Loaded, ImmutableArray.Create(requests));
}
