using AutoGcSupply.Core.HandIn.Model;
using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Core.Value.Model;
using static AutoGcSupply.Tests.Run.RunFixtures;

namespace AutoGcSupply.Tests.Run;

public sealed class RunAuthorizationTests
{
    private static readonly Guid runId = new("0d7f0000-1111-2222-3333-444455556666");
    private static readonly DateTime startedAtUtc = new(2026, 10, 13, 21, 15, 0, DateTimeKind.Utc);
    private static readonly DayKey startDay = DayKey.For(startedAtUtc);

    [Fact]
    public void FreezesTheCharacterTheDayTheCapsAndOnlyTheIncludedRows()
    {
        RowDecision[] decisions =
        [
            Row(SupplyClasses.Carpenter, CarpenterItem, 1, RowCoverage.MaxLevel, ItemQuality.High, 1, 0, 0, 0, 0, 840, included: false),
            ToBuy(SupplyClasses.Culinarian, CulinarianItem, 3, ItemQuality.Normal, 3, 1_250),
            Owned(SupplyClasses.Miner, MinerItem, 10),
            Row(SupplyClasses.Botanist, 5509, 20, RowCoverage.NoListings, ItemQuality.Normal, 20, 0, 0, 0, 91_000, 301),
        ];
        var character = CharacterFixtures.Standard() with { CurrentWorldId = 40 };

        var authorization = Freeze(character, decisions);

        Assert.Equal(runId, authorization.RunId);
        Assert.Equal(SupplyFixtures.ContentId, authorization.ContentId);
        Assert.Equal(40u, authorization.WorldId);
        Assert.Equal(startDay, authorization.Day);
        Assert.Equal(startedAtUtc, authorization.StartedAtUtc);
        Assert.Equal(150_000, authorization.RunCap);
        Assert.Equal(25, authorization.TolerancePercent);
        Assert.Equal(SealCapPolicy.SkipRow, authorization.SealCapPolicy);
        Assert.Equal(new[] { decisions[1], decisions[2], decisions[3] }, authorization.Rows.AsSpan().ToArray());
        Assert.Equal("GC supply 2026-10-13", authorization.ShopperLabel);
    }

    [Fact]
    public void FrozenRowsDoNotFollowLaterChangesToTheDecisions()
    {
        RowDecision[] decisions = [ToBuy(SupplyClasses.Culinarian, CulinarianItem, 3, ItemQuality.Normal, 3, 1_250)];

        var authorization = Freeze(CharacterFixtures.Standard(), decisions);
        decisions[0] = decisions[0] with { MaxUnitPrice = 9_999 };

        Assert.Equal(1_250, authorization.Rows[0].MaxUnitPrice);
    }

    [Fact]
    public void PurchasesAreAllowedOnlyOnTheDayTheRunStarted()
    {
        var authorization = Freeze(CharacterFixtures.Standard(), []);
        var nextDay = new DayKey(startDay.DayNumber + 1);

        Assert.True(authorization.AllowsPurchases(startDay, startedAtUtc.AddHours(1)));
        Assert.False(authorization.AllowsPurchases(startDay, startDay.NextResetUtc));
        Assert.False(authorization.AllowsPurchases(nextDay, startedAtUtc));
    }

    private static RunAuthorization Freeze(in Core.Character.Model.CharacterState character, RowDecision[] decisions)
        => RunAuthorization.Freeze(runId, character, startDay, startedAtUtc, 150_000, 25, SealCapPolicy.SkipRow, decisions);
}
