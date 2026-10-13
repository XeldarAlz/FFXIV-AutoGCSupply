using AutoGcSupply.Core.Localization;

namespace AutoGcSupply.Core.Changelog;

internal readonly record struct ChangelogEntry(string Version, string Date, LocString[] Highlights);
