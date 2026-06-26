# Sparring Thread Handoff

Last updated: 2026-06-26

## Repository

- Repo: `C:\starai\StarAI.PracticeClient-1.3.0` / installed app root `C:\sparring`
- User entrypoint: desktop/start-menu shortcut targeting `Sparring.Client.exe`
- Reset baseline: 기존 tracked/untracked 파일을 제거하고 `.git`만 보존한 뒤 새 .NET 8 골격으로 재시작함
- Current version: `1.6.0`
- Last verified implementation state: 1.6.0 release candidate with Neo practice bots marked as development preview, excluded from ladder/random, plus all 1.5.x launcher/runtime hotfixes merged.
- Current WIP: release packaging/publish may still be pending until the current turn completes. Do not reset/revert local changes unless the user explicitly asks.

## Hard Rules

- 답변과 보고는 한국어 존댓말로 한다.
- release/tag/push/installer 배포는 사용자가 명시적으로 요청할 때만 한다.
- Sparring 제품 실행은 기본 설치 기준 `C:\sparring\data` 내장 자산을 사용해야 한다. SCHNAIL은 개발/릴리즈 import 소스일 뿐 최종 사용자 필수 조건이 아니다.
- SCHNAIL/Remastered 원본은 읽기 전용이다.
- 사람 런타임: `C:\sparring\SC116AI`
- AI 런타임: `C:\sparring\SC116AI_ai`
- 사람 `bwapi.ini`의 `ai` 값은 비워야 한다.
- AI 클라이언트는 창모드, 음소거, APMAlert OFF가 기본이다.
- 독점 전체화면은 금지한다. 현재는 cnc-ddraw 기반 borderless/fullscreen 설정으로 해상도 강제변경을 피한다.
- CoachAI 또는 플레이어 유닛 제어 오버레이는 되살리지 않는다.
- MPQ 쓰기는 SCHNAIL과 같은 `SFmpq`/`org.jasperge.mpq.MPQEditor.addFile` 방식만 사용한다. `JMpqEditor` 직접 쓰기 모드는 listfile 누락 시 MPQ 손상 위험이 확인되어 금지한다.

## Current State

- Core:
  - `PracticePaths` / `RuntimeWritePolicy`
  - Sparring `data` asset catalog loader and SCHNAIL-compatible `bots.dat` / `maps.dat` parser
  - bot-map compatibility filter
  - initial `PracticeLaunchPlanBuilder`
  - hotkey CSV editor model, `stat_txt.txt` patcher, TBL compiler integration, SFmpq runtime MPQ insert helper
  - Remastered/Battle.net `STR_*` hotkey importer for working CSV entries
  - SCHNAIL ELO -> SCR MMR/grade reference estimator
  - player-only ladder rating store and ELO result calculator
  - runtime provisioning for Sparring bundled maps/bots into player/AI runtime folders
  - user map catalog reader for `.scm`/`.scx`
  - Remastered ladder map reader with SCHNAIL compatibility map IDs
  - player/AI `bwapi.ini` and `wmode.ini` generation
  - session history store for launch/APM/result/MMR records
- App:
  - SCHNAIL-inspired Korean WinForms UI with Game/Settings/Hotkeys/History tabs
  - Hotkeys tab can import Sparring default CSV, import Battle.net/Remastered key-value hotkeys, save working CSV, and apply to `C:\sparring\SC116AI\patch_rt.mpq`
  - Game tab shows ladder rating controls in ladder mode. Settings tab stores replay root, user map folder, Remastered ladder map folder, and the `AI 이름 가리기` option under `%APPDATA%\Sparring\settings.json`
  - History tab reads `%APPDATA%\Sparring\history.json` and displays mode/result/MMR delta/result source with a dark table style
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
dotnet test .\Sparring.sln -v:minimal
.\scripts\smoke.ps1
.\scripts\smoke-app-start.ps1
```

Latest 1.3.0 release-candidate verification:

- `dotnet test .\Sparring.sln -v:minimal`: 147 passed.
- `.\scripts\smoke.ps1`: build warning 0 / error 0, launcher smoke passed.
- `.\scripts\build-release.ps1`: produced `Sparring-1.3.0-setup.exe` and `Sparring-1.3.0-win-x64.zip`.
- Isolated runtime setup smoke using a temporary fake StarCraft 1.16.1 source and temporary player/AI runtime roots: passed; BWAPI, TournamentModule, cnc-ddraw, and AI runtime files were created without touching `C:\sparring\SC116AI`.
- `.\scripts\audit-compatibility.ps1`: `issues=0`.
- `.\scripts\smoke-app-start.ps1 -DryRun -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4 [Remastered Ladder]'`: selected a compatible DLL Terran bot.

1.3.0 packaging notes:

- Primary user installer: `Sparring-1.3.0-setup.exe`.
- The setup app is a self-contained WinForms installer with an embedded payload ZIP.
- The setup app asks for install path, StarCraft 1.16.1 source folder, desktop shortcut option, and optional launch after install.
- The setup app validates `StarCraft.exe`, `stardat.mpq`, `broodat.mpq`, and `patch_rt.mpq` before copying.
- `scripts\setup-runtime.ps1` copies the user-provided StarCraft source into `C:\sparring\SC116AI`, installs public BWAPI/cnc-ddraw/TournamentModule runtime files, then seeds `C:\sparring\SC116AI_ai`.
- `C:\sparring\Start-Sparring.cmd` is no longer created from 1.3.1 onward; desktop/start-menu shortcuts target `Sparring.Client.exe`.

Last known local verification:

- `dotnet test .\Sparring.sln -v:minimal`: 74 passed
- `.\scripts\smoke.ps1`: Release build warning 0 / error 0
- `.\scripts\smoke-app-start.ps1 -BotName 'Dragon' -MapName '(4)Fighting Spirit'`: passed on 2026-06-07
  - `playerState=InGame`
  - `aiState=InGame`
  - `inGame=True`
  - `aiInGame=True`
  - `timerOverlay=True`
  - Actual evidence: `artifacts\screenshots\smoke-start-player-overlay.png`, `smoke-start-player-final.png`, `smoke-start-ai-final.png`
  - During smoke the player `bwapi.ini` kept `ai =` empty and the AI `bwapi.ini` used `bwapi-data\AI\Sparring\Bots\Dragon\dragon.dll`

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
  - `AI 이름 가리기` option: default hides as `SparringBot`; unchecked reveals the selected bot name as the AI character name.
  - Dark History tab with mode/result/MMR/source columns.
  - Remastered/Battle.net hotkey importer and SCHNAIL icon-based 3x3 command-card hotkey UI.
  - Known-bad runtime compatibility filter for Fighting Spirit variants:
    - `Feint` is blocked after `Steamhammer.dll` access-violation crashes were found in `C:\sparring\SC116AI_ai\Errors\2026 Jun 07.txt` for `(4)Fighting_Spirit 1.4.scx`.
    - `ICELab` is blocked on Fighting Spirit variants after user-observed in-game stop on the same local runtime/map family.
- Remaining known limitation:
  - Natural win/loss without a bot result log still needs a stronger replay/BWAPI event/score-screen parser. Current fallback is intentionally conservative and does not assign a ladder win/loss from player quit/process exit alone.

## 2026-06-06 WIP Handoff

The current worktree is intentionally dirty and not committed. Do not reset it unless the user explicitly asks.

Modified tracked files:

- `scripts\smoke-app-start.ps1`
- `src\Sparring.Client\MainForm.cs`
- `src\Sparring.Client\PracticeOverlayForm.cs`
- `src\Sparring.Client\Program.cs`
- `src\Sparring.Client\SmokeChecks.cs`
- `src\Sparring.Client\StarCraftBorderlessWindow.cs`
- `src\Sparring.Core\SessionMetrics.cs`
- `tests\Sparring.Tests\Sparring.Tests.csproj`

Untracked WIP files:

- `src\Sparring.Client\Properties\AssemblyInfo.cs`
- `src\Sparring.Client\StarCraftScreenState.cs`
- `tests\Sparring.Tests\PracticeSessionClockTests.cs`
- `tests\Sparring.Tests\StarCraftScreenAnalyzerTests.cs`

Implemented in this WIP:

- Added `PracticeSessionClock` so timer/APM elapsed time is based on real UTC time from actual in-game HUD detection.
- Moved launcher session history/APM hook/overlay startup in `MainForm` until `StarCraftScreenDetector.WaitForInGameAsync` succeeds.
- Added `StarCraftScreenAnalyzer` / `StarCraftScreenDetector` to distinguish menu, room, pregame wait, dialogs, and in-game HUD.
- Improved local StarCraft window activation/bounds helpers so smoke captures the intended local 1.16.1 window instead of a user Remastered window.
- Made overlay topmost and reassert topmost periodically so it remains visible above StarCraft.
- Strengthened actual launch smoke to check player in-game, AI in-game, timer overlay visibility, and to save screenshots.
- Added tests for real-time clock behavior and screen-state classification.

Important observations:

- A user Remastered StarCraft window may be open during verification. Ignore it and never kill it. Only local runtimes under `C:\sparring\SC116AI` and `C:\sparring\SC116AI_ai` are test/control targets.
- In the latest successful actual smoke, both local clients entered the game and the Dragon bot DLL loaded on the AI client.
- The player screen can still show `ERROR: Failed to load the AI Module ""` because the human `bwapi.ini` intentionally keeps `ai =` empty. BWAPI source inspection indicates this is the human client's empty module warning, not proof that the AI bot failed. Do not “fix” it by putting a bot/CoachAI DLL into the human runtime. If suppressing this message is pursued, find a way that preserves a human `ai` value that is effectively empty/no-op and does not reattach CoachAI or player unit control.
- A temporary ignored reference clone was created under `artifacts\deps\bwapi-src` to inspect BWAPI source; it is under ignored `artifacts/` and should not be committed.
- At handoff time, no release/tag/push/commit has been done for this WIP.

## 2026-06-08 Runtime Fix Notes

- The startup chat error was reproduced with early trace screenshots, not inferred:
  - `ERROR: Could not find ai under ai in "C:\sparring\SC116AI\bwapi-data\bwapi.ini".`
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
  - `C:\sparring\SC116AI_ai\Errors\2026 Jun 08.txt` contained `Stone.dll` access violations on `(4)Jade.scx`.
  - The same log contained `LetaBot.dll` access violations on `(4)Fighting_Spirit 1.4.scx`.
  - `Sapphire`/`Gems` config existed under `bwapi-data\AI\Sparring\Bots\Sapphire\Gems_config.json`, while the bot error screen expected `bwapi-data\AI\Gems_config.json`.
  - Current launcher smoke screenshots showed the map preview panel was gone.
  - `scripts\smoke.ps1` printed launcher smoke failure but returned exit code 0 because native command exit codes were not checked.
- Implemented:
  - Known-bad compatibility exclusions now also block:
    - `LetaBot` + Fighting Spirit variants
    - `Stone` + Fighting Spirit variants
    - `Stone` + Jade variants
  - The exclusions live in `PracticeCatalogCompatibility`, so bot list, map list, ladder candidates, and random pair generation share the same filter.
  - AI runtime provisioning mirrors selected bot config sidecars such as `Gems_config.json` into `bwapi-data\AI` while still copying the full bot folder under `bwapi-data\AI\Sparring\Bots\<BotName>`.
  - `scripts\smoke.ps1` now fails if `dotnet build` or launcher smoke returns non-zero.
  - `smoke-app-start.ps1` now accepts `-Mode`, `-PlayerRace`, and `-EnemyRace`.
  - Game tab map preview panel is restored. Remastered ladder maps reuse the linked SCHNAIL map preview image when their own image is absent.
  - Launcher smoke now selects a concrete map and fails if the map preview control/image is missing.
- Latest verification:
  - `dotnet test .\Sparring.sln -v:minimal`: 99 passed.
  - `.\scripts\smoke.ps1`: passed, warning 0 / error 0.
  - `.\scripts\smoke-app-start.ps1 -DryRun -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4 [Remastered Ladder]'`: selected compatible Terran bot.
  - `.\scripts\smoke-app-start.ps1 -DryRun -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4 [Remastered Ladder]' -BotName 'LetaBot'`: failed as expected because blocked.
  - `.\scripts\smoke-app-start.ps1 -DryRun -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4 [Remastered Ladder]' -BotName 'Stone'`: failed as expected because blocked.
  - `.\scripts\smoke-app-start.ps1 -PrepareOnly -BotName 'Sapphire' -MapName '(4)Fighting Spirit' -PlayerRace Protoss -EnemyRace Terran`: passed; `C:\sparring\SC116AI_ai\bwapi-data\AI\Gems_config.json` exists.
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
- Shared DLL crash evidence without a bot directory is promoted for every still-compatible candidate using that DLL, so shared-DLL bot families are handled conservatively instead of silently keeping siblings selectable.
- Latest audit result:
  - `bots=86`
  - `dllBots=61`
  - `maps=31`
  - `declaredDllPairs=1050`
  - `compatibleDllPairs=1003`
  - `blockedDeclaredDllPairs=47`
  - `issues=0`
  - `runtimeCrashes=24`
- Current blocked declared pairs are:
  - `Feint` + `(4)Fighting Spirit`
  - `Feint` + `(4)Fighting Spirit 1.4 [Remastered Ladder]`
  - `Crazyhammer` + `(4)Fighting Spirit`
  - `Crazyhammer` + `(4)Fighting Spirit 1.4 [Remastered Ladder]`
  - `Randomhammer` + `(4)Fighting Spirit`
  - `Randomhammer` + `(4)Fighting Spirit 1.4 [Remastered Ladder]`
  - `Steamhammer` + `(4)Fighting Spirit`
  - `Steamhammer` + `(4)Fighting Spirit 1.4 [Remastered Ladder]`
  - `CUBOT` + `(4)Fighting Spirit`
  - `CUBOT` + `(4)Fighting Spirit 1.4 [Remastered Ladder]`
  - `ICELab` + `(4)Fighting Spirit`
  - `ICELab` + `(4)Fighting Spirit 1.4 [Remastered Ladder]`
  - `LetaBot` + `(4)Fighting Spirit`
  - `LetaBot` + `(4)Fighting Spirit 1.4 [Remastered Ladder]`
  - `Stone` + all 16 declared maps, after repeated `Stone.dll` access violations on Fighting Spirit, Jade, and Benzene.
  - `RedRum` + all declared maps, until a validated supported-map whitelist exists.
  - `Yuanheng Zhu` + `(4)Andromeda`
- This is exhaustive static/log auditing, not exhaustive dynamic boot testing for all 1003 compatible pairs.

## 2026-06-08 Sparring Bundled Asset Follow-up

- User clarified that SCHNAIL was only a development reference and must not be a product/runtime dependency.
- Added `PracticeAssetCatalogReader` and `PracticeAssetPaths`.
- Product catalog/hotkey/map preview loading now uses Sparring-owned `data` under the resolved app root. The default installed app root is now `C:\sparring`.
- Added `scripts\import-schnail-assets.ps1` to copy local SCHNAIL assets into Sparring-owned `data` during development/release preparation.
- Imported current local assets:
  - `data\bots`: 100 bot folders plus `bots.dat`
  - `data\maps`: 59 map/preview files plus `maps.dat`
  - `data\res`: hotkey CSV, messages, icon assets, TBL compiler data
  - `data\tools\mpq\schnail-client.exe`: MPQ writer classpath used by the hotkey patcher
- `scripts\build-release.ps1` now fails if `data` is missing and includes it in the release ZIP/install copy.
- Final user install docs must not list SCHNAIL as a requirement.

## 2026-06-08 Ladder Result GameState Follow-up

- Reproduced the user-reported Halo ladder match result issue from local evidence before editing:
  - `%APPDATA%\Sparring\history.json` had the latest Halo ladder match as `Outcome=Unknown` with `ResultSource=player-left-ingame:GameRoom`.
  - `C:\sparring\SC116AI\bwapi-data\gameState.txt` was written during the same match and reported `defeated=0`, `victorious=1`, `gameOver=1` for `SparringHuman`.
  - The AI runtime did not have a fresh bot result log for that match, so the previous resolver could not confirm the result.
- Added `TournamentGameStateReader` as a second result source after bot result logs and before quit/process-exit fallback.
- Ladder result precedence is now:
  - bot result log if present
  - human runtime TournamentModule `bwapi-data\gameState.txt` if it is fresh for the session
  - existing conservative fallback (`Unknown` for ladder player quit/process exit)
- Corrected the latest local Halo history record to `PlayerWin`, based on the gameState evidence.
- Follow-up scoring rule: 1453 beating Halo 692 produced a zero-point rounded Elo change with the original formula, so Sparring now applies a custom ladder floor where every win grants at least +1 point.
- The same local Halo record and current rating were adjusted to `PlayerRatingAfter=1454`, `RatingDelta=+1`, `RatingText=1454 (+1)`.

## 2026-06-08 Ladder MMR Matching Follow-up

- Reproduced the user's matchmaking concern from code:
  - `LadderBotSelector.PickRandom` selected uniformly from compatible candidates.
  - `MainForm.ResolveLadderSelection` used that uniform random selector, so a 1453 MMR player could still draw Halo 692 at the same per-candidate probability as near-MMR bots.
- Implemented MMR-aware ladder matching:
  - fixed-map ladder: pick a compatible bot with distance-based weight around the current player MMR.
  - random-map ladder: pick the bot first with current-MMR weighting across all enabled compatible maps, then pick one compatible map for that bot.
  - low-MMR bots are not hard-blocked, but their probability drops sharply as rating distance grows.
- Updated the ladder details UI to say MMR-weighted matching and show MMR-nearest candidates first.
- Added regression tests in `LadderBotSelectorTests` to verify near-MMR bots dominate the selection distribution and random-map ladder still uses rating weight.

## 2026-06-08 Ladder Win Floor Follow-up

- Added `EloCalculatorGuaranteesAtLeastOnePointForAnyWin`.
- `EloRatingCalculator.Calculate` still uses the Elo expected-score formula, but if a `PlayerWin` rounds to no gain, it forces `after = playerRating + 1`.
- Loss and draw calculations remain unchanged.
- `LoadCatalog()` now also calls `RefreshRatingUi()` so the Game tab refresh button picks up rating file changes.

## 2026-06-10 Alt+F4 Exit Follow-up

- Reproduced the user-reported exit-error path before editing with the real launcher UI flow:
  - Starting from the WinForms launcher, a local player `Brood War` window and AI `Brood War Instance 2` window were active.
  - Sending Alt+F4 to the player window left an AI-side Windows application error dialog in one reproduction.
  - The fresh AI runtime log showed `RedRum.dll` access violation on `(4)Jade.scx`; this was a separate compatibility regression found during the exit repro.
  - A later release-candidate UI verification selected `Stone.dll` on `(2)Benzene.scx` and reproduced another AI-side access violation.
  - Another release-candidate UI verification selected `CUBOT.dll` on `(4)Fighting Spirit.scx` and reproduced another AI-side access violation.
  - Another release-candidate UI verification selected `Yuanheng Zhu` / `Juno.dll` on `(4)Andromeda.scx` and reproduced another AI-side access violation.
- Implemented:
  - `GlobalInputActionHook` now intercepts Alt+F4 only when the foreground process is the captured player StarCraft PID.
  - The intercepted Alt+F4 is not passed directly to StarCraft. Sparring sends the normal in-game leave sequence (`F10`, `Q`, `Q`), closes the player process, then runs the existing AI graceful shutdown/finalization path.
  - `RedRum` is excluded from all declared compatible maps until a validated supported-map whitelist exists.
  - `Stone` is excluded from all declared compatible maps until runtime safety is proven.
  - `CUBOT` is blocked on Fighting Spirit variants.
  - `Yuanheng Zhu` is blocked on Andromeda variants.
  - Random/sparring candidate filtering now removes bots with no currently compatible maps, and launch resolution rechecks explicit bot-map compatibility.
- Regression tests added:
  - `GlobalInputActionHookTests` verifies Alt+F4 interception is limited to the captured player PID and does not catch other keys/windows.
  - `PracticeCatalogCompatibilityTests` covers `RedRum` Jade/Fighting Spirit blocking while keeping other whitelisted RedRum maps selectable, `Stone` + `(2)Benzene`, full `Stone` map exclusion, `CUBOT` Fighting Spirit variants, and `Yuanheng Zhu` + `(4)Andromeda`.
- Latest verification:
  - `dotnet test .\Sparring.sln -v:minimal`: 121 passed.
  - `.\scripts\smoke.ps1`: warning 0 / error 0.
  - `.\scripts\audit-compatibility.ps1`: `compatibleDllPairs=1024`, `blockedDeclaredDllPairs=26`, `issues=0`, `runtimeCrashes=10`.
  - `.\scripts\smoke-app-start.ps1 -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4 [Remastered Ladder]' -BotName 'Dragon'`: passed with `inGame=True`, `aiInGame=True`, `timerOverlay=True`, `aiGracefulShutdown=True`.
  - Foreground Alt+F4 UI automation was stopped after user safety feedback; Alt+F4 interception remains covered by unit tests and non-foreground smoke paths.
- Computer Use note:
  - The Computer Use plugin skill was read and bootstrap was attempted.
  - Current local helper failed to load with `Package subpath './dist/project/cua/sky_js/src/targets/windows/internal/computer_use_client_base.js' is not defined by "exports" ... @oai/sky\package.json`.
  - Windows UI verification therefore used direct Win32/UIAutomation automation after the plugin helper failure.

## 2026-06-12 RedRum Fighting Spirit Follow-up

- Verified before editing:
  - `C:\sparring\SC116AI_ai\Errors\2026 Jun 12.txt` had repeated `RedRum.dll` access violations on `(4)Fighting_Spirit 1.4.scx`.
  - Earlier runtime evidence also had `RedRum.dll` access violation on `(4)Jade.scx`.
  - A partially executed RedRum runtime matrix added `RedRum.dll` access-violation evidence on `(2)Benzene`, `(2)Destination`, `(2)Heartbreak Ridge`, `(3)Neo Moon Glaive`, and `(3)Tau Cross`.
  - `%APPDATA%\Sparring\history.json` had `RedRum` ladder entries on `(4)Fighting Spirit 1.4 [Remastered Ladder]` ending as `AI 종료` after 1-6 seconds.
- Implemented:
  - `RedRum` is now excluded from all declared maps until a validated supported-map whitelist exists.
  - Local package inspection found no trustworthy supported-map list in `bots.dat`, `RedRum.zip`, or `RedRum-0_1.json`; the only map-specific RedRum read data references Fighting Spirit, which has crash evidence.
  - `Crazyhammer`, `Randomhammer`, and `Steamhammer` are blocked on Fighting Spirit variants because the same `Steamhammer.dll` family has crash evidence there.
  - Compatibility audit now promotes shared-DLL crash evidence for every still-compatible candidate instead of ignoring ambiguous shared module names.
  - The exclusion is in `PracticeCatalogCompatibility`, so bot list, map list, ladder candidates, random selection, and launch-time validation share it.
- Regression coverage:
  - `PracticeCatalogCompatibilityTests.RedRumIsExcludedUntilAValidatedSupportedMapWhitelistExists` covers evidence-based RedRum filtering.
  - `PracticeCatalogCompatibilityTests.KnownBadRuntimePairsAreNotCompatible` covers the Steamhammer-family Fighting Spirit variants.
  - `PracticeCompatibilityAuditorTests.AuditReportsSharedDllCrashForEveryStillCompatibleCandidate` covers shared-DLL crash promotion.
- Latest verification:
  - `dotnet test .\Sparring.sln -v:minimal`: 133 passed.
  - `.\scripts\smoke.ps1`: warning 0 / error 0.
  - `.\scripts\audit-compatibility.ps1`: `compatibleDllPairs=1003`, `blockedDeclaredDllPairs=47`, `issues=0`, `runtimeCrashes=24`.
  - Compatibility audit CSV check: `RedRum` compatible pairs = 0, blocked declared pairs = 16.
  - `.\scripts\smoke-app-start.ps1 -DryRun -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -BotName 'RedRum'`: failed as expected because RedRum has no currently compatible maps.
  - `.\scripts\smoke-app-start.ps1 -DryRun -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(2)Benzene' -BotName 'RedRum'`: failed as expected because RedRum has no currently compatible maps.
  - `.\scripts\smoke-app-start.ps1 -DryRun -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit' -BotName 'RedRum'`: failed as expected because blocked.
  - `.\scripts\smoke-app-start.ps1 -DryRun -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4 [Remastered Ladder]' -BotName 'RedRum'`: failed as expected because blocked.
  - `.\scripts\smoke-app-start.ps1 -DryRun -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Jade' -BotName 'RedRum'`: failed as expected because blocked.
  - `.\scripts\smoke-app-start.ps1 -DryRun -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4 [Remastered Ladder]' -BotName 'Steamhammer'`: failed as expected because blocked.
  - `.\scripts\smoke-app-start.ps1 -DryRun -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4 [Remastered Ladder]' -BotName 'Crazyhammer'`: failed as expected because blocked.
  - `.\scripts\smoke-app-start.ps1 -DryRun -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4 [Remastered Ladder]' -BotName 'Randomhammer'`: failed as expected because blocked.
  - `.\scripts\smoke-app-start.ps1 -DryRun -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4 [Remastered Ladder]'`: selected another compatible Terran bot.

## 2026-06-12 Short History Compatibility Follow-up

- Verified before editing without Computer Use or foreground focus changes:
  - `%APPDATA%\Sparring\history.json` had 14 records under 60 seconds with `Unknown` or AI-exit style sources, grouped into 13 bot-map pairs.
  - None of those exact bot-map pairs had a normal win/loss history result.
  - `.\scripts\audit-compatibility.ps1` failed before the fix with 16 runtime crash issues from fresh local AI error logs.
- Classified and handled:
  - `KillAlll`, `Iron bot`, and `XIAOYICOG2019` are blocked on Fighting Spirit variants because `C:\sparring\SC116AI_ai\Errors\2026 Jun 12.txt` has repeated DLL access-violation evidence there.
  - `Zia bot` is blocked on Fighting Spirit variants because local history shows immediate AI exit on Fighting Spirit 1.4, the DLL/source sidecars exist, and no trustworthy supported-map whitelist was found.
  - `Crazyhammer` + `(4)Empire of the Sun`, `McRaveZ` + `(4)La Mancha1.1`, and `PurpleWave` + `(4)Polypoid 1.65` are exact-pair blocked from short unresolved history records with no matching normal result.
  - `Dragon` + Fighting Spirit and `Sapphire` + Fighting Spirit are intentionally kept compatible because previous local verification/config-sidecar fixes showed those runtime paths can work and no current crash evidence was found for them.
- Regression coverage:
  - `PracticeCatalogCompatibilityTests.KnownBadRuntimePairsAreNotCompatible` covers the newly blocked Fighting Spirit bot set.
  - `PracticeCatalogCompatibilityTests.ShortUnresolvedHistoryPairsWithoutNormalResultsAreNotCompatible` covers the exact short-history pair blocks.
  - `PracticeCatalogCompatibilityTests.OtherBotsCanStillUseFightingSpiritWhenDeclaredCompatible` and `SapphireCanStillUseFightingSpiritAfterConfigSidecarProvisioningFix` guard the basic known-good Fighting Spirit paths.

## 2026-06-21 Installer Prerequisite Follow-up

- Verified before editing:
  - SSCAIT/BWAPI tutorial still exists, but old direct StarCraft 1.16.1 mirror links can return 403/404.
  - Local SCHNAIL install contains `starcraft_bundled`, `starcraft_bundled_forAI`, `jre`, and `redists`, but this is not Sparring redistribution permission.
  - Bot DLL/EXE scan found x86 runtime dependencies including `MSVCP90/MSVCR90`, `MSVCP120/MSVCR120`, `VCRUNTIME140/MSVCP140`, and `api-ms-win-crt-*`.
  - Recent local AI error logs are mostly `EXCEPTION_ACCESS_VIOLATION`, not clear missing-runtime loader errors, so previous map-bot crashes were not reclassified as VC++ issues.
- Implemented:
  - Sparring setup still does not include StarCraft game files; users must point the installer at a valid StarCraft 1.16.1 source folder.
  - Setup UI now shows optional prerequisites before path selection:
    - VC++ x86 runtime install, checked by default and described as recommended for native bot loading.
    - Java runtime preparation, checked by default and described as required for custom hotkey MPQ patching.
    - .NET is documented as unnecessary because the app/setup are self-contained.
  - VC++ installers are downloaded from Microsoft official URLs at install time.
  - OpenJDK 17 is downloaded via the Adoptium API and extracted under the app install folder `runtime\jdk`, without changing system Java.
  - Hotkey MPQ patching now resolves bundled Java from `<app root>\runtime\jdk\bin\java.exe` before falling back to system Java.
  - Added `docs\INSTALL.md` and updated `README.md`/`docs\TECH_DECISIONS.md`.
- Latest verification:
  - `dotnet test .\Sparring.sln -v:minimal`: 148 passed.
  - `.\scripts\smoke.ps1`: Release build warning 0 / error 0.
  - `.\scripts\build-release.ps1`: produced updated `Sparring-1.3.0-setup.exe` and `Sparring-1.3.0-win-x64.zip`.
  - VC++/Adoptium dependency URLs returned HTTP 200 in HEAD checks.
  - No StarCraft/ChaosLauncher launch flow was changed, so `smoke-app-start.ps1` was not required for this change.

## 2026-06-21 1.3.1 Release Follow-up

- Implemented:
  - Removed the `C:\sparring\Start-Sparring.cmd` entrypoint from new setup generation and smoke requirements.
  - Setup now cleans up legacy `Start-Sparring.cmd` files on a best-effort basis.
  - Release/install docs now point users to multiple public 1.16.1 preparation pages with Markdown links instead of exposing a dead direct mirror URL.
  - Battle.net/Remastered 1.23.x downgrade is explicitly not supported; users must provide a separate 1.16.1 folder.
  - Added Windows Defender/antivirus false-positive guidance and links to Microsoft exclusion instructions.
- Version bumped to `1.3.1` and `docs\RELEASE_NOTES_1.3.1.md` added.
- Required verification for the release:
  - `dotnet test .\Sparring.sln -v:minimal`
  - `.\scripts\smoke.ps1`
  - `.\scripts\build-release.ps1`
  - Because launch flow itself was not changed, actual `smoke-app-start.ps1` is not required unless later changes touch StarCraft/ChaosLauncher execution.

## 2026-06-22 1.3.2 Installer UX Follow-up

- Setup UI now follows a familiar Windows wizard layout:
  - install path and StarCraft 1.16.1 source page
  - component selection page
  - progress/log page
  - finish state
- Default install path changed from `C:\sparring\Sparring` to `C:\sparring`.
- Setup builds a SHA-256 manifest from the embedded payload and verifies copied app/data files. It also verifies required app/runtime files after runtime provisioning.
- New release ZIP payload no longer includes `install.cmd`; Setup EXE is the supported installer path.
- Version bumped to `1.3.2` and `docs\RELEASE_NOTES_1.3.2.md` added.

## 2026-06-22 1.3.3 Setup Display Hotfix

- Fixed the setup path page layout for high-DPI/large-font environments by removing fixed label columns and fixed group heights.
- Setup window is now resizable and uses wider path pickers/buttons.
- Added `--ui-smoke` validation for setup layout, and `scripts\smoke.ps1` now renders/validates setup UI at normal, large, and extra-large font sizes.
- Version bumped to `1.3.3` and `docs\RELEASE_NOTES_1.3.3.md` added.

## 2026-06-23 Startup Integrity Repair WIP

- Added installed payload integrity repair:
  - setup writes `install-manifest.json`, `install-state.json`, and `install-cache\payload.zip`.
  - manifest excludes `install-manifest.json`, `install-state.json`, and `install-cache\*`.
  - launcher startup verifies the payload manifest before opening `MainForm`.
  - missing/hash-mismatched app payload files are restored from the cache when possible.
  - the running `Sparring.Client.exe` is intentionally not overwritten.
- Added runtime missing-file startup repair:
  - launcher checks required app/runtime files with the same required-file list as setup.
  - if player/AI runtime files are missing, launcher attempts noninteractive `scripts\setup-runtime.ps1` using the stored StarCraft source path when available.
  - if repair fails or files continue to disappear, launcher displays Windows Defender/protection-history guidance.
- `PracticePaths.ResolveApplicationRoot()` now treats a folder with `install-manifest.json` as an app root, so data-folder loss can still be repaired.
- `scripts\build-release.ps1` writes `install-manifest.json` into the payload stage for installer/launcher repair, but does not create a public checksum text asset.
- Added regression tests:
  - `InstallationVerifierTests` for manifest save/load, metadata exclusion, zip repair, and runtime missing-file reporting.
  - `StartupIntegrityCheckTests` for startup payload repair, cache-missing unresolved reporting, runtime repair callback, and Defender guidance text.

## 2026-06-23 Free Security/SmartScreen Mitigation WIP

- Added assembly metadata defaults in `Directory.Build.props`.
- This hotfix only includes free packaging, internal integrity repair, and user-guidance mitigations.
- `scripts\build-release.ps1` now:
  - passes version/file-version metadata into app/setup publishes.
  - produces `Sparring-<version>-setup-folder.zip`, a small setup EXE plus external `payload.zip`.
- Setup now supports `payload.zip` placed next to the setup EXE, not only embedded payload resources or a `payload` folder.
- Release pages should not expose checksum lists by default; integrity data is internal to setup/launcher repair.
- Added `scripts\new-defender-submission-package.ps1` for Microsoft Defender false-positive submission bundles.

## 2026-06-23 1.4.0 Responsive UX Follow-up

- User-facing release/README/install text must stay focused on normal StarCraft users. Do not add chat-specific notes, private validation details, or public checksum sections to release pages.
- Launcher UX:
  - launcher shell and main tabs resize with the window instead of leaving fixed-position dead space.
  - game tab is checked at small/default/wide sizes in launcher smoke.
  - combo boxes use a dark StarCraft-like owner-drawn style; smoke fails if default white dropdown rendering returns.
  - last mode, race filters, player race, bot, and map selection are persisted in `%APPDATA%\Sparring\settings.json`.
  - bot descriptions summarize English-only notes into Korean style/build/player-facing text.
- In-game overlay:
  - overlay width is measured from the actual timer/APM text and clamped to the game bounds to avoid clipping at different resolutions/DPI.
- Setup:
  - setup progress is determinate. `ProgressBarStyle.Marquee` / `SetBusy` is disallowed by `scripts\smoke.ps1`.
  - file copy and dependency download progress use actual copied/downloaded bytes where available.
- Runtime:
  - ChaosLauncher runtime registry writes temporarily set StarCraft intro/tip values so first-run intro screens are skipped, then restore the previous values after launch.
  - player/AI runtime preparation now creates `bwapi-data\write`, `bwapi-data\logs`, and `Errors` directories.
  - bundled ladder map presence is checked by launcher smoke.
- Release packaging:
  - `scripts\build-release.ps1` produces setup EXE, win-x64 ZIP, and setup-folder ZIP only. No public checksum text asset is generated.

## 2026-06-23 1.5.0 Sparring UX and Map Cleanup

- Product naming:
  - Source projects, solution, setup/client process names, release artifacts, README, and user-visible launcher title use `Sparring`.
  - Default installed app root remains `C:\sparring`; player/AI runtimes remain `C:\sparring\SC116AI` and `C:\sparring\SC116AI_ai`.
- Launcher UX:
  - Game/settings/hotkeys/history tabs are validated at compact/default/FHD/QHD/UHD sizes.
  - `SmokeChecks` now fails on visible important-control overlap in captured launcher tabs.
  - Hotkey UI groups worker command-card pages as general/basic structures/advanced structures/all.
  - Game speed, mouse scroll speed, and keyboard scroll speed can be configured before launching.
  - The update checker calls the GitHub latest-release API and prompts for download/install, later, or skip-version.
- Map cleanup:
  - `Fighting_Spirit_1.41_Official.scm` was excluded from bundled maps after actual 1.16.1 launch produced `The selected scenario is not valid.`
  - Keep `(4)Fighting Spirit 1.4` as the current bundled Fighting Spirit ladder variant unless a valid 1.16.1-compatible 1.41 file is obtained and verified.
  - `StarCraftScreenAnalyzer` now classifies create-screen red-frame/dialog states before HUD colors so invalid-scenario modals do not start the timer/APM path.
- Latest verification before release packaging:
  - `dotnet test .\Sparring.sln -v:minimal`: 179 passed.
  - `.\scripts\smoke.ps1`: warning 0 / error 0.
  - `.\scripts\audit-compatibility.ps1`: issues 0 / runtimeCrashes 0.
  - `.\scripts\smoke-app-start.ps1 -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4' -BotName 'Dragon'`: passed with `playerState=InGame`, `aiState=InGame`, `inGame=True`, `aiInGame=True`, `timerOverlay=True`, `aiGracefulShutdown=True`.

## 2026-06-25 NeoProtossF WIP

- Added first Sparring-owned BWAPI DLL bot:
  - Source: `src\Sparring.Bots\NeoProtossF`
  - Packaged DLL: `data\bots\NeoProtossF\NeoProtossF.dll`
  - Catalog name: `NeoProtossF`
  - Race/MMR baseline: Protoss / ELO 950
  - Registered maps: Fighting Spirit variants, Polypoid 1.65/1.75, Python, Circuit Breaker, Match Point.
- Opening behavior:
  - Randomly chooses among 1012, fast power dragoon, 23 Nexus, 29 Arbiter, Forge double, gate double Corsair/Dark Templar, naked double, fast Dark Templar, and forward-gate Dark Templar families by matchup.
  - Early visible threats near the main/natural trigger probe pull, Zealot training, and Cannon placement when Forge tech exists.
  - Active scouting is intentionally deferred after early BWAPI runtime instability during actual smoke; the bot uses visible enemy buildings and fallback start locations for attack targeting.
- Build support:
  - `scripts\build-neo-bots.ps1` builds the Win32 BWAPI module and copies the DLL into `data\bots\NeoProtossF`.
  - `scripts\build-release.ps1` calls the Neo bot build script before packaging.
- Verification completed:
  - `.\scripts\build-neo-bots.ps1 -SkipDownloads`: passed.
  - `dotnet test .\Sparring.sln -v:minimal`: 181 passed.
  - `.\scripts\smoke.ps1`: warning 0 / error 0.
  - `.\scripts\audit-compatibility.ps1`: `bots=87`, `dllBots=62`, `declaredDllPairs=1065`, `compatibleDllPairs=1008`, `blockedDeclaredDllPairs=57`, `issues=0`, `runtimeCrashes=0`.
  - `.\scripts\smoke-app-start.ps1 -PrepareOnly -Mode Sparring -PlayerRace Terran -EnemyRace Protoss -MapName '(4)Fighting Spirit 1.4' -BotName 'NeoProtossF'`: passed with `playerAi=''`, `aiModuleExists=True`.
- Actual launch smoke was attempted but is not currently trustworthy:
  - NeoProtossF, Fresh Meat, and the previously known-good Dragon all failed with the same `Brood War Instance 2` Windows application error at `0x1001CB6E` / read `0x00000148`.
  - The failure persisted after reseeding `C:\sparring\SC116AI_ai` from `scripts\setup-runtime.ps1`.
  - Because Dragon now fails the same way, this is currently classified as a local actual-smoke/runtime environment issue, not as proof that NeoProtossF is uniquely broken.

## 2026-06-25 Control Import / Neo Bot First-Cut Wrap

- Implemented before pausing:
  - Battle.net/Remastered control import now detects `CSettings.json` under StarCraft document/config folders.
  - `CSettings.json` hotkeys are parsed from the embedded `Hotkeys` string and applied to the working hotkey CSV entries.
  - Control import now also reads game speed, mouse sensitivity, mouse scroll speed, and keyboard scroll speed when present.
  - The Control tab now exposes a separate mouse sensitivity input, plus wheel scroll and keyboard scroll speed inputs.
  - Launch-time StarCraft registry writes now temporarily apply mouse sensitivity alongside scroll speed, then restore through the existing restore-point path.
  - NeoProtossF selectable build plumbing is in place: catalog `buildOptions`, launcher build combo in sparring mode, prepare-only smoke writes `build=<id>` into the AI bot config.
- Verification completed:
  - `dotnet test .\Sparring.sln -v:minimal`: 190 passed.
  - `.\scripts\build-neo-bots.ps1 -SkipDownloads`: passed.
  - `.\scripts\smoke.ps1`: build warning 0 / error 0.
  - `.\scripts\audit-compatibility.ps1`: `issues=0`, `runtimeCrashes=0`.
  - `.\scripts\smoke-app-start.ps1 -PrepareOnly -Mode Sparring -PlayerRace Terran -EnemyRace Protoss -MapName '(4)Fighting Spirit 1.4' -BotName 'NeoProtossF' -BotBuildId '23_nexus'`: passed; human `ai` stayed empty and AI config had `build=23_nexus`.
  - `.\scripts\smoke-app-start.ps1 -Mode Sparring -PlayerRace Terran -EnemyRace Protoss -MapName '(4)Fighting Spirit 1.4' -BotName 'NeoProtossF' -BotBuildId '23_nexus'`: passed with both clients in game, timer overlay visible, and graceful AI shutdown.
  - Local installed root `C:\sparring` was refreshed from the current WIP payload for manual follow-up, and `C:\sparring\Sparring.Client.exe` was launched.
- Deferred roadmap:
  - Do not continue NeoTerranF feature development until the user asks; it exists in WIP but was intentionally not expanded in this wrap.
  - NeoProtossF quality work remains: stronger opening execution, less brittle scouting/defense, better probe-pull limits, earlier return-to-mining, smarter GG timing, and investigation of whether any public BWAPI policy/model can be reused.
  - Control tab needs a later visual pass on the problematic Surface Book/high-DPI environment; automated smoke passed, but user asked to stop broad UI verification for now.
  - Battle.net import should be manually checked in the live launcher by pressing `컨트롤 > Battle.net` after the user is ready for focus-stealing UI checks.
  - If a new release is requested later, rebuild/package, run the required tests again, then release/tag/push/release-page update explicitly.

## 2026-06-26 Control UX / Neo Bot WIP Wrap

- Latest user instruction before this wrap:
  - Quickly finish the work already in progress, reflect the launcher changes, and leave unfinished items as a roadmap.
  - Do not start a new release/tag/push unless explicitly requested.
- Control tab UX changes completed:
  - The tab is still named `컨트롤`, but it now has explicit sections for `설정 불러오기`, `게임 조작 설정`, and `핫키 커스터마이징`.
  - Ambiguous buttons such as `Battle.net`, `CSV 저장`, and `런타임 반영` were replaced with player-facing labels:
    - `Battle.net 설정 불러오기`
    - `설정 폴더 선택`
    - `현재 단축키 저장`
    - `스타에 단축키 적용`
    - `조작 설정 저장`
  - Mouse sensitivity, wheel scroll speed, and keyboard scroll speed now use sliders with readable value labels instead of bare numeric boxes.
  - Hotkey detail text no longer exposes internal IDs/TBL/source strings to normal users.
  - Selected filter buttons now use a green selected style instead of a red warning-like border.
  - Hotkey layout was tightened to avoid filter/action/detail overlaps in the launcher smoke capture.
- Neo bot changes completed in this wrap:
  - `NeoProtossF` and `NeoTerranF` no longer build emergency supply before the scripted first supply building, fixing an early build-order regression.
  - Defense workers now return to mining if they chase too far from the main base.
  - When a Neo bot sends GG, it now calls `leaveGame()` shortly after instead of waiting until full elimination.
  - `NeoTerranF` selectable build packaging and prepare-only launch config were verified.
- Regression coverage added:
  - `RuntimeProvisionerTests.PrepareRuntimeAssetsRefreshesLegacyBuildConfigWhenSwitchingSelectableBots` guards against stale `sparring-bot.ini` build values after switching between selectable bots.
- Verification completed in this wrap:
  - `dotnet test .\Sparring.sln --filter RuntimeProvisionerTests -v:minimal`: 5 passed.
  - `dotnet test .\Sparring.sln -v:minimal`: 191 passed.
  - `dotnet run --project .\src\Sparring.Client\Sparring.Client.csproj -c Release -- --smoke`: passed.
  - `.\scripts\smoke.ps1`: Release build warning 0 / error 0.
  - `.\scripts\build-neo-bots.ps1 -SkipDownloads`: passed; copied updated `NeoProtossF.dll` and `NeoTerranF.dll` into `data\bots`.
  - `.\scripts\smoke-app-start.ps1 -PrepareOnly -Mode Sparring -PlayerRace Terran -EnemyRace Protoss -MapName '(4)Fighting Spirit 1.4' -BotName 'NeoProtossF' -BotBuildId '23_nexus'`: passed; AI config `build=23_nexus`.
  - `.\scripts\smoke-app-start.ps1 -PrepareOnly -Mode Sparring -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4' -BotName 'NeoTerranF' -BotBuildId 'factory_expand'`: passed; AI config `build=factory_expand`.
- Important verification note:
  - Do not run two `smoke-app-start.ps1` commands in parallel against the same `C:\sparring\SC116AI_ai` runtime. They share the AI runtime config file and can interfere with each other. Run actual StarCraft launch smokes serially.
- Deferred roadmap:
  - Launch the updated UI manually when the user is ready for foreground/focus changes and visually inspect the Control tab on the problematic Surface Book/high-DPI setup.
  - Run actual in-game smoke for `NeoProtossF` and `NeoTerranF` serially, then inspect early build execution, worker defense behavior, and GG/leave behavior from screenshots/logs.
  - Improve `NeoProtossF` beyond this first-cut: tighter build-order execution, matchup-aware transition rules, less brittle scout/attack targeting, and better anti-worker-harass handling.
  - Continue `NeoTerranF` quality work later; the current WIP has selectable openings and basic worker-defense/GG behavior, but it has not had the same gameplay review depth as the launcher changes.
  - Consider a user-facing build-selection help text pass in the Game tab after actual bot quality stabilizes.

## 2026-06-26 Completion Pass After Deferred Notes

This section supersedes the deferred roadmap notes immediately above.

- Control tab UX completion:
  - The control page is split into load-settings, game-control, and hotkey-customization sections.
  - Short ambiguous labels were replaced with labels that explain what each action does.
  - Mouse sensitivity, wheel scroll speed, and keyboard scroll speed use slider-style controls with readable values.
  - Hotkey detail text is player-facing and no longer exposes internal IDs/TBL/source strings by default.
  - Launcher smoke validates compact/default/FHD/QHD/UHD tab layouts and fails on important visible-control overlap.
- Neo bot completion:
  - `NeoProtossF` has selectable opening builds. Smoke verified `23_nexus` is written to the AI runtime config.
  - `NeoTerranF` has selectable opening builds. Smoke verified `factory_expand` is written to the AI runtime config.
  - `NeoProtossF` and `NeoTerranF` no longer place emergency supply before their scripted first supply building.
  - Defense workers return to mining after chasing too far from the main base.
  - GG flow calls `leaveGame()` shortly after resigning.
  - `NeoProtossF` 23 Nexus production was adjusted so the bot does not spend Nexus timing minerals on excessive early Dragoons.
- Screen-state fix:
  - `StarCraftScreenAnalyzer` now gives a strong in-game HUD priority over weak central bright/dialog-like pixels.
  - Genuine scenario-error/create-screen dialog cases remain classified as `BlockedDialog`/`GameRoom`.
  - Added regression coverage for in-game HUD plus central bright terrain/player-color pixels.
- Verification completed:
  - `dotnet test .\Sparring.sln --filter StarCraftScreenAnalyzerTests -v:minimal`: 11 passed.
  - `dotnet test .\Sparring.sln -v:minimal`: 192 passed.
  - `.\scripts\audit-compatibility.ps1`: `bots=88`, `dllBots=63`, `maps=34`, `declaredDllPairs=1073`, `compatibleDllPairs=1016`, `blockedDeclaredDllPairs=57`, `issues=0`, `runtimeCrashes=0`.
  - `.\scripts\smoke.ps1`: Release build warning 0 / error 0.
  - `.\scripts\smoke-app-start.ps1 -Mode Sparring -PlayerRace Terran -EnemyRace Protoss -MapName '(4)Fighting Spirit 1.4' -BotName 'NeoProtossF' -BotBuildId '23_nexus' -ObserveSeconds 240`: passed with `playerState=InGame`, `aiState=InGame`, `timerOverlay=True`, `aiGracefulShutdown=True`.
  - The NeoProtossF observe screenshot showed active 23 Nexus progress: 26/33 supply, second Nexus, Gateway/Cybernetics Core tech, and workers mining.
  - `.\scripts\smoke-app-start.ps1 -Mode Sparring -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4' -BotName 'NeoTerranF' -BotBuildId 'factory_expand' -ObserveSeconds 180`: passed with `playerState=InGame`, `aiState=InGame`, `timerOverlay=True`, `aiGracefulShutdown=True`.
  - The NeoTerranF observe screenshot showed active Terran progress: 19/26 supply, Barracks/Factory production path, SCVs mining, and Marines/Vultures moving.
  - `.\scripts\build-release.ps1`: produced fresh 1.5.0 setup/zip payloads and rebuilt both Neo bot DLLs.
  - Installed root `C:\sparring` was refreshed from `artifacts\release\payload-stage-1.5.0`.
  - `C:\sparring\Sparring.Client.exe --smoke`: passed.
- Operational note:
  - Run actual `smoke-app-start.ps1` checks serially because player and AI runtimes share `C:\sparring\SC116AI_ai` config during launch preparation.
  - No release/tag/push was done in this completion pass.

## 2026-06-26 NeoZergF Addition

- Added third Sparring-owned BWAPI DLL bot:
  - Source: `src\Sparring.Bots\NeoZergF`
  - Packaged DLL: `data\bots\NeoZergF\NeoZergF.dll`
  - Catalog name: `NeoZergF`
  - Race/MMR baseline: Zerg / ELO 950
  - Registered maps: Fighting Spirit variants, Polypoid 1.65/1.75, Python, Circuit Breaker, Match Point.
- Selectable opening builds:
  - All/ZvZ: `overpool_speed`, `nine_pool_speed`
  - ZvT: `two_hatch_muta`, `five_thirty_muta`, `lurker_contain`
  - ZvP: `three_hatch_hydra`, `five_hatch_hydra`, `nine_pool_speed`
- Implementation notes:
  - `NeoZergF` is BWAPI rule-based, not ML/model-based.
  - The launcher writes the selected build into `sparring-bot.ini`, and the bot reads both the bot-folder and legacy AI-root config paths.
  - Actual smoke uncovered two Zerg-specific issues before completion:
    - Zerg HUD colors were misclassified as `MenuLike`; `StarCraftScreenAnalyzer` now recognizes Zerg HUD panels and has a regression test.
    - Early Zerg production was stuck by egg/building counts and supply reservation. `NeoZergF` now counts morphing eggs/buildings for build decisions, avoids repeated Overlord production, reserves minerals for second Hatchery/Extractor, and falls back to a main macro Hatchery if third expansion placement fails.
- Verification completed:
  - `.\scripts\build-neo-bots.ps1 -SkipDownloads`: passed; copied `NeoZergF.dll` into `data\bots\NeoZergF`.
  - `dotnet test .\Sparring.sln --filter NeoZergBotCatalogTests -v:minimal`: 2 passed.
  - `dotnet test .\Sparring.sln --filter StarCraftScreenAnalyzerTests -v:minimal`: 12 passed.
  - `.\scripts\smoke-app-start.ps1 -PrepareOnly -Mode Sparring -PlayerRace Terran -EnemyRace Zerg -MapName '(4)Fighting Spirit 1.4' -BotName 'NeoZergF' -BotBuildId 'two_hatch_muta'`: passed; human `ai` stayed empty and AI config had `build=two_hatch_muta`.
  - `.\scripts\smoke-app-start.ps1 -Mode Sparring -PlayerRace Terran -EnemyRace Zerg -MapName '(4)Fighting Spirit 1.4' -BotName 'NeoZergF' -BotBuildId 'two_hatch_muta' -ObserveSeconds 240`: passed with `playerState=InGame`, `aiState=InGame`, `timerOverlay=True`, `aiGracefulShutdown=True`. Observe screenshot showed 2 Hatch Muta tech progress with gas and Lair/Spire path underway.
  - `.\scripts\smoke-app-start.ps1 -Mode Sparring -PlayerRace Protoss -EnemyRace Zerg -MapName '(4)Fighting Spirit 1.4' -BotName 'NeoZergF' -BotBuildId 'three_hatch_hydra' -ObserveSeconds 240`: passed with `playerState=InGame`, `aiState=InGame`, `timerOverlay=True`, `aiGracefulShutdown=True`. Observe screenshot showed multi-Hatchery tech progress.
  - `dotnet test .\Sparring.sln -v:minimal`: 195 passed.
  - `.\scripts\smoke.ps1`: Release build warning 0 / error 0.
  - `.\scripts\audit-compatibility.ps1`: `bots=89`, `dllBots=64`, `maps=34`, `declaredDllPairs=1081`, `compatibleDllPairs=1024`, `blockedDeclaredDllPairs=57`, `issues=0`, `runtimeCrashes=0`.
- No release/tag/push was done.

## 2026-06-26 Neo Bot Preview Release Follow-up

- Neo bot catalog policy:
  - `NeoProtossF`, `NeoTerranF`, and `NeoZergF` are development-preview practice bots.
  - Their catalog ELO is now unset, and the launcher displays them as `ELO -`.
  - They remain manually selectable in sparring mode.
  - They are excluded from ladder candidates and from sparring `Random` selection.
  - Their launcher descriptions warn that midgame judgment and detailed control may still feel awkward.
- Code policy:
  - `PracticeBotCandidatePolicy.IsLadderEligible` excludes practice-only or unrated bots.
  - `PracticeBotCandidatePolicy.IsSparringRandomEligible` excludes practice-only bots from random sparring selection.
- Neo bot quality roadmap:
  - Improve midgame transition after the scripted opening finishes.
  - Add scouting-based reactions for enemy tech, expansions, and army composition.
  - Make worker-defense behavior less brittle against small harassment.
  - Improve GG/leave timing based on actual fighting and production state.
  - Re-evaluate public ELO only after replay review across common maps and matchups.
- 1.6.0 verification:
  - `dotnet test .\Sparring.sln --filter "LadderBotSelectorTests|NeoProtossBotCatalogTests|NeoTerranBotCatalogTests|NeoZergBotCatalogTests" -v:minimal`: 14 passed.
  - `dotnet test .\Sparring.sln -v:minimal`: 196 passed.
  - `.\scripts\smoke.ps1`: Release build warning 0 / error 0.
  - `.\scripts\audit-compatibility.ps1`: `issues=0`, `runtimeCrashes=0`.
  - `.\scripts\smoke-app-start.ps1 -PrepareOnly -Mode Sparring -PlayerRace Terran -EnemyRace Zerg -MapName '(4)Fighting Spirit 1.4' -BotName 'NeoZergF' -BotBuildId 'two_hatch_muta'`: passed.
  - Ladder dry-run with `NeoProtossF` failed as expected because preview Neo bots are excluded from ladder candidates.
  - `.\scripts\build-release.ps1`: produced `Sparring-1.6.0-setup.exe`, `Sparring-1.6.0-win-x64.zip`, and `Sparring-1.6.0-setup-folder.zip`.

## 2026-06-24 1.5.1 High-DPI Launcher and AI Minimize Hotfix

- Verified before editing on the current problem PC:
  - Windows 11 Home 64-bit, Surface Pro 9, 2880x1920 display, 200% scaling.
  - Installed 1.5.0 launcher opened at about 579x405 captured pixels and showed Game tab content cut off with page scrollbars and cramped list/detail/button areas.
  - The running setup completion page was captured and did not show the same severe overlap as the launcher.
- Implemented:
  - Launcher initial sizing now uses most of small/high-DPI work areas while staying capped on large monitors.
  - Game tab launch button width and scroll sizing were adjusted so the default Surface/high-DPI window no longer shows unnecessary page scrollbars.
  - Settings and Hotkeys labels now reserve enough width for Korean text under 200% scaling.
  - Hotkeys action/filter buttons use shorter labels (`기본값`, `배틀넷`, `폴더`, `CSV`, `반영`, `Terr`, `Prot`, `공통`, `업글`, `기본`, `고급`) and avoid unnecessary maximized scrollbars.
  - Setup path page uses top-docked autosized content under the scroll panel so large-font mode can scroll instead of clipping the StarCraft source group.
  - AI window minimization now starts before player borderless enforcement and still minimizes once if the AI client is already detected as in-game.
  - `smoke-app-start.ps1` no longer reactivates the AI StarCraft window for verification and now requires `aiMinimized=True`.
  - `StarCraftScreenAnalyzer` now treats bright minerals and cropped game-world captures with green selection rings as in-game when no room/error frame is present.
- Regression coverage:
  - `LauncherWindowSizingTests` covers high-DPI/small-screen initial size policy and large-screen caps.
  - `AiWindowMinimizePolicyTests.StepOnceMinimizesWhenAiAlreadyReachedInGame` covers the in-game AI minimize path.
  - `StarCraftScreenAnalyzerTests` covers bright-mineral HUD and cropped-world AI captures.
- Direct UI verification:
  - Local self-contained launcher was checked with Computer Use at default, small, wide, low-height, and maximized sizes.
  - Final `artifacts\release\payload-stage-1.5.1\Sparring.Client.exe` was checked with Computer Use on the Surface 200% environment.
  - Game, Settings, Hotkeys, and History tabs were checked; final Settings/Hotkeys label clipping was fixed and rechecked.
  - Final Release `Sparring.Setup.dll` setup UI was checked directly; 1.5.1 setup EXE launch was slow/blocked during local UI inspection, while setup UI smoke passed for large and extra-large font.
- Final verification for the 1.5.1 hotfix:
  - `dotnet test .\Sparring.sln -v:minimal`: 188 passed.
  - `.\scripts\smoke.ps1`: warning 0 / error 0.
  - `.\scripts\audit-compatibility.ps1`: `issues=0`, `runtimeCrashes=0` after archiving local smoke shutdown error logs under `artifacts\compatibility-audit`.
  - `.\scripts\smoke-app-start.ps1 -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit' -BotName 'Dragon'`: passed with `playerState=InGame`, `aiState=InGame`, `inGame=True`, `aiInGame=True`, `aiMinimized=True`, `timerOverlay=True`, `aiGracefulShutdown=True`.
  - `.\scripts\build-release.ps1`: produced `Sparring-1.5.1-setup.exe`, `Sparring-1.5.1-win-x64.zip`, and `Sparring-1.5.1-setup-folder.zip`.

## 2026-06-24 1.5.2 AI Shutdown Error Hotfix

- Reproduced after the 1.5.1 release:
  - `smoke-app-start.ps1` with Dragon on `(4)Fighting Spirit` could leave `C:\sparring\SC116AI_ai\Errors\2026 Jun 24.txt`.
  - Crash evidence showed `EXCEPTION: 0xE06D7363`, `BWAPI.dll`, `StarCraft.exe DestroyGame`, and `preLoadGame`.
  - The Windows error dialog could remain on screen, which is not acceptable from the user perspective even if the match smoke otherwise reached in-game.
- Implemented:
  - AI cleanup no longer uses the StarCraft in-game quit menu path that triggers BWAPI `DestroyGame` shutdown exceptions.
  - ChaosLauncher/StarCraft child process launch now suppresses inherited Windows fault dialogs.
  - AI shutdown cleanup removes only the known BWAPI `DestroyGame`/`preLoadGame` shutdown crash files created during intentional AI disconnect, leaving unrelated crash evidence intact.
  - `smoke-app-start.ps1` now snapshots the AI runtime `Errors` folder and fails if new/changed runtime error files remain after AI cleanup.
- Validation evidence so far:
  - Targeted `StarCraftGameExitControllerTests`: passed.
  - `smoke-app-start.ps1 -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit' -BotName 'Dragon'`: passed with `aiRuntimeErrorsClean=True`, `aiRuntimeErrorFiles=none`, `aiGracefulShutdown=True`.

## 2026-06-24 1.5.3 Surface Compact UI Hotfix

- Rechecked on the current problem PC:
  - Windows 11 Home, Surface Pro 9, 1440x960 working area at 200% scaling.
  - `Downloads\Starcraft_1161` and `Downloads\Starcraft_1161.zip` exist as StarCraft 1.16.1 source candidates.
  - A leftover elevated `Sparring-1.5.0-setup.exe` process and its child `Sparring.Client.exe` could not be terminated from the current non-elevated tool context.
  - Launching `Sparring-1.5.2-setup.exe` reached the Windows UAC consent screen; actual install could not continue without user approval.
- Reproduced from launcher UI smoke screenshots:
  - Compact game tab had an oversized difficulty/ladder summary card that clipped text under 200% scaling.
  - Compact game and Hotkeys tabs used too much vertical space in the header/card/detail areas, making the minimum-size view feel cramped.
  - Status text at the bottom could be clipped at compact height.
- Implemented:
  - Launcher header and status row now use less vertical space while keeping status text readable.
  - Game tab summary card is single-line, smaller, and smoke-validated for text clipping.
  - Compact game tab uses a smaller map preview and more compact detail sizing.
  - Compact Hotkeys command card/detail blocks are reduced so more useful content is visible without overlap.
  - Setup first-screen wording now uses `Sparring을` instead of `Sparring를`.
  - Launcher UI smoke now fails when the important difficulty label text is clipped.
