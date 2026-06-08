# StarAI Practice Client 1.2.0

Minor release focused on making StarAI standalone from SCHNAIL and making ladder play behave like an actual MMR ladder.

## Highlights

- StarAI now ships bundled bot/map/hotkey assets under `data`, so end users do not need SCHNAIL Client installed.
- Release packaging includes the bundled `data` folder and fails early if required bot/map catalogs are missing.
- Ladder matching now uses current player MMR to weight bot selection toward similarly rated opponents.
- Random-map ladder first picks an MMR-near bot, then picks one of that bot's compatible maps.
- Ladder results can fall back to the human runtime TournamentModule `gameState.txt` when a bot does not write a result log.
- Ladder wins now grant at least +1 point even when standard Elo rounding would produce 0.
- The latest local Halo ladder win was repaired to `1454 (+1)` based on TournamentModule evidence.

## Verification

- `dotnet test .\StarAI.PracticeClient.sln -v:minimal`
- `.\scripts\smoke.ps1`
- `.\scripts\audit-compatibility.ps1`
- `.\scripts\smoke-app-start.ps1 -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4 [Remastered Ladder]' -BotName 'Dragon'`
- `.\scripts\build-release.ps1`

## Install

1. Download `StarAI-PracticeClient-1.2.0-win-x64.zip`.
2. Extract it.
3. Run `install.cmd`.
4. Launch `C:\starai\Start-StarAI-PracticeClient.cmd`.
