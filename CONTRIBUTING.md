# Contributing

Thanks for taking an interest. This is a small solo project, but PRs are welcome and I'll review them.

## Quick start

```bash
git clone --recurse-submodules https://github.com/XeldarAlz/FFXIV-AutoGCSupply.git
cd FFXIV-AutoGCSupply
dotnet build AutoGcSupply.sln -c Release
dotnet test AutoGcSupply.sln -c Release
```

You need the .NET 10 SDK. The plugin requires Dalamud at runtime; CI pulls a Dalamud dev build automatically and that's enough to compile. See `.github/workflows/ci.yml` if you want to reproduce CI locally.

Load the built plugin via `/xlsettings` -> **Experimental** -> **Dev Plugin Locations**, pointing at `AutoGcSupply/bin/Release/AutoGcSupply.dll`.

## Project layout

- `AutoGcSupply/Core/Tasks/`: the run controller and the shared movement helpers (teleport, aethernet, walking, landing).
- `AutoGcSupply/Core/Travel/` and `AutoGcSupply/Core/Ipc/`: zone and aetheryte data, and the Lifestream and vnavmesh bridges.
- `AutoGcSupply/Core/Game/`: NPC and dialog handling, and the duty and GM watchers.
- `AutoGcSupply/Core/External/`: the helper plugins on the Plugins page and their one-click install.
- `AutoGcSupply/Core/Stats/` and `AutoGcSupply/Core/Changelog/`: History and the changelog entries.
- `AutoGcSupply/Core/Localization/` and `AutoGcSupply/Localization/`: the string table and the nine language catalogs.
- `AutoGcSupply/Windows/`: the ImGui window, its pages and settings.
- `AutoGcSupply/`: plugin entry points, config, command wiring.
- `AutoGcSupply.Tests/`: unit tests for everything above that runs without the game.
- `ECommons/`: submodule, shared Dalamud helpers. Don't patch this directly; upstream it.

Keep logic small and direct. This plugin has one job.

## Before you open a PR

1. `dotnet build -c Release` cleanly, and `dotnet test -c Release` passes.
2. Test in game on at least one run of a request or two. Grand Company headquarters and their officers differ between the three cities, so a fix that works for one Grand Company may not work for another. When you change how items are bought, keep Auto Shopper on dry run first; when you change the hand-in, hand in one request for real and attach the Console lines of that delivery.
3. Keep the diff focused. One concern per PR.
4. Match the existing style. No heavy abstractions "for later."
5. Every string a user sees goes through `Core/Localization/L*.cs` and all nine catalogs under `AutoGcSupply/Localization/`, with the same keys in the same order. CI checks the keys and rejects em dashes.
6. If your change affects what a user sees or types (commands, window layout, settings), update the README.
7. If you used AI beyond autocomplete, say which level in the PR description. It is one line, and [AI-USAGE.md](AI-USAGE.md) explains the level names and why I ask.

## Good first issues

Check the tracker for anything labeled `good first issue`. Travel quirks (an aetheryte it misses, an officer it can't walk up to) are usually the lowest-friction way to help: name where it misbehaved, attach a log of what the plugin did vs. what should have happened, and a fix is usually a small change.

## Security

Please don't file public issues for security problems; see [SECURITY.md](SECURITY.md).

## Code of conduct

See [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md). Be decent.

## License

By contributing, you agree your contributions are licensed under AGPL-3.0-or-later with the additional terms in [NOTICE](NOTICE), the same as the project.
