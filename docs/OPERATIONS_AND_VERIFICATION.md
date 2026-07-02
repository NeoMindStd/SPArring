# Operations And Verification

## 사용자 실행 경로

사용자 진입점은 설치 프로그램이 만든 바탕화면/시작 메뉴 바로가기다. 바로가기는 설치 폴더의 EXE를 직접 실행한다.

```text
C:\sparring\Sparring.Client.exe
```

`C:\sparring\Start-Sparring.cmd`는 1.3.1부터 생성하지 않는 레거시 진입점이다.

## 개발 작업 전 확인

작업 시작 시 우선 읽을 문서:

```text
C:\sparring\Sparring\AGENTS.md
C:\sparring\Sparring\docs\THREAD_HANDOFF.md
C:\sparring\Sparring\docs\README.md
```

현재 작업트리가 dirty일 수 있다. 사용자가 명시적으로 요청하지 않는 한 reset/revert로 기존 변경을 날리지 않는다.

## 기본 검증

코드 변경 후 기본 검증:

```powershell
dotnet test .\Sparring.sln -v:minimal
.\scripts\smoke.ps1
```

StarCraft/ChaosLauncher 실행 흐름을 건드렸다면 실제 실행 smoke도 수행한다.

```powershell
.\scripts\smoke-app-start.ps1
```

특정 봇/맵 조합 검증:

```powershell
.\scripts\smoke-app-start.ps1 -BotName 'Dragon' -MapName '(4)Fighting Spirit'
```

런타임 준비만 확인:

```powershell
.\scripts\smoke-app-start.ps1 -BotName 'Dragon' -MapName '(4)Fighting Spirit' -PrepareOnly
```

선택만 확인:

```powershell
.\scripts\smoke-app-start.ps1 -BotName 'Dragon' -MapName '(4)Fighting Spirit' -DryRun
```

## 실제 실행 smoke 기대값

성공 기준:

- 사람 StarCraft 프로세스가 시작된다.
- AI StarCraft 프로세스가 시작된다.
- 사람 클라이언트가 인게임에 진입한다.
- AI 클라이언트가 인게임에 진입한다.
- 타이머/APM 오버레이가 보인다.
- 화면 해상도/스크린 bounds가 smoke 전후로 바뀌지 않는다.
- smoke 종료 후 새로 띄운 로컬 StarCraft 프로세스는 정리된다.

대표 로그:

```text
playerState=InGame
aiState=InGame
inGame=True
aiInGame=True
timerOverlay=True
```

증거 스크린샷은 보통 아래에 저장된다.

```text
artifacts\screenshots\smoke-start-player-overlay.png
artifacts\screenshots\smoke-start-player-final.png
artifacts\screenshots\smoke-start-ai-final.png
```

## 래더/랜덤 호환성 회귀 검증

투혼 1.4 Remastered 래더맵, 플레이어 Protoss, 상대 Terran 조건은 아래처럼 직접 확인한다.

```powershell
.\scripts\smoke-app-start.ps1 -DryRun -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4 [Remastered Ladder]'
```

실제 시작까지 확인할 때는 검증된 봇을 명시한다.

```powershell
.\scripts\smoke-app-start.ps1 -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4 [Remastered Ladder]' -BotName 'Dragon'
```

known-bad 봇이 후보에서 빠졌는지는 강제 지정 dry-run이 실패하는지로 확인한다.

```powershell
.\scripts\smoke-app-start.ps1 -DryRun -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4 [Remastered Ladder]' -BotName 'LetaBot'
.\scripts\smoke-app-start.ps1 -DryRun -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4 [Remastered Ladder]' -BotName 'Stone'
```

`scripts\smoke.ps1`는 `dotnet run -- --smoke`의 종료 코드를 반드시 전파해야 한다. 런처 UI smoke가 실패했는데 스크립트가 0으로 끝나면 smoke 자체의 회귀로 본다.

## 전체 호환성 감사

런처가 쓰는 병합 카탈로그 기준으로 모든 선언된 DLL 봇-맵 조합을 감사한다.

```powershell
.\scripts\audit-compatibility.ps1
```

성공 기준:

- `issues=0`
- `compatible-pairs.csv`에 현재 허용 후보가 저장된다.
- `blocked-declared-pairs.csv`에 known-bad로 막힌 후보가 저장된다.
- `runtime-crashes.csv`에 로컬 AI 런타임 오류 로그에서 읽은 크래시 증거가 저장된다.

주의:

- 이 감사는 정적 파일/카탈로그/기존 런타임 로그 기반 전수검사다.
- 모든 허용 조합을 실제 StarCraft로 부팅하는 동적 전수검사는 별도 장시간 작업으로 수행한다.

## 봇-맵 매트릭스 검증

릴리즈 번들 기준의 허용 조합은 매트릭스 스크립트로 dry-run 전수검사를 먼저 수행한다.

```powershell
.\scripts\test-compatible-matrix.ps1 -ValidationMode DryRun
```

성공 기준:

- `compatibility-audit`가 `--bundled-catalog-only` 기준으로 `issues=0`, `runtimeCrashes=0`을 출력한다.
- Neo 계열 개발 봇은 기본 매트릭스에서 제외된다. 수동 확인이 필요할 때만 `-IncludeNeo`를 쓴다.
- 결과 CSV의 `exitCode`가 모두 `0`이어야 한다.
- 사용자 개인 Remastered ladder/user map 폴더의 맵이 섞이면 안 된다.

특정 조합을 실제 StarCraft로 확인할 때는 범위를 좁혀 Runtime 모드로 실행한다.

```powershell
.\scripts\test-compatible-matrix.ps1 -ValidationMode Runtime -BotName Stardust -MapName '(4)Fighting Spirit' -ObserveSeconds 90 -NoBuild
```

이미 카탈로그에서 차단된 조합을 재검증할 때는 일반 런처 경로가 아니라 smoke 전용 우회 플래그를 명시한다. 이 플래그는 검증용이며 사용자 실행 후보를 바꾸지 않는다.

```powershell
.\.dotnet\dotnet.exe .\src\Sparring.Client\bin\Release\net8.0-windows\Sparring.Client.dll --smoke-start --bundled-catalog-only --allow-incompatible --mode Sparring --bot ABCDxyz --map '(4)Andromeda' --observe-seconds 120 --require-ai-activity
```

Runtime 모드는 로컬 `C:\sparring\SC116AI`와 `C:\sparring\SC116AI_ai`만 대상으로 하며, 사용자가 별도로 실행한 StarCraft/Remastered 창을 정리 대상으로 삼지 않는다.

Runtime 모드는 기본적으로 AI 화면의 시작 시점과 관찰 후 스크린샷을 비교해 의미 있는 화면 변화가 있는지도 확인한다. 이 검사는 드롭되지 않았지만 일꾼이 멈춰 있는 조합을 찾기 위한 보조 신호다. 캡처 환경 문제를 분리해야 할 때만 `-SkipAiActivityCheck`를 사용한다.

## 프로세스 정리 원칙

정리 대상:

- `C:\sparring\SC116AI` 아래에서 실행된 `StarCraft.exe`
- `C:\sparring\SC116AI_ai` 아래에서 실행된 `StarCraft.exe`
- `C:\sparring` 아래 로컬 ChaosLauncher

정리 금지:

- 사용자가 별도로 실행한 Remastered StarCraft
- `C:\Program Files (x86)\StarCraft\...` 아래 프로세스
- SCHNAIL 원본 설치 프로세스/파일

프로세스가 헷갈리면 PID와 실행 경로를 먼저 확인한다.

```powershell
Get-CimInstance Win32_Process |
  Where-Object { $_.Name -eq 'StarCraft.exe' } |
  Select-Object ProcessId, ExecutablePath, CommandLine
```

## 자주 헷갈리는 현상

### 사람 화면의 `Failed to load the AI Module ""`

사람 `bwapi.ini`의 `ai` 값은 의도적으로 비어 있다. BWAPI는 이 빈 값을 빨간 에러 문구로 표시할 수 있다.

이 문구만 보고 AI 봇이 실패했다고 판단하면 안 된다. AI 런타임의 `bwapi.ini`와 실제 AI 화면/동작을 따로 확인한다.

금지:

- 사람 런타임에 선택 봇 DLL을 넣어서 경고를 없애기.
- CoachAI 또는 플레이어 조작용 AI DLL을 되살리기.

### 사용자 Remastered 창이 켜져 있는 경우

사용자 플레이용 Remastered 창은 테스트 대상이 아니다. 실제 smoke나 수동 검증 때는 로컬 1.16.1 인스턴스와 구분한다.

### APMAlert

APMAlert는 크래시 이력이 있어 기본 OFF다. 타이머/APM 기능을 고친다고 APMAlert를 기본 ON으로 바꾸지 않는다.

## 배포 작업

아래 작업은 사용자가 명시적으로 요청할 때만 한다.

- Git commit
- Git tag
- GitHub push
- GitHub Release
- installer/zip 생성 및 업로드

배포를 수행했다면 실제 URI 또는 GitHub Release 페이지를 확인해 설치 파일과 안내가 정상 반영됐는지 검증한다.
## 2026-06-08 smoke-start runtime checks

`.\scripts\smoke-app-start.ps1 -BotName 'Dragon' -MapName '(4)Fighting Spirit'` now validates these additional runtime conditions:

- Player startup trace is captured before the timer overlay:
  - `artifacts\screenshots\startup-trace\player-startup-trace.csv`
  - `artifacts\screenshots\startup-trace\player-*.png`
- The human client should not show the red BWAPI empty-AI error.
  - Human `bwapi.ini` must not contain a normal `ai = <bot dll>` value.
  - Human `bwapi.ini` should contain `tournament = bwapi-data\TM\TournamentModule.dll`.
  - AI `bwapi.ini` should contain the selected bot DLL and `tournament =`.
- The timer/APM overlay should start as soon as the in-game HUD is visible, even if startup chat text is still visible.
- AI shutdown should be graceful before termination:
  - `aiShutdownSent=True`
  - `aiProcessGoneAfterCleanup=True`
  - `aiGracefulShutdown=True`
  - `playerAfterAiShutdownState` should not be `BlockedDialog`.

If `traceMaxRed` is nonzero, inspect `traceMaxRedFrame`. Red pixels from the AI player name in a normal BWAPI startup chat line are acceptable; red `ERROR:` text is not.
