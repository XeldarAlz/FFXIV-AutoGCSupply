# Security policy

## Supported versions

Only the latest release published to the custom Dalamud repo is supported. If you're on an older version, please update before reporting.

## Reporting a vulnerability

Please report security issues privately via GitHub's private vulnerability reporting:

https://github.com/XeldarAlz/FFXIV-AutoGCSupply/security/advisories/new

Please don't open a public issue or Discussion for anything that could let someone else exploit users of the plugin before a fix is out.

What counts:

- Code execution or crashes triggerable by crafted game state or a crafted answer from a helper plugin.
- The plugin handing in something it shouldn't: an item for a request you did not choose, or an answer to a prompt it does not know.
- The plugin buying anything itself instead of through Auto Shopper, or asking Auto Shopper for more than the chosen requests need.
- The plugin teleporting, travelling, or persisting something it shouldn't, beyond the documented supply and provisioning automation, which is the feature.
- Data exfiltration: the one-click helper install on the Plugins page only asks Dalamud to add that helper's repository and install it. Sending your data anywhere would be a bug.

What doesn't:

- The fact that Auto GC Supply runs the supply and provisioning loop for you. That's against Square Enix's Terms of Service and is the whole feature; turning it on is the user's choice.

I'll aim to acknowledge reports within a few days and to ship a fix or workaround as soon as I've verified the issue.
