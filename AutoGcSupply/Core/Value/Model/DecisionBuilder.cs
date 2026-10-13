using AutoGcSupply.Core.Character.Model;
using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Requests.Model;

namespace AutoGcSupply.Core.Value.Model;

public static class DecisionBuilder
{
    public static RowDecision[] Build(
        in RequestSnapshot snapshot,
        in CharacterState character,
        ReadOnlySpan<OwnedCount> ownedBySlot,
        ReadOnlySpan<ItemPrice> prices,
        in DecisionSettings settings)
    {
        if (!snapshot.Loaded)
        {
            return [];
        }

        var requests = snapshot.Requests;
        if (ownedBySlot.Length != requests.Length)
        {
            throw new ArgumentException($"Expected {requests.Length} owned counts, got {ownedBySlot.Length}.", nameof(ownedBySlot));
        }

        var decisions = new RowDecision[requests.Length];
        for (var slot = 0; slot < requests.Length; slot++)
        {
            var request = requests[slot];
            decisions[SupplyClasses.DisplayIndex(request.ClassJobId)] = Decide(request, ownedBySlot[slot], character, prices, settings);
        }

        return decisions;
    }

    private static RowDecision Decide(
        in SupplyRequest request,
        OwnedCount owned,
        in CharacterState character,
        ReadOnlySpan<ItemPrice> prices,
        in DecisionSettings settings)
    {
        if (!request.Open)
        {
            return Delivered(request, owned);
        }

        var priced = TryFindPrice(prices, request.ItemId, out var price);
        var usable = settings.UseOwnedItems ? owned : default;
        var choice = QualityRule.Choose(request.Kind, settings.SupplyQuality, request.Requested, usable, priced, price);
        var maxUnitPrice = 0L;
        if (choice.Coverage == RowCoverage.ToBuy && !CapRule.TryMaxUnitPrice(choice.UnitPrice, settings.PriceTolerancePercent, out maxUnitPrice))
        {
            choice = choice with { Coverage = RowCoverage.Unpriced, UnitPrice = 0, EstimatedTotal = 0 };
        }

        var progress = character.Class(request.ClassJobId);
        var atMaxLevel = progress.Level >= character.MaxLevel;
        var experience = atMaxLevel ? 0 : RewardMath.Experience(request, choice.Quality);
        var coverage = CoverageOf(request.ClassJobId, choice.Coverage, atMaxLevel, settings);
        return new RowDecision
        {
            Request = request,
            Included = coverage is not (RowCoverage.Excluded or RowCoverage.MaxLevel),
            Owned = owned,
            Coverage = coverage,
            ChosenQuality = choice.Quality,
            BuyQuantity = choice.BuyQuantity,
            UnitPrice = choice.UnitPrice,
            EstimatedTotal = choice.EstimatedTotal,
            MaxUnitPrice = maxUnitPrice,
            ExcessAllowance = choice.Coverage == RowCoverage.ToBuy ? ExcessRule.Allowance(request.Requested) : 0,
            Experience = experience,
            Seals = RewardMath.Seals(request, choice.Quality),
            LevelShare = RewardMath.LevelShare(experience, progress.ExperienceToNext),
            GilPerThousandExperience = GilPerThousand(choice.EstimatedTotal, experience),
        };
    }

    private static RowCoverage CoverageOf(byte classJobId, RowCoverage chosen, bool atMaxLevel, in DecisionSettings settings)
    {
        if (settings.ExcludedClasses.Contains(classJobId))
        {
            return RowCoverage.Excluded;
        }

        return atMaxLevel && !settings.IncludeMaxLevelClasses ? RowCoverage.MaxLevel : chosen;
    }

    private static RowDecision Delivered(in SupplyRequest request, OwnedCount owned)
    {
        return new RowDecision
        {
            Request = request,
            Included = false,
            Owned = owned,
            Coverage = RowCoverage.Delivered,
            ChosenQuality = ItemQuality.Normal,
            BuyQuantity = 0,
            UnitPrice = 0,
            EstimatedTotal = 0,
            MaxUnitPrice = 0,
            ExcessAllowance = 0,
            Experience = 0,
            Seals = 0,
            LevelShare = 0d,
            GilPerThousandExperience = 0,
        };
    }

    private static long GilPerThousand(long estimatedTotal, long experience)
        => estimatedTotal > 0 && RewardMath.TryGilPerThousandExperience(estimatedTotal, experience, out var gilPerThousand) ? gilPerThousand : 0;

    private static bool TryFindPrice(ReadOnlySpan<ItemPrice> prices, uint itemId, out ItemPrice price)
    {
        for (var index = 0; index < prices.Length; index++)
        {
            if (prices[index].ItemId == itemId)
            {
                price = prices[index];
                return true;
            }
        }

        price = default;
        return false;
    }
}
