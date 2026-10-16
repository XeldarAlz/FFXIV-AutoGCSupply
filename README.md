<p align="center">
  <img src="AutoGcSupply/Images/Icon.png" width="180" alt="Auto GC Supply icon" />
</p>

<h1 align="center">Auto GC Supply</h1>

<p align="center">
  <a href="https://discord.gg/hppkAvdBEE"><img alt="Discord" src="https://img.shields.io/badge/Discord-join-5865F2?style=flat-square&logo=discord&logoColor=white"></a>
  <a href="https://github.com/XeldarAlz/FFXIV-AutoGCSupply/releases/latest"><img alt="Release" src="https://img.shields.io/github/v/release/XeldarAlz/FFXIV-AutoGCSupply?style=flat-square&color=blue"></a>
  <a href="https://github.com/XeldarAlz/FFXIV-AutoGCSupply/releases"><img alt="Downloads" src="https://img.shields.io/github/downloads/XeldarAlz/FFXIV-AutoGCSupply/total?style=flat-square&color=blue&cacheSeconds=300"></a>
  <a href="https://github.com/XeldarAlz/FFXIV-AutoGCSupply/actions/workflows/release.yml"><img alt="Build" src="https://img.shields.io/github/actions/workflow/status/XeldarAlz/FFXIV-AutoGCSupply/release.yml?style=flat-square"></a>
  <a href="LICENSE.md"><img alt="License" src="https://img.shields.io/badge/license-AGPL--3.0--or--later-blue?style=flat-square"></a>
</p>

<p align="center">
  <em>Grand Company supply and provisioning, delivered for you. Built on Dalamud.</em>
</p>

---

## What it does

Reads the day's Grand Company supply and provisioning requests and shows what each one costs and pays. Tick the requests you want and press **Automate**: the plugin buys what you are missing through Auto Shopper, travels to your Personnel Officer, hands in each request, and confirms every delivery.

## Features

- **Today page**: the day's supply and provisioning requests with what you hold, the experience and seals each pays, the estimated cost of what is missing, and the gil per 1,000 experience.
- **Pick what to do**: include or leave out any class, kept per character; classes at the level cap start left out since they earn seals only.
- **Quality that pays**: HQ turn-ins pay double, so Automatic quality buys HQ when it costs at most twice the NQ item; Always HQ and Always NQ are there too.
- **Prices through Auto Shopper**: market prices from the worlds Auto Shopper may visit, with a price tolerance you set on top and a fresh quote right before buying.
- **Run spend cap**: no run that buys starts without one, and Auto Shopper never spends past it.
- **Guarded hand-in**: menus chosen by text, rows matched by item and class, the reward window checked before Deliver, and every delivery confirmed against your bags and seals.
- **Seal cap**: deliver anyway for the experience, or skip a request whose seals would pass your rank's cap.
- **Run view**: a step rail from Reading to Done, every row's state and reason, and a summary of requests delivered, gil spent and time taken.
- **Run controls**: Pause, Resume and Stop, auto-pause while you're in a duty, and hold new moves while you type in chat.
- **Humanizer**: short random delays before moving off or teleporting.
- **GM alert**: ends a run when a GM is near, with optional toast, chat line, beeps, custom commands, or closing the game.
- **History and console**: the last 200 runs with their outcome, and every step logged on the Console page, ready to copy into a bug report.

## Install

In-game: `/xlsettings` → **Experimental** → paste into **Custom Plugin Repositories**:

```
https://raw.githubusercontent.com/XeldarAlz/DalamudPlugins/main/repo.json
```

Tick **Enabled**, click **+**, then **Save and Close**. Open `/xlplugins` → **All Plugins**, search for **Auto GC Supply**, and install.

The plugin needs **vnavmesh** to walk to the Personnel Officer and **Auto Shopper** (1.2.0.0 or later) to buy what is missing; without Auto Shopper it still hands in what your bags already hold. Open `/gcsupply deps` after install to see the list and one-click each missing one. The first run that buys asks you once, on Auto Shopper's Shop page, to allow Auto GC Supply.

## Commands

| Command | Action |
|---|---|
| `/gcsupply` | Toggle the main window |
| `/agcs` | Alias for `/gcsupply` |
| `/gcsupply config` | Open the Settings page |
| `/gcsupply stats` | Open the History page (`/gcsupply history` works too) |
| `/gcsupply deps` | Open the Plugins page (`/gcsupply dependencies` works too) |
| `/gcsupply log` | Open the Console page |
| `/gcsupply changelog` | Open the Changelog page |
| `/gcsupply about` | Open the About page |
| `/gcsupply start` | Start a run of the chosen requests |
| `/gcsupply stop` | Stop the current run |
| `/gcsupply pause` | Pause or resume the current run (`/gcsupply resume` works too) |
| `/gcsupply target` | Log targeted NPC's BaseId (debug helper) |

## Languages

The windows are available in English, Deutsch, Français, Español, Português (Brasil), Русский, Türkçe, 日本語, and 中文. The plugin picks a language from your Dalamud and game client settings on first launch; change it any time under Settings, General, Language. Game data such as zone, item, and world names always follows the game client.

Spotted a wrong or awkward translation? Open a [translation issue](https://github.com/XeldarAlz/FFXIV-AutoGCSupply/issues/new?template=translation_report.yml) and tell me what it should say instead.

## Community

Questions, ideas, or just want to hang out with other players? Come say hi on Discord.

→ [Join our Discord](https://discord.gg/hppkAvdBEE)

## More from me

If you liked this plugin, take a look at my other Dalamud work. You might find something else there for you.

→ [XeldarAlz Dalamud Plugins](https://github.com/XeldarAlz/DalamudPlugins)

## License

AGPL-3.0-or-later. See [LICENSE.md](LICENSE.md). [NOTICE](NOTICE) adds the attribution terms the AGPL allows: a fork, or any project that reuses this code, must credit the original author and must not pass itself off as the original. The license covers the code, not the name or the icon: read the [trademark and naming policy](TRADEMARK.md) before you publish a fork.

How AI is used to build this plugin is written down in [AI usage](AI-USAGE.md).
