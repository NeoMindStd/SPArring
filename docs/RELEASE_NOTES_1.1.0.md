# StarAI Practice Client 1.1.0

Minor release focused on making practice sessions safer and more useful in real play.

## Highlights

- Timer/APM overlay now starts after actual in-game HUD detection instead of at launcher click or room wait time.
- Human client uses BWAPI tournament mode without loading a normal AI module.
- AI client leaves the game before process cleanup to avoid player-side disconnect waits.
- Ladder and random matching now share compatibility filtering with Remastered ladder maps.
- Known broken local runtime combinations are blocked from bot/map candidates.
- Full compatibility audit command added for declared DLL bot-map pairs.
- Map preview panel restored on the Game tab.
- History, ladder MMR controls, result handling, AI-name hiding, and hotkey import/UI received the WIP improvements carried into this release.

## Verification

- `dotnet test .\StarAI.PracticeClient.sln -v:minimal`
- `.\scripts\smoke.ps1`
- `.\scripts\audit-compatibility.ps1`
- `.\scripts\smoke-app-start.ps1 -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4 [Remastered Ladder]' -BotName 'Dragon'`

## Compatibility Audit Snapshot

- Bots: 86
- DLL bots: 61
- Maps: 31
- Declared DLL bot-map pairs: 1050
- Compatible DLL pairs: 1041
- Blocked declared pairs: 9
- Audit issues: 0

## Install

1. Download `StarAI-PracticeClient-1.1.0-win-x64.zip`.
2. Extract it.
3. Run `install.cmd`.
4. Launch `C:\starai\Start-StarAI-PracticeClient.cmd`.
