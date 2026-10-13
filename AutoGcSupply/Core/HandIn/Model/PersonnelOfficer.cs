using System.Numerics;

namespace AutoGcSupply.Core.HandIn.Model;

internal readonly record struct PersonnelOfficer(byte GrandCompany, uint TerritoryId, uint BaseId, Vector3 ApproachPoint);
