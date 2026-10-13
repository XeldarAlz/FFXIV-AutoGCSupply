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

> **In development.** This page describes what version 1.0 will do. Until it ships, the plugin is a skeleton and Automate does not run anything yet.

## What it does

Every day your Grand Company asks for crafted and gathered items in exchange for experience and company seals. Auto GC Supply reads the day's supply and provisioning requests, shows what each one costs and pays, and with one button buys what you are missing, travels to your Grand Company and hands everything in, confirming every delivery.

The buying is never its own: every purchase goes through Auto Shopper, with Auto Shopper's dry run, caps and confirmations. Without Auto Shopper the plugin still hands in what your bags already hold.

## Features

- **Today page**: the day's 8 supply requests (crafting classes) and 3 provisioning requests (gathering classes), each with its item, quantity, how many you own, the experience and seals it pays, the estimated cost of what is missing, and the gil it costs per 1,000 experience.
- **Pick what to do**: click a row to choose it or leave it out.
- **One button**: Automate reads the requests, buys the missing items through Auto Shopper, travels to your Grand Company with Lifestream, hands in at the Personnel Officer, and checks each delivery went through.
- **HQ when it pays**: HQ supply turn-ins pay double, so a request takes the HQ item automatically when it costs at most twice the NQ one.
- **Max-level classes off by default**: requests for classes at the level cap start unselected.
- **Hand-in-only mode**: without Auto Shopper, a run skips buying and hands in what you already have.

## How a run works

1. Open the Today page, choose the requests you want, and press **Automate** (or type `/gcsupply start`).
2. It reads the day's supply and provisioning requests and counts what you already own.
3. It buys the missing items through Auto Shopper. Auto Shopper does every purchase, with its own dry run, caps and confirmations.
4. It travels to your Grand Company with Lifestream and walks to the Personnel Officer.
5. It hands in each chosen request and verifies the delivery before moving on to the next one.

## Safety

- **Every purchase belongs to Auto Shopper.** Auto GC Supply never buys from the market board itself; Auto Shopper's dry run, caps and confirmations apply to every item it buys.
- **Every delivery is verified** before the run counts it and moves on.
- **Unknown prompts are never accepted.** The plugin answers only the dialogs it knows.
- **Max-level classes stay off** until you choose them.

## Install

In-game: `/xlsettings` → **Experimental** → paste into **Custom Plugin Repositories**:

```
https://raw.githubusercontent.com/XeldarAlz/DalamudPlugins/main/repo.json
```

Tick **Enabled**, click **+**, then **Save and Close**. Open `/xlplugins` → **All Plugins**, search for **Auto GC Supply**, and install.

## Requirements

- **Lifestream** for travel to your Grand Company.
- **vnavmesh** for walking to the Personnel Officer.
- **Auto Shopper** for buying the missing items. It is optional: without it, runs are hand-in only.

Open `/gcsupply deps` after install to see the helpers and one-click each missing one.

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
| `/gcsupply start` | Start a run |
| `/gcsupply stop` | Stop the run |
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
