using AutoGcSupply.Core.Character.Model;
using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Value.Model;
using System.Collections.Immutable;

namespace AutoGcSupply.Core.Tasks.Model;

internal readonly record struct TodayInputs
{
    public required bool LoggedIn { get; init; }

    public required bool HasCharacter { get; init; }

    public required CharacterState Character { get; init; }

    public required RequestSnapshot Snapshot { get; init; }

    public required bool RequestsCurrent { get; init; }

    public required bool Reading { get; init; }

    public required ImmutableArray<OwnedCount> OwnedBySlot { get; init; }

    public required DecisionSettings Settings { get; init; }

    public required ShopperState Shopper { get; init; }

    public required bool ShopperDryRun { get; init; }

    public required long SpendCap { get; init; }

    public required bool SetupReady { get; init; }

    public required bool Running { get; init; }

    public required DateTime NowUtc { get; init; }
}
