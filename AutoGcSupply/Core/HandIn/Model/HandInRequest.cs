namespace AutoGcSupply.Core.HandIn.Model;

internal readonly record struct HandInRequest(HandInTarget[] Targets, SealCapPolicy SealCapPolicy);
