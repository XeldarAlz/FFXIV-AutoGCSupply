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

Every day your Grand Company asks for crafted and gathered items in exchange for experience and company seals. Auto GC Supply reads the day's supply and provisioning requests, shows what each one costs and pays, and with one button buys what you are missing, travels to your Personnel Officer and hands in the requests you chose, confirming every delivery.

It never buys anything itself. Every purchase goes through Auto Shopper (1.1.0.0 or later), under Auto Shopper's own caps and purchase checks, and Auto Shopper's dry run covers the whole run: while it is on, a run buys nothing and hands nothing in, and every step shows what it would do. Without Auto Shopper the plugin still hands in what your bags already hold, and those hand-ins are real.

## Features

- **Today page**: the day's 8 supply requests (crafting classes) and 3 provisioning requests (gathering classes), each with its item and quantity, how many NQ and HQ you hold in your bags, the experience it pays and its share of your current level, the seals it pays, the estimated cost of what is missing, and the gil it costs per 1,000 experience. Above the list: your Grand Company rank, your seals against your rank's cap, the time to the next reset, Auto Shopper's state and dry run, and totals for what you chose, with the estimated spend and any seals the cap would cost you.
- **Reading the requests**: the game sends the day's requests only once its Timers window has been opened, so Auto GC Supply opens it for a moment when it needs them, and reads them again after the daily reset at 20:00 UTC. Refresh on the Grand Company card reads them again on demand.
- **Pick what to do**: click a row to include or leave out its class, or use All and None for each group. Your choices are kept per character. Classes at the level cap start left out, since they earn seals only; Include max-level classes under Settings, Run brings them in.
- **Quality that pays**: HQ supply turn-ins pay double experience and seals, so Automatic quality buys the HQ item when it costs at most twice the NQ one, and when your bags already cover a request, it hands in HQ before NQ. Always HQ and Always NQ are there too. Provisioning is always bought and handed in as NQ.
- **Prices through Auto Shopper**: the Today page asks Auto Shopper for the market price of what is missing, on the worlds Auto Shopper may visit. Each line's price cap is the dearest listing unit price that quote chose, plus the price tolerance you set (25% by default, up to 200%). A fresh quote right before buying can lower a cap or drop a line with no listings, never raise a cap.
- **Run spend cap**: any run that buys needs one. The Today page says whether it covers the estimate and offers the estimate plus 20% in one click. Auto Shopper never spends past it.
- **One button**: Automate (or `/gcsupply start`) buys the missing items through Auto Shopper, travels to your Grand Company, hands in each chosen request at the Personnel Officer, and checks every delivery. See [How a run works](#how-a-run-works).
- **Guarded hand-in**: the officer's menu is chosen by its text, rows are matched by item and class, the reward window is checked before Deliver, and every delivery is confirmed. See [Safety](#safety).
- **Seal cap**: when a request's seals would pass your rank's cap, deliver anyway for the experience (the default) or skip the row and keep the item.
- **Run view**: a step rail (Reading, Pricing, Buying, Travelling, Handing in, Done), Auto Shopper's spend while it buys, every row's state and reason, and a summary card at the end with the requests delivered, the gil spent and the time taken. When a run needs you, a card says why and what to do next.
- **Run controls**: Pause, Resume and Stop in the dock at the bottom of the window or as commands, auto-pause while you are in a duty, and hold new moves while you type in chat. While Auto Shopper is buying, pausing belongs to Auto Shopper; Stop asks it to stop, and the run ends once its purchase settles.
- **Humanizer**: short random delays before the character moves off or casts a teleport.
- **GM alert**: ends a run when a GM is near, with an optional toast, chat line, beeps, custom commands, or closing the game.
- **History and console**: the last 200 runs with their outcome, requests delivered, experience, seals, seals lost to the cap and gil spent, with dry runs marked. Every step is logged on the Console page, ready to copy into a bug report.

## How a run works

1. Choose the requests you want on the Today page, set a run spend cap when anything needs buying, and press **Automate** (or type `/gcsupply start`). Automate starts only when you are logged in, in a Grand Company, on your home world and outside a duty, with today's requests loaded and, when anything needs buying, priced. The run keeps to the rows and caps you saw at that moment.
2. It reads the day's requests again and drops any row that was handed in elsewhere or changed since.
3. When something is missing, it gets fresh prices from Auto Shopper if the last ones are older than 10 minutes, then sends Auto Shopper one order: each missing item, its quality and price cap, and the run spend cap. Auto Shopper buys under its own rules and travels back to your home world. The first time, the run stops here: Auto Shopper asks you on its Shop page to allow Auto GC Supply, and you press Automate again once you have.
4. It counts your bags, teleports to your Grand Company's city, takes the aethernet when that is shorter, and walks to the Personnel Officer.
5. It opens the supply list from the officer's menu and hands in the chosen requests one at a time, confirming each delivery before it picks the next. In a dry run it stops short of choosing a row and marks each one it would hand in.
6. It closes the officer's windows and records the run in History.

## Safety

- **Every purchase belongs to Auto Shopper.** Auto GC Supply never touches the market board. It sends Auto Shopper one order per run, with a price cap on each line and the run spend cap, and Auto Shopper's worlds, gil reserve and purchase checks still apply.
- **Dry run.** Auto Shopper's Dry run switch, shown next to Automate, governs the whole run. While it is on, nothing is bought and nothing is handed in: the run travels, opens the supply list, and marks each row it would deliver. When Auto Shopper 1.1.0.0 or later is there but its switch cannot be read, the hand-in is dry too. Without it (not installed, or a version without the Buying API) there is no dry run, so a hand-in-only run delivers for real.
- **Asked once.** The first run that buys stops at Auto Shopper's permission card on its Shop page, and nothing is bought until you allow Auto GC Supply there. You can take that back under Auto Shopper's Settings, General.
- **Caps.** No run that buys starts without a run spend cap, and each line carries its own price cap.
- **Only the rows you chose.** A run hands in only the rows you had chosen at Start, and drops a row whose request changed or was handed in elsewhere.
- **Menu by text.** The officer's supply option is found by its text, read from the game data in your client's language, never by its place in the menu.
- **Rows by item and class.** A row is handed in only when exactly one row of the supply list shows that item for that class, its amount matches the request the run started with, the game still takes it, and your bags hold enough of the chosen quality.
- **One row at a time.** It hands in one request, waits for its confirmation, and only then picks the next.
- **Reward window checked before Deliver.** The item and the seals in the reward window must match the request (seals may run higher with the star and seal bonuses, or lower near your seal cap). Anything else is cancelled and the run stops.
- **Only the HQ trade prompt.** The one question it answers Yes to is the game's prompt to trade a high-quality item, and only on an HQ row, once. Any other question is answered No and the run stops; an unexpected window stops it too.
- **Confirmed, not assumed.** A delivery counts only when your bags dropped by the requested amount and your seals rose (or the row left the list). Anything less marks it unconfirmed, and the run asks you to check the supply list.
- **Stops for you.** Combat, a duty pop, an officer it cannot find or reach, a list that does not open, or a hand-in that takes too long ends the run with a card that says why. Press Automate to try again once you have checked the game. A logout or a reload ends the run, and nothing resumes on its own.
- **What it never does**: buy anything itself, sell or list items, accept a prompt it does not know, hand in a row you did not choose, spend past the run spend cap, or deliver while Auto Shopper's dry run is on. It makes no network calls of its own; prices come through Auto Shopper.

## Install

In-game: `/xlsettings` → **Experimental** → paste into **Custom Plugin Repositories**:

```
https://raw.githubusercontent.com/XeldarAlz/DalamudPlugins/main/repo.json
```

Tick **Enabled**, click **+**, then **Save and Close**. Open `/xlplugins` → **All Plugins**, search for **Auto GC Supply**, and install.

## Requirements

- **vnavmesh** for walking to the Personnel Officer. Automate stays off until it is installed and loaded.
- **Auto Shopper 1.1.0.0 or later** for buying the missing items, through its Buying API. It is optional: without it, only requests your bags already cover are handed in, and there is no dry run. The first run that buys asks you once, on Auto Shopper's Shop page, to allow Auto GC Supply; press Allow there, then press Automate again. Auto Shopper brings its own requirements, such as Lifestream for its trips between worlds.
- **A Grand Company**, with runs started from your home world.
- **A run spend cap** on the Today page or under Settings, Run, for any run that buys.

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
| `/gcsupply start` | Open the Today page and start a run of the chosen requests; when it cannot start, the chat says why |
| `/gcsupply stop` | Stop the run; while Auto Shopper is buying, it is asked to stop and the run ends once it settles |
| `/gcsupply pause` | Pause or resume the current run (`/gcsupply resume` works too); while Auto Shopper is buying, pause it there |
| `/gcsupply target` | Log the targeted NPC's BaseId (debug helper) |

## Known limits

- **Bags only.** Owned counts and hand-ins use your four inventory bags. The armoury chest, equipped gear, retainers and saddlebags are never counted or used.
- **Provisioning is NQ only.** HQ copies in your bags are not handed in for a provisioning request, and the row offers to buy NQ instead.
- **The Timers window opens by itself** for a moment when today's requests have not reached the game yet.
- **Runs close to the daily reset.** When 20:00 UTC passes before the buying, nothing is bought. When it passes after, only requests that still match are handed in, and what was bought for the others stays in your bags.
- **Extra units.** When only bigger stacks are listed, Auto Shopper may buy up to as many extra units as a request asks for (requests of 2 or more), within the price caps and the run spend cap. The extras stay in your bags.
- **Prices are estimates.** They are Auto Shopper's Universalis prices, and Auto Shopper's own known limits apply to every purchase.

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
