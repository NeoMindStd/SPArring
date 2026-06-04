# AIStarClient Thread Handoff

Last updated: 2026-06-05

## Repository

- Repo: `C:\starai\StarAI.PracticeClient`
- Remote: `https://github.com/NeoMindStd/AIStarClient`
- User taskbar entrypoint: `C:\starai\Start-StarAI-PracticeClient.cmd`
- Current app version: `0.1.5`
- Latest handoff commit: current `HEAD` with message `docs: add thread handoff context`
- Last known good code commit: `b1abe4f fix: avoid exclusive fullscreen launch`
- Current expected working tree at handoff: clean.

## User Preferences

- Answer in Korean 존댓말.
- Be careful and test regressions; user is sensitive to repeated breakage.
- Do not create release artifacts, GitHub releases, tags, pushes, or installer uploads unless explicitly requested.
- The user launches from the Windows taskbar through `C:\starai\Start-StarAI-PracticeClient.cmd`; preserve that path and behavior.
- If editing code, prefer TDD/regression tests and then smoke tests.

## Product Direction

AIStarClient is now a SCHNAIL-like local StarCraft 1.16.1 + BWAPI sparring launcher, not a CoachAI overlay launcher.

Keep these decisions:

- CoachAI is removed from active flow. Do not reintroduce CoachAI or any overlay that can control the player's units.
- Player client and AI client must use separate runtime folders:
  - Player: `C:\starai\SC116AI`
  - AI: `C:\starai\SC116AI_ai`
- Player `bwapi.ini` must keep `ai =` empty.
- AI `bwapi.ini` receives the selected bot DLL.
- AI client is always windowed, sound OFF, APMAlert OFF.
- Player client avoids old exclusive fullscreen. It should use W-MODE and optional borderless window placement.
- Launcher saved preferences are only startup defaults. Once the app is open, current UI selections must drive `스파링 시작`.
- APMAlert is optional and experimental. If recent StarCraft `.ERR` logs mention `APMAlert.bwl`, the app disables it.

## Current Launch Flow

Normal flow in `MainForm.StartSparringAsync`:

1. Read current UI values with `CurrentSettings()`.
2. Stop stale local `C:\starai` StarCraft/ChaosLauncher processes only.
3. Import hotkeys from SCHNAIL/Remastered when available.
4. Validate selected player runtime.
5. Ensure AI runtime via `StarCraftRuntimeRoot.EnsureAiRoot`.
6. Force AI settings with `StarCraftRoot = aiRoot` and `WindowedMode = true`.
7. Apply W-MODE:
   - Player: `windowedMode: true`, clip cursor only if UI says so.
   - AI: `windowedMode: true`, `clipCursor: false`.
8. Apply player-host `bwapi.ini` and launch player via ChaosLauncher `RunScOnStartup`.
9. If borderless mode is checked, `StarCraftBorderlessWindow` removes frame and fits the StarCraft window to the current monitor.
10. Close the launcher window.
11. Apply bot-join `bwapi.ini` in AI runtime.
12. Launch AI via ChaosLauncher `RunScOnStartup`.
13. Close the launcher window.
14. Record match history and rely on BWAPI replay autosave path.

## SCHNAIL State

SCHNAIL was accidentally modified earlier in the old thread and then restored.

Current restored SCHNAIL expectations:

- SCHNAIL install: `C:\Program Files (x86)\SCHNAIL Client`
- `res\client.properties` should contain:
  - `schnail_remote_url = https://app.schnail.com:8181/schnail-serv`
  - `client_version=0.4.2.3`
- `bots\bots.dat` should not contain `[StarAI]` notes.
- Do not add helper `.cmd` files back into the SCHNAIL install folder.
- Helper scripts remain outside the install under `C:\starai\SCHNAIL_HELPERS`.
- The official SCHNAIL version endpoint was verified with HTTP 200:
  - `https://app.schnail.com:8181/schnail-serv/rest/version/`

If SCHNAIL login breaks again, first check `client.properties` for `127.0.0.1:18181`; that was the previous failure.

## Important Files

- App UI/flow: `src\StarAI.PracticeClient.App\MainForm.cs`
- Borderless window placement: `src\StarAI.PracticeClient.App\StarCraftBorderlessWindow.cs`
- Mouse clipping: `src\StarAI.PracticeClient.App\StarCraftMouseClipper.cs`
- Preferences: `src\StarAI.PracticeClient.App\LauncherPreferences.cs`
- BWAPI/player/bot INI config: `src\StarAI.PracticeClient.Core\PracticeConfigurator.cs`
- ChaosLauncher registry setup: `src\StarAI.PracticeClient.Core\ChaosLauncherConfigurator.cs`
- Launch/process cleanup: `src\StarAI.PracticeClient.Core\PracticeLauncher.cs`
- AI runtime sync: `src\StarAI.PracticeClient.Core\StarCraftRuntimeRoot.cs`
- Bot catalog/metadata: `src\StarAI.PracticeClient.Core\PracticeCatalog.cs`
- APMAlert crash guard: `src\StarAI.PracticeClient.Core\CrashLogInspector.cs`
- Regression tests: `tests\StarAI.PracticeClient.Tests\PracticeConfiguratorTests.cs`
- Smoke tests:
  - `scripts\smoke.ps1`
  - `scripts\smoke-app-start.ps1`
  - `scripts\smoke-chaos-autostart.ps1`

## Verification Policy

For code changes:

```powershell
dotnet test .\StarAI.PracticeClient.sln -v:minimal
.\scripts\smoke.ps1
```

If StarCraft/ChaosLauncher launch behavior changes, also run:

```powershell
.\scripts\smoke-app-start.ps1
```

After smoke tests, clean temporary artifacts and leave only `artifacts\run` when possible. Do not delete the taskbar launcher or the run folder needed by the taskbar flow.

## Known Issues / Next Investigation Candidates

- User reported previous issues now addressed in commit `b1abe4f`:
  - Exclusive fullscreen broke all monitor resolutions and window positions.
  - APMAlert appeared in a crash call stack.
  - Mouse sensitivity felt abnormal, likely due to old fullscreen/low-resolution mode.
- If mouse sensitivity is still abnormal after borderless W-MODE, inspect Chaosplugin mouse-speed settings or any StarCraft-specific registry/plugin setting before changing app logic.
- APM/game-time display through APMAlert is risky in this setup. Prefer OFF unless a safer display method is implemented.
- Avoid touching `C:\Program Files (x86)\SCHNAIL Client` except for read-only diagnostics or explicitly requested repair.

## Recent Commits

- `HEAD docs: add thread handoff context`
- `b1abe4f fix: avoid exclusive fullscreen launch`
- `9fbee6d fix: respect live launcher settings`
- `25cc169 fix: isolate bot runtime from player client`
- `e06bb54 refactor: remove CoachAI practice flow`
