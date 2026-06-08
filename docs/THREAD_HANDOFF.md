# StarAI Practice Client Thread Handoff

Last updated: 2026-06-07

## Repository

- Repo: `C:\starai\StarAI.PracticeClient`
- User taskbar entrypoint: `C:\starai\Start-StarAI-PracticeClient.cmd`
- Reset baseline: 기존 tracked/untracked 파일을 제거하고 `.git`만 보존한 뒤 새 .NET 8 골격으로 재시작함
- Current version: `1.1.0`
- Last verified implementation state: 1.1.0 minor release candidate for HUD-gated timer/APM, game-end cleanup, random/ladder matching, Remastered ladder maps, result/MMR history, runtime compatibility audit, AI-name hiding, and hotkey import/UI.

## Hard Rules

- 답변과 보고는 한국어 존댓말로 한다.
- release/tag/push/installer 배포는 사용자가 명시적으로 요청할 때만 한다.
- SCHNAIL/Remastered 원본은 읽기 전용이다.
- 사람 런타임: `C:\starai\SC116AI`
- AI 런타임: `C:\starai\SC116AI_ai`
- 사람 `bwapi.ini`의 `ai` 값은 비워야 한다.
- AI 클라이언트는 창모드, 음소거, APMAlert OFF가 기본이다.
- 독점 전체화면은 금지한다. 현재는 cnc-ddraw 기반 borderless/fullscreen 설정으로 해상도 강제변경을 피한다.
- CoachAI 또는 플레이어 유닛 제어 오버레이는 되살리지 않는다.
- MPQ 쓰기는 SCHNAIL과 같은 `SFmpq`/`org.jasperge.mpq.MPQEditor.addFile` 방식만 사용한다. `JMpqEditor` 직접 쓰기 모드는 listfile 누락 시 MPQ 손상 위험이 확인되어 금지한다.

## Current State

- Core:
  - `PracticePaths` / `RuntimeWritePolicy`
  - SCHNAIL `bots.dat` / `maps.dat` parser
  - bot-map compatibility filter
  - initial `PracticeLaunchPlanBuilder`
  - hotkey CSV editor model, `stat_txt.txt` patcher, TBL compiler integration, SFmpq runtime MPQ insert helper
  - Remastered/Battle.net `STR_*` hotkey importer for working CSV entries
  - SCHNAIL ELO -> SCR MMR/grade reference estimator
  - player-only ladder rating store and ELO result calculator
  - runtime provisioning for SCHNAIL maps/bots into player/AI runtime folders
  - user map catalog reader for `.scm`/`.scx`
  - Remastered ladder map reader with SCHNAIL compatibility map IDs
  - player/AI `bwapi.ini` and `wmode.ini` generation
  - session history store for launch/APM/result/MMR records
- App:
  - SCHNAIL-inspired Korean WinForms UI with Game/Settings/Hotkeys/History tabs
  - Hotkeys tab can import SCHNAIL CSV, import Battle.net/Remastered key-value hotkeys, save working CSV, and apply to `C:\starai\SC116AI\patch_rt.mpq`
  - Game tab shows ladder rating controls in ladder mode. Settings tab stores replay root, user map folder, Remastered ladder map folder, and the `AI 이름 가리기` option under `%APPDATA%\StarAI.PracticeClient\settings.json`
  - History tab reads `%APPDATA%\StarAI.PracticeClient\history.json` and displays mode/result/MMR delta/result source with a dark table style
  - Launch flow starts player StarCraft with cnc-ddraw borderless/fullscreen settings and starts the AI client muted, then minimizes it after join/start timing
  - Overlay shows timer/APM without enabling APMAlert, starts only after in-game HUD detection, and is disposed on game end
  - After HUD detection, auto_menu is disabled in both local runtimes to avoid returning to the room/menu automation after the user leaves the game
  - Game/session finalization stops only captured local StarCraft runtime processes and never targets user Remastered windows
  - `--smoke` entrypoint
- Scripts:
  - `scripts\smoke.ps1`
  - `scripts\smoke-app-start.ps1`
  - Current WIP `smoke-app-start.ps1` accepts `-BotName`, `-MapName`, `-DryRun`, and `-PrepareOnly`
- Decision log:
  - `docs\TECH_DECISIONS.md`
  - 기능별 후보/장단점/선택 이유를 먼저 기록하고 구현한다.
- Added optional goal:
  - 봇 난이도를 SCHNAIL ELO, 가능하면 한국 서버 래더 MMR/등급 기준으로 병행 표기한다.
  - 사용자 맵, 리플레이 경로, 전적/APM 기록은 구현됨.
  - 봇 빌드 선택과 Remastered 직접 실행은 보류. 봇별 설정 구조와 BWAPI/Remastered 호환성 조사가 필요하다.

## MPQ Recovery Note

During development, unsafe direct `JMpqEditor` write attempts damaged local `patch_rt.mpq` copies. The local runtime and SCHNAIL bundled copies were restored from the intact `starcraft_bundled_forAI\patch_rt.mpq` copy. The current code no longer uses that unsafe writer path. If exact original SCHNAIL bundled player MPQ fidelity matters later, reinstall or repair SCHNAIL from the official source before comparing hashes.

## Verification

```powershell
dotnet test .\StarAI.PracticeClient.sln -v:minimal
.\scripts\smoke.ps1
.\scripts\smoke-app-start.ps1
```

Last known local verification:

- `dotnet test .\StarAI.PracticeClient.sln -v:minimal`: 74 passed
- `.\scripts\smoke.ps1`: Release build warning 0 / error 0
- `.\scripts\smoke-app-start.ps1 -BotName 'Dragon' -MapName '(4)Fighting Spirit'`: passed on 2026-06-07
  - `playerState=InGame`
  - `aiState=InGame`
  - `inGame=True`
  - `aiInGame=True`
  - `timerOverlay=True`
  - Actual evidence: `artifacts\screenshots\smoke-start-player-overlay.png`, `smoke-start-player-final.png`, `smoke-start-ai-final.png`
  - During smoke the player `bwapi.ini` kept `ai =` empty and the AI `bwapi.ini` used `bwapi-data\AI\StarAI\Bots\Dragon\dragon.dll`

## 2026-06-07 Continuation Notes

- Verified the previous bug reports before editing:
  - Actual UI/smoke showed HUD detection false negatives on 2560x1440/borderless and terrain false positives.
  - Manual in-game quit produced `Outcome=Unknown` with `ResultSource=player-left-ingame:GameRoom`.
  - Random selection allowed a non-DLL bot (`PurpleWave`), which violated the AI-DLL-only launch rule.
  - History tab screenshot showed no result/MMR columns and white DataGridView headers.
  - Hotkeys tab lacked Battle.net/Remastered import buttons.
- Implemented:
  - More tolerant HUD panel detection and stricter pre-game room color checks.
  - Captured Brood War window PID handling and process cleanup for local runtime processes with restricted process metadata.
  - Game-end lifecycle finalization: result resolution, overlay disposal, input hook disposal, local runtime process stop.
  - Outcome resolver: bot result log wins; ladder player quit/process exit without a bot result remains unknown; sparring player quit/process exit becomes abandoned.
  - ELO update for ladder only, with fixed AI rating from bot ELO and player-only rating persistence.
  - DLL-only bot candidates for UI/random/ladder matching.
  - Random bot/map options and Remastered ladder maps using SCHNAIL compatibility IDs.
  - `AI 이름 가리기` option: default hides as `StarAIBot`; unchecked reveals the selected bot name as the AI character name.
  - Dark History tab with mode/result/MMR/source columns.
  - Remastered/Battle.net hotkey importer and SCHNAIL icon-based 3x3 command-card hotkey UI.
  - Known-bad runtime compatibility filter for Fighting Spirit variants:
    - `Feint` is blocked after `Steamhammer.dll` access-violation crashes were found in `C:\starai\SC116AI_ai\Errors\2026 Jun 07.txt` for `(4)Fighting_Spirit 1.4.scx`.
    - `ICELab` is blocked on Fighting Spirit variants after user-observed in-game stop on the same local runtime/map family.
- Remaining known limitation:
  - Natural win/loss without a bot result log still needs a stronger replay/BWAPI event/score-screen parser. Current fallback is intentionally conservative and does not assign a ladder win/loss from player quit/process exit alone.

## 2026-06-06 WIP Handoff

The current worktree is intentionally dirty and not committed. Do not reset it unless the user explicitly asks.

Modified tracked files:

- `scripts\smoke-app-start.ps1`
- `src\StarAI.PracticeClient.App\MainForm.cs`
- `src\StarAI.PracticeClient.App\PracticeOverlayForm.cs`
- `src\StarAI.PracticeClient.App\Program.cs`
- `src\StarAI.PracticeClient.App\SmokeChecks.cs`
- `src\StarAI.PracticeClient.App\StarCraftBorderlessWindow.cs`
- `src\StarAI.PracticeClient.Core\SessionMetrics.cs`
- `tests\StarAI.PracticeClient.Tests\StarAI.PracticeClient.Tests.csproj`

Untracked WIP files:

- `src\StarAI.PracticeClient.App\Properties\AssemblyInfo.cs`
- `src\StarAI.PracticeClient.App\StarCraftScreenState.cs`
- `tests\StarAI.PracticeClient.Tests\PracticeSessionClockTests.cs`
- `tests\StarAI.PracticeClient.Tests\StarCraftScreenAnalyzerTests.cs`

Implemented in this WIP:

- Added `PracticeSessionClock` so timer/APM elapsed time is based on real UTC time from actual in-game HUD detection.
- Moved launcher session history/APM hook/overlay startup in `MainForm` until `StarCraftScreenDetector.WaitForInGameAsync` succeeds.
- Added `StarCraftScreenAnalyzer` / `StarCraftScreenDetector` to distinguish menu, room, pregame wait, dialogs, and in-game HUD.
- Improved local StarCraft window activation/bounds helpers so smoke captures the intended local 1.16.1 window instead of a user Remastered window.
- Made overlay topmost and reassert topmost periodically so it remains visible above StarCraft.
- Strengthened actual launch smoke to check player in-game, AI in-game, timer overlay visibility, and to save screenshots.
- Added tests for real-time clock behavior and screen-state classification.

Important observations:

- A user Remastered StarCraft window may be open during verification. Ignore it and never kill it. Only local runtimes under `C:\starai\SC116AI` and `C:\starai\SC116AI_ai` are test/control targets.
- In the latest successful actual smoke, both local clients entered the game and the Dragon bot DLL loaded on the AI client.
- The player screen can still show `ERROR: Failed to load the AI Module ""` because the human `bwapi.ini` intentionally keeps `ai =` empty. BWAPI source inspection indicates this is the human client's empty module warning, not proof that the AI bot failed. Do not “fix” it by putting a bot/CoachAI DLL into the human runtime. If suppressing this message is pursued, find a way that preserves a human `ai` value that is effectively empty/no-op and does not reattach CoachAI or player unit control.
- A temporary ignored reference clone was created under `artifacts\deps\bwapi-src` to inspect BWAPI source; it is under ignored `artifacts/` and should not be committed.
- At handoff time, no release/tag/push/commit has been done for this WIP.

## 2026-06-08 Runtime Fix Notes

- The startup chat error was reproduced with early trace screenshots, not inferred:
  - `ERROR: Could not find ai under ai in "C:\starai\SC116AI\bwapi-data\bwapi.ini".`
  - `ERROR: Failed to load the AI Module ""`.
- Root cause:
  - The human client still needs BWAPI for `auto_menu` room automation, but a plain BWAPI player runtime without tournament mode tries to load a normal AI module.
  - Removing or blanking `[ai] ai` alone still prints a red BWAPI error.
- Fix:
  - Human runtime keeps `[ai] ai` absent/empty.
  - Human runtime now sets `[ai] tournament = bwapi-data\TM\TournamentModule.dll`.
  - Human launch inherits SCHNAIL-style TournamentModule environment variables to suppress TM drawing overlays.
  - AI runtime keeps `tournament =` empty and only the selected bot DLL is written to `[ai] ai`.
- Timer/APM startup:
  - The red BWAPI error text was misclassified as `GameRoom`, delaying HUD detection.
  - `StarCraftScreenAnalyzer` now treats a real HUD with dark bottom panels as `InGame` even if red startup chat is visible.
  - Smoke now stores early startup trace frames under `artifacts\screenshots\startup-trace`.
- AI shutdown:
  - Smoke and app cleanup send `F10`, `Q`, `Q` to the captured AI Brood War window handle before termination.
  - This avoids the player-side disconnect wait because the AI leaves the game before cleanup kills the process.
  - Covered/background AI windows should not block shutdown; screen capture of a covered AI window can report the player screen.
- Latest actual smoke evidence:
  - `.\scripts\smoke-app-start.ps1 -BotName 'Dragon' -MapName '(4)Fighting Spirit'`: passed
  - `inGame=True`, `aiInGame=True`, `timerOverlay=True`
  - `aiShutdownSent=True`, `aiProcessGoneAfterCleanup=True`, `playerAfterAiShutdownState=GameRoom`, `aiGracefulShutdown=True`
  - No local StarCraft/ChaosLauncher processes remained after cleanup.

## 2026-06-08 Ladder Compatibility and Map Preview Follow-up

- Verified before editing:
  - `C:\starai\SC116AI_ai\Errors\2026 Jun 08.txt` contained `Stone.dll` access violations on `(4)Jade.scx`.
  - The same log contained `LetaBot.dll` access violations on `(4)Fighting_Spirit 1.4.scx`.
  - `Sapphire`/`Gems` config existed under `bwapi-data\AI\StarAI\Bots\Sapphire\Gems_config.json`, while the bot error screen expected `bwapi-data\AI\Gems_config.json`.
  - Current launcher smoke screenshots showed the map preview panel was gone.
  - `scripts\smoke.ps1` printed launcher smoke failure but returned exit code 0 because native command exit codes were not checked.
- Implemented:
  - Known-bad compatibility exclusions now also block:
    - `LetaBot` + Fighting Spirit variants
    - `Stone` + Fighting Spirit variants
    - `Stone` + Jade variants
  - The exclusions live in `PracticeCatalogCompatibility`, so bot list, map list, ladder candidates, and random pair generation share the same filter.
  - AI runtime provisioning mirrors selected bot config sidecars such as `Gems_config.json` into `bwapi-data\AI` while still copying the full bot folder under `bwapi-data\AI\StarAI\Bots\<BotName>`.
  - `scripts\smoke.ps1` now fails if `dotnet build` or launcher smoke returns non-zero.
  - `smoke-app-start.ps1` now accepts `-Mode`, `-PlayerRace`, and `-EnemyRace`.
  - Game tab map preview panel is restored. Remastered ladder maps reuse the linked SCHNAIL map preview image when their own image is absent.
  - Launcher smoke now selects a concrete map and fails if the map preview control/image is missing.
- Latest verification:
  - `dotnet test .\StarAI.PracticeClient.sln -v:minimal`: 99 passed.
  - `.\scripts\smoke.ps1`: passed, warning 0 / error 0.
  - `.\scripts\smoke-app-start.ps1 -DryRun -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4 [Remastered Ladder]'`: selected compatible Terran bot.
  - `.\scripts\smoke-app-start.ps1 -DryRun -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4 [Remastered Ladder]' -BotName 'LetaBot'`: failed as expected because blocked.
  - `.\scripts\smoke-app-start.ps1 -DryRun -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4 [Remastered Ladder]' -BotName 'Stone'`: failed as expected because blocked.
  - `.\scripts\smoke-app-start.ps1 -PrepareOnly -BotName 'Sapphire' -MapName '(4)Fighting Spirit' -PlayerRace Protoss -EnemyRace Terran`: passed; `C:\starai\SC116AI_ai\bwapi-data\AI\Gems_config.json` exists.
  - `.\scripts\smoke-app-start.ps1 -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4 [Remastered Ladder]' -BotName 'Dragon'`: passed with `inGame=True`, `aiInGame=True`, `timerOverlay=True`, `aiGracefulShutdown=True`.
  - No local StarCraft/ChaosLauncher processes remained after cleanup.

## 2026-06-08 Full Compatibility Audit Follow-up

- Added `PracticeCompatibilityAuditor` and `.\scripts\audit-compatibility.ps1`.
- The audit loads the same merged catalog as the launcher, enumerates every declared DLL bot-map pair, and writes CSVs under `artifacts\compatibility-audit`.
- The audit fails if a currently compatible pair has:
  - missing bot source directory
  - missing bot executable
  - missing map source file
  - runtime crash evidence from local AI runtime error logs
- Shared DLL crash evidence without a bot directory is kept in `runtime-crashes.csv` but not promoted to a blocking issue if multiple current bots use the same DLL name.
- Latest audit result:
  - `bots=86`
  - `dllBots=61`
  - `maps=31`
  - `declaredDllPairs=1050`
  - `compatibleDllPairs=1041`
  - `blockedDeclaredDllPairs=9`
  - `issues=0`
  - `runtimeCrashes=6`
- Current blocked declared pairs are:
  - `Feint` + `(4)Fighting Spirit`
  - `Feint` + `(4)Fighting Spirit 1.4 [Remastered Ladder]`
  - `ICELab` + `(4)Fighting Spirit`
  - `ICELab` + `(4)Fighting Spirit 1.4 [Remastered Ladder]`
  - `LetaBot` + `(4)Fighting Spirit`
  - `LetaBot` + `(4)Fighting Spirit 1.4 [Remastered Ladder]`
  - `Stone` + `(4)Fighting Spirit`
  - `Stone` + `(4)Fighting Spirit 1.4 [Remastered Ladder]`
  - `Stone` + `(4)Jade`
- This is exhaustive static/log auditing, not exhaustive dynamic boot testing for all 1041 compatible pairs.
