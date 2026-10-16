using AutoGcSupply.Core.Tasks.Model;

namespace AutoGcSupply.Core.Stats;

internal sealed record RunRecord(
    Guid RunId,
    DateTime StartedAtUtc,
    DateTime EndedAtUtc,
    RunEnd End,
    uint WorldId,
    int Delivered,
    int Planned,
    long Experience,
    long Seals,
    long SealsLost,
    long GilSpent);
