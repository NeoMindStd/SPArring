# StarAI Practice Client Thread Handoff

Last updated: 2026-06-05

## Repository

- Repo: `C:\starai\StarAI.PracticeClient`
- User taskbar entrypoint: `C:\starai\Start-StarAI-PracticeClient.cmd`
- Reset baseline: 기존 tracked/untracked 파일을 제거하고 `.git`만 보존한 뒤 새 .NET 8 골격으로 재시작함
- Current version: `1.0.0`
- Last verified implementation state: rebuilt .NET 8 WinForms/Core launcher with 44 passing tests and actual StarCraft start capture

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
  - SCHNAIL ELO -> SCR MMR/grade reference estimator
  - runtime provisioning for SCHNAIL maps/bots into player/AI runtime folders
  - user map catalog reader for `.scm`/`.scx`
  - player/AI `bwapi.ini` and `wmode.ini` generation
  - session history store for launch/APM records
- App:
  - SCHNAIL-inspired Korean WinForms UI with Game/Settings/Hotkeys/History tabs
  - Hotkeys tab can import SCHNAIL CSV, save working CSV, and apply to `C:\starai\SC116AI\patch_rt.mpq`
  - Settings tab stores replay root and user map folder under `%APPDATA%\StarAI.PracticeClient\settings.json`
  - History tab reads `%APPDATA%\StarAI.PracticeClient\history.json`
  - Launch flow starts player StarCraft with cnc-ddraw borderless/fullscreen settings and starts the AI client muted, then minimizes it after join/start timing
  - Overlay shows timer/APM without enabling APMAlert
  - `--smoke` entrypoint
- Scripts:
  - `scripts\smoke.ps1`
  - `scripts\smoke-app-start.ps1`
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

- `dotnet test .\StarAI.PracticeClient.sln -v:minimal`: 36 passed
- `.\scripts\smoke.ps1`: Release build warning 0 / error 0
  - `.\scripts\smoke-app-start.ps1`: passed after cnc-ddraw and delayed AI minimize implementation
  - Actual launch evidence: player game started, AI client joined and was minimized after startup
  - Launcher UI evidence: `artifacts\screenshots\starai-launcher-hotkeys-smoke.png`
