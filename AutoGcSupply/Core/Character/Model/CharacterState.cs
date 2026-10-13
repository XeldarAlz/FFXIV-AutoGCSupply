using AutoGcSupply.Core.Requests.Model;
using System.Collections.Immutable;

namespace AutoGcSupply.Core.Character.Model;

public readonly record struct CharacterState(
    ulong ContentId,
    uint HomeWorldId,
    uint CurrentWorldId,
    byte GrandCompany,
    byte Rank,
    uint Seals,
    uint SealCap,
    byte MaxLevel,
    ImmutableArray<ClassProgress> Classes)
{
    private const byte NoGrandCompany = 0;

    public bool InGrandCompany => GrandCompany != NoGrandCompany;

    public bool OnHomeWorld => CurrentWorldId == HomeWorldId;

    public ClassProgress Class(byte classJobId) => Classes[SupplyClasses.DisplayIndex(classJobId)];

    public bool IsMaxLevel(byte classJobId) => Class(classJobId).Level >= MaxLevel;
}
