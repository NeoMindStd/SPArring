# AIStarClient

AIStarClient is a Windows practice launcher for local StarCraft: Brood War
1.16.1 + BWAPI sparring. It helps select a bot, map, race, game speed, CoachAI
build overlay, hotkeys, and replay/history paths from one launcher UI.

This repository contains only the AIStarClient application. It intentionally
does not redistribute StarCraft, ChaosLauncher, BWAPI runtime binaries, CoachAI,
SCHNAIL, BWAPI Revamped packages, or third-party bot binaries.

## Current version

`0.1.0`

Semantic versioning is used:

- Patch: bug fixes, bot metadata corrections, smoke-test improvements.
- Minor: new launcher features, installer/setup flow improvements.
- Major: incompatible config/storage/runtime behavior changes.

## Requirements

- Windows 10/11
- .NET 8 Desktop Runtime, or the self-contained release package when available
- A user-owned StarCraft: Brood War 1.16.1 folder
- A compatible BWAPI/ChaosLauncher setup in that StarCraft folder
- Optional: CoachAI installed under the selected StarCraft folder

Recommended local layout while developing:

```text
C:\starai\
  StarAI.PracticeClient\   # this repository
  SC116AI\                 # local runtime folder, not committed
```

## Run locally

From this repository:

```powershell
dotnet run --project .\src\StarAI.PracticeClient.App\StarAI.PracticeClient.App.csproj
```

Or build Release and run:

```powershell
dotnet build .\src\StarAI.PracticeClient.App\StarAI.PracticeClient.App.csproj -c Release
.\src\StarAI.PracticeClient.App\bin\Release\net8.0-windows\StarAI.PracticeClient.App.exe
```

## Practice flow

1. Select your StarCraft 1.16.1 folder.
2. Select your race, opponent race, difficulty, bot, map, and bot build.
3. Optionally enable CoachAI and choose a build overlay.
4. Click `스파링 시작`.
5. The launcher starts the player client first, waits for the host room to be
   created, then starts the AI client with sound disabled.

Known-crashing local bots are blocked before launch when crash evidence is known
from local StarCraft error logs. At the moment `XIAOYICOG2019` and `Stone` are
blocked because they produced access-violation crashes in this environment.

## Smoke test

The smoke test is intentionally non-invasive: it does not start StarCraft,
ChaosLauncher, or any bot. It only builds, runs unit tests, publishes the client,
and checks that release output does not contain common forbidden runtime/game
files.

```powershell
.\scripts\smoke.ps1
```

## Build a release package

```powershell
.\scripts\build-release.ps1
```

This creates:

- `artifacts\AIStarClient-<version>-win-x64.zip`
- `artifacts\installer\AIStarClient-<version>-Setup.exe` if Inno Setup is installed

The installer installs AIStarClient only. The user still selects their existing
StarCraft/BWAPI/ChaosLauncher folder from the app.

## Release policy

Git tags use semantic versioning:

```powershell
git tag v0.1.0
git push origin main --tags
```

The GitHub Actions release workflow builds a zip and installer from tagged
commits. Release artifacts must not contain:

- `StarCraft.exe`, `StarEdit.exe`, or MPQ files
- ChaosLauncher executables
- `BWAPI.dll`
- replays, error dumps, local screenshots, or local runtime folders

## Third-party and legal notes

See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

Important summary:

- StarCraft/Brood War assets are proprietary Blizzard Entertainment assets and
  are not distributed here.
- BWAPI is referenced as a separate user-installed runtime. GitHub identifies
  `bwapi/bwapi` as LGPL-3.0 plus an additional unknown license file.
- CoachAI and BWAPI Revamped did not expose a detectable GitHub license at the
  time this notice was written, so they are not redistributed here.
- ChaosLauncher and SCHNAIL redistribution terms are not verified here, so they
  are not redistributed here.

## Repository

Remote:

```text
https://github.com/NeoMindStd/AIStarClient
```
