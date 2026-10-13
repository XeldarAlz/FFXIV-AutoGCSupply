## What

<!-- One or two sentences on what this PR changes. -->

## Why

<!-- The motivating problem, linked issue, or user-visible behavior this fixes. -->

Closes #

## How to test

<!--
Minimum steps a reviewer can run to verify the change. Testing usually means opening the Today page, choosing one or two of the day's requests, and pressing Automate with Auto Shopper's dry run on, watching the plugin read, price, travel to your Grand Company and open the supply list with the Console open. When the change touches the run, the buying or the hand-in, say which request you handed in for real and that you watched it the whole way; when it touches buying, say which cheap item Auto Shopper bought and under what run spend cap. Make sure the dependencies listed in /gcsupply deps and Auto Shopper 1.1.0.0 or later are installed first. For UI-only changes, describe what to click.
-->

## Checklist

- [ ] `dotnet build -c Release` passes and `dotnet test -c Release` is green
- [ ] Verified in-game on at least one dry run, plus one supervised real hand-in when this touches the run, the buying or the hand-in
- [ ] New or changed user-facing text is in `L*.cs` and all nine catalogs, same keys in the same order
- [ ] If this changes user-visible behavior, README is updated
- [ ] If this touches the run, buying or the hand-in, relevant `[AGCS]` log lines make the sequence auditable
