# AIStarClient

AIStarClient is a Windows practice launcher for local StarCraft: Brood War
1.16.1 + BWAPI sparring. It focuses on a SCHNAIL-like one-click practice flow:
pick your race, opponent race, bot, map, difficulty, and bot build, then launch
a local human-vs-bot game.

This repository contains only the AIStarClient application. It intentionally
does not redistribute StarCraft, ChaosLauncher, BWAPI runtime binaries, SCHNAIL,
BWAPI Revamped packages, or third-party bot binaries.

## Current version

`0.1.5`

Semantic versioning is used:

- Patch: bug fixes, bot metadata corrections, smoke-test improvements.
- Minor: new launcher features, installer/setup flow improvements.
- Major: incompatible config/storage/runtime behavior changes.

## Requirements

- Windows 10/11
- .NET 8 Desktop Runtime, or a self-contained package when available
- A user-owned StarCraft: Brood War 1.16.1 folder
- A compatible BWAPI + ChaosLauncher setup in that StarCraft folder
- Local bot DLLs installed under the selected StarCraft folder

Recommended local layout while developing:

```text
C:\starai\
  StarAI.PracticeClient\   # this repository
  SC116AI\                 # local runtime folder, not committed
  SC116AI_ai\              # generated bot runtime folder, not committed
```

## Run locally

From this repository:

```powershell
dotnet run --project .\src\StarAI.PracticeClient.App\StarAI.PracticeClient.App.csproj
```

The taskbar entry used on this machine points to:

```text
C:\starai\Start-StarAI-PracticeClient.cmd
```

That command rebuilds the local run folder and starts the app.

## Practice flow

1. Select your StarCraft 1.16.1 folder.
2. Select your race, opponent race, difficulty, bot, map, and bot build.
3. Choose windowed/W-MODE or fullscreen behavior.
4. Click `스파링 시작`.
5. AIStarClient writes a human-host `bwapi.ini` in the selected StarCraft
   folder, syncs a separate `<StarCraft folder>_ai` runtime for the bot, starts
   the first StarCraft client, waits for the Local PC room, then starts the bot
   client from the AI runtime with its own bot-join `bwapi.ini`.

The player host role writes:

- `ai =` empty
- selected map and player race
- `character_name = StarAIHuman`
- sound ON

The bot join role writes:

- selected bot DLL as `ai`
- empty map so it joins the existing Local PC game
- bot race and selected game name
- `character_name = StarAIBot`
- sound OFF

Known-crashing local bots are excluded from the selectable bot list when crash
evidence is known from local StarCraft error logs. At the moment
`XIAOYICOG2019` and `Stone` are hidden because they produced access-violation
crashes in this environment.

## Features

- Bot pool with race, difficulty tier, ELO metadata, tags, and Korean notes
- Build/opening filters where a bot exposes configurable strategy files
- Map selection from the selected StarCraft folder
- Windowed/W-MODE toggle and optional StarCraft mouse confinement
- Hotkey import/editor support
- Replay auto-save path under `D:\OneDrive\Documents\StarCraft\Maps\Replays\ai`
- Match history and replay browser
- Last selected map, bot, build, filters, speed, and window options are saved

## Stability model

AIStarClient uses two local runtime folders: the selected player folder and a
generated sibling folder named `<StarCraft folder>_ai`. The player folder always
keeps `ai =` empty, while the AI folder receives the selected bot DLL. This is
required because BWAPI can read `bwapi.ini` when the game begins, not only when
the process starts. Keeping the files separate prevents the human client from
loading the bot AI. ChaosLauncher windows are not kept open concurrently; each
StarCraft instance is started with `RunScOnStartup`.

## Smoke test

The default smoke test is intentionally non-invasive: it does not start
StarCraft, ChaosLauncher, or any bot. It builds, runs unit tests, verifies
source-level regression guards, publishes the client to a temporary smoke
folder, and checks that output does not contain common forbidden runtime/game
files.

```powershell
.\scripts\smoke.ps1
```

For launch-flow changes, run the live smoke scripts on the local machine:

```powershell
.\scripts\smoke-chaos-autostart.ps1
.\scripts\smoke-app-start.ps1
```

These scripts stop only local `C:\starai` StarCraft/ChaosLauncher processes and
restore preferences/INI files after the check.

## Build a release package

Only build release artifacts when explicitly requested:

```powershell
.\scripts\build-release.ps1
```

The installer installs AIStarClient only. The user still selects their existing
StarCraft/BWAPI/ChaosLauncher folder from the app.

Release artifacts must not contain:

- `StarCraft.exe`, `StarEdit.exe`, or MPQ files
- ChaosLauncher executables unless redistribution terms are verified first
- `BWAPI.dll`
- replays, error dumps, local screenshots, or local runtime folders

## Third-party and legal notes

See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

Important summary:

- StarCraft/Brood War assets are proprietary Blizzard Entertainment assets and
  are not distributed here.
- BWAPI is referenced as a separate user-installed runtime. GitHub identifies
  `bwapi/bwapi` as LGPL-3.0 plus an additional unknown license file.
- BWAPI Revamped did not expose a detectable GitHub license at the time this
  notice was written, so it is not redistributed here.
- ChaosLauncher and SCHNAIL redistribution terms are not verified here, so they
  are not redistributed here.

## Repository

Remote:

```text
https://github.com/NeoMindStd/AIStarClient
```
