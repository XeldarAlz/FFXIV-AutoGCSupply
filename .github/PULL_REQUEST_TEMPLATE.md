## What

<!-- One or two sentences on what this PR changes. -->

## Why

<!-- The motivating problem, linked issue, or user-visible behavior this fixes. -->

Closes #

## How to test

<!--
Minimum steps a reviewer can run to verify the change. Testing usually means opening the Today page, choosing one or two of the day's requests, pressing Automate and watching the plugin read, buy through Auto Shopper, travel to your Grand Company and hand in, with the Console open. When the change touches buying, keep Auto Shopper on dry run first; when it touches the hand-in, say which request you handed in for real. Make sure the dependencies listed in /gcsupply deps are installed first. For UI-only changes, describe what to click.
-->

## Checklist

- [ ] `dotnet build -c Release` passes and `dotnet test -c Release` is green
- [ ] Verified in-game on at least one run, or on one hand-in for changes that only touch the hand-in
- [ ] New or changed user-facing text is in `L*.cs` and all nine catalogs, same keys in the same order
- [ ] If this changes user-visible behavior, README is updated
- [ ] If this touches the run, buying or the hand-in, relevant `[AGCS]` log lines make the sequence auditable
