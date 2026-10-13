using AutoGcSupply.Core.HandIn.Model;

namespace AutoGcSupply.Tests.HandIn;

public sealed class PersonnelOfficersTests
{
    [Theory]
    [InlineData(1, 128u, 1002388u)]
    [InlineData(2, 132u, 1002394u)]
    [InlineData(3, 130u, 1002391u)]
    public void FindsTheOfficerOfEachGrandCompany(int grandCompany, uint territoryId, uint baseId)
    {
        Assert.True(PersonnelOfficers.TryFor((byte)grandCompany, out var officer));
        Assert.Equal((byte)grandCompany, officer.GrandCompany);
        Assert.Equal(territoryId, officer.TerritoryId);
        Assert.Equal(baseId, officer.BaseId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void HasNoOfficerOutsideAGrandCompany(int grandCompany)
    {
        Assert.False(PersonnelOfficers.TryFor((byte)grandCompany, out var officer));
        Assert.Equal(default, officer);
    }

    [Fact]
    public void ListsOneOfficerPerGrandCompany()
    {
        var officers = PersonnelOfficers.All;
        Assert.Equal(3, officers.Length);
        for (var officerIndex = 0; officerIndex < officers.Length; officerIndex++)
        {
            Assert.Equal(officerIndex + 1, officers[officerIndex].GrandCompany);
        }
    }

    [Fact]
    public void ApproachPointsMatchTheRecordedSpots()
    {
        Assert.True(PersonnelOfficers.TryFor(1, out var maelstrom));
        Assert.Equal(94.02f, maelstrom.ApproachPoint.X, 2);
        Assert.Equal(40.28f, maelstrom.ApproachPoint.Y, 2);
        Assert.Equal(74.48f, maelstrom.ApproachPoint.Z, 2);
    }
}
