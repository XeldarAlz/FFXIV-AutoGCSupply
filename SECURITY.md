# Security policy

## Supported versions

Only the latest release published to the custom Dalamud repo is supported. If you're on an older version, please update before reporting.

## Reporting a vulnerability

Please report security issues privately via GitHub's private vulnerability reporting:

https://github.com/XeldarAlz/FFXIV-AutoGCSupply/security/advisories/new

Please don't open a public issue or Discussion for anything that could let someone else exploit users of the plugin before a fix is out.

What counts:

- Code execution or crashes triggerable by crafted game state, or by a crafted answer from a helper plugin such as Auto Shopper's Buying API replies.
- The plugin handing in something it shouldn't: a row you did not choose, a row whose item, class or amount does not match, the wrong quality, anything at all while Auto Shopper's dry run is on, more than one row at a time, or a Yes to any prompt other than the game's HQ trade prompt on an HQ row.
- The plugin buying anything itself instead of through Auto Shopper, or asking Auto Shopper for more than the chosen rows need, at a price cap above the one the Today page showed, or under a spend cap other than the run spend cap you set.
- The plugin teleporting, travelling, or persisting something it shouldn't, beyond the documented supply and provisioning automation, which is the feature.
- Data exfiltration: the plugin makes no network calls of its own. Market prices come through Auto Shopper, which reads them from Universalis, and the one-click helper install on the Plugins page only asks Dalamud to add that helper's repository and install it. Sending your data anywhere would be a bug.

What it reads and keeps, so you can tell a bug from the design:

- **Read from the game, never sent anywhere**: your Grand Company, rank, seals and seal cap, your crafting and gathering class levels and experience, the day's supply and provisioning requests, how many of each requested item your four inventory bags hold, and, during a hand-in, the officer's menu, the supply list, the reward window and any prompt that opens.
- **Kept on disk in the plugin's config folder**: your settings, including the classes you left out per character (keyed by the character's content id, the number Dalamud uses to tell characters apart), and `run-history.json` with your last 200 runs (times, outcome, dry run or not, world, requests delivered, experience, seals and gil). The run history holds no character names.
- **Copied only when you press Copy log**: the Console lines, with a header naming the plugin version, the Dalamud version and your zone.

What doesn't:

- The fact that Auto GC Supply runs the supply and provisioning loop for you. That's against Square Enix's Terms of Service and is the whole feature; turning it on is the user's choice.

I'll aim to acknowledge reports within a few days and to ship a fix or workaround as soon as I've verified the issue.
