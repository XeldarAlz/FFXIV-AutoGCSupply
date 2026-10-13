using AutoGcSupply.Core.Character.Model;
using AutoGcSupply.Core.Requests.Model;
using System.Collections.Immutable;

namespace AutoGcSupply.Tests;

public sealed class CharacterStateTests
{
    [Fact]
    public void ClassLookupFollowsTheDisplayOrder()
    {
        var character = CharacterFixtures.Standard();

        var fisher = character.Class(SupplyClasses.Fisher);
        var carpenter = character.Class(SupplyClasses.Carpenter);

        Assert.Equal(SupplyClasses.Fisher, fisher.ClassJobId);
        Assert.Equal(SupplyClasses.Carpenter, carpenter.ClassJobId);
        Assert.Equal(CharacterFixtures.MaxLevel, carpenter.Level);
    }

    [Fact]
    public void MaxLevelIsReachedAtTheCap()
    {
        var character = CharacterFixtures.Standard();

        Assert.True(character.IsMaxLevel(SupplyClasses.Carpenter));
        Assert.False(character.IsMaxLevel(SupplyClasses.Blacksmith));
    }

    [Fact]
    public void GrandCompanyZeroMeansNone()
    {
        Assert.True(CharacterFixtures.Standard().InGrandCompany);
        Assert.False((CharacterFixtures.Standard() with { GrandCompany = 0 }).InGrandCompany);
    }

    [Fact]
    public void HomeWorldComparesCurrentAndHome()
    {
        var character = CharacterFixtures.Standard();

        Assert.True(character.OnHomeWorld);
        Assert.False((character with { CurrentWorldId = character.HomeWorldId + 1 }).OnHomeWorld);
    }
}

internal static class CharacterFixtures
{
    public const byte MaxLevel = 100;
    public const uint HomeWorldId = 33;
    public const int ExperienceToNext = 1_000_000;

    public static CharacterState Standard()
    {
        var order = SupplyClasses.DisplayOrder;
        var classes = new ClassProgress[order.Length];
        for (var index = 0; index < order.Length; index++)
        {
            var classJobId = order[index];
            var level = classJobId == SupplyClasses.Carpenter ? MaxLevel : (byte)90;
            classes[index] = new ClassProgress(classJobId, level, 250_000, classJobId == SupplyClasses.Carpenter ? 0 : ExperienceToNext);
        }

        return new CharacterState(SupplyFixtures.ContentId, HomeWorldId, HomeWorldId, 1, 9, 12_000, 15_000, MaxLevel, ImmutableArray.Create(classes));
    }
}
