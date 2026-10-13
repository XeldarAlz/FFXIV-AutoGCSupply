namespace AutoGcSupply.Core.Character.Model;

public readonly record struct ClassProgress(byte ClassJobId, byte Level, int Experience, int ExperienceToNext);
