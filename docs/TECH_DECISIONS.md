# Technical Decision Log

Last updated: 2026-06-05

이 문서는 기능별 구현 옵션을 먼저 비교하고, 선택 이유와 검증 방법을 남기기 위한 기록이다.

## 결정 원칙

- 특정 구현 방식을 미리 고정하지 않는다.
- SCHNAIL이 이미 해결한 방식이 있으면 먼저 관찰/추정/검증한다.
- 원본 SCHNAIL/Remastered 설치 폴더는 수정하지 않는다.
- 실패 가능성이 큰 방식은 작은 smoke로 먼저 검증한다.
- 결정이 바뀌면 이 문서에 이유를 남긴다.

## 런처 기반

상태: 부분 결정

| 옵션 | 장점 | 단점/리스크 | 현재 판단 |
| --- | --- | --- | --- |
| ChaosLauncher + BWAPI/W-MODE | 현재 로컬 1.16.1/BWAPI 런타임에 이미 존재, 멀티 인스턴스 실행 경험 있음 | 레지스트리/플러그인 전역 상태를 건드리므로 순서 제어가 필요 | 초기 실행 경로 후보 |
| SCHNAIL 클라이언트 직접 제어/개조 | SCHNAIL 기능을 가장 많이 재사용 가능 | 원본 설치 수정 금지, 로그인/업데이트/원격 서비스와 엮임 | 읽기 전용 분석 대상으로만 시작 |
| 자체 StarCraft 실행 + DLL 인젝션 | 제어권이 큼 | BWAPI 주입/멀티 인스턴스/호환성 구현 부담 큼 | 후순위 |

다음 검증: ChaosLauncher 없이 SCHNAIL이 어떤 방식으로 핫키/타이머/실행을 처리하는지 로컬 파일과 실행 상태를 확인한다.

## 런처 UI

상태: 결정

| 옵션 | 장점 | 단점/리스크 | 현재 판단 |
| --- | --- | --- | --- |
| SCHNAIL식 어두운 단일 화면 | 원터치 스파링에 익숙함, 봇/맵 선택 흐름이 빠름 | 정보가 많아지면 복잡해질 수 있음 | 기본 레이아웃으로 채택 |
| 일반 WinForms 설정 폼 | 구현이 빠르고 안정적 | 스파링 도구 느낌이 약하고 클릭 수가 늘어남 | 초기 골격에서 제외 |
| 마법사형 단계 UI | 초보자에게 친절 | 반복 연습에 느림 | 설정/첫 실행 안내에만 고려 |

선택: SCHNAIL의 상단 탭, 봇 리스트, 맵 리스트, 큰 시작 버튼 구조를 참고하되 광고/로그인 영역은 제거하고 난이도/런타임 안전 상태/설정 탭을 추가한다.

## 핫키 커스터마이징

상태: 결정

| 옵션 | 장점 | 단점/리스크 | 확인할 것 |
| --- | --- | --- | --- |
| Remastered/SCHNAIL 핫키 파일 가져오기 후 런타임에 적용 | 사용자 기존 설정을 가져오기 쉬움 | 1.16.1에서 실제 적용 방식이 MPQ/string patch인지 확인 필요 | `remastered_hotkeys.txt`, `sc_hotkeys.csv`, `patch_rt.mpq` 관계 |
| ChaosLauncher 플러그인 또는 별도 패처 사용 | 1.16.1에 적용 가능성이 높음 | 플러그인 의존/크래시 리스크 | SCHNAIL bundled plugin 조사 |
| 런처 내부 CSV 편집 + 런타임 패치 파일 생성 | UI 제어권과 테스트 가능성 좋음 | MPQ writer 선택을 잘못하면 파일 손상 가능 | 채택 |

결정 기준: 원본 수정 없이 `C:\starai\SC116AI`와 `C:\starai\SC116AI_ai`에만 반영 가능하고, 게임 실행 후 실제 단축키가 바뀌는 방식.

현재 선택: SCHNAIL `sc_hotkeys.csv`를 편집하고 `stat_txt.txt`를 패치한 뒤 `sctblcmp.exe`로 `stat_txt.tbl`을 만든다. 실제 삽입은 SCHNAIL 실행 파일 안의 `org.jasperge.mpq.MPQEditor`/`SFmpq` 기반 `addFile`을 사용한다. `JMpqEditor` 직접 쓰기는 listfile 누락 시 MPQ를 줄여버리는 손상 위험이 확인되어 금지한다. 적용 대상은 작업용 사람 런타임 `C:\starai\SC116AI\patch_rt.mpq`뿐이다.

검증: 임시 복사본 MPQ에 `rez\stat_txt.tbl`을 삽입한 뒤 `rez\minimappreview.bin`과 삽입된 TBL을 읽기 전용으로 확인하는 테스트를 둔다.

## 타이머/APM 표시

상태: 결정

| 옵션 | 장점 | 단점/리스크 | 확인할 것 |
| --- | --- | --- | --- |
| BWAPI/플러그인으로 인게임 텍스트 표시 | 진짜 인게임 표시 가능 | 플레이어 클라에 BWAPI/플러그인을 켜야 할 수 있음, 안정성 리스크 | 읽기 전용/관찰 전용 플러그인 가능 여부 |
| APMAlert 재사용 | 이미 1.16.1용 기능이 존재 | 이전 크래시 로그에 잡힘, 기본 OFF 원칙과 충돌 | 안전 모드/대체 플러그인 가능 여부 |
| 외부 투명 오버레이 | StarCraft 내부를 덜 건드림 | 오버레이 위치/캡처/전체창모드 호환성 필요 | 채택 |
| 런처/사이드카 기록만 제공 | 안전함 | 사용자가 원하는 인게임 타이머와 다름 | 임시 단계로만 적합 |

결정 기준: 플레이어 유닛 제어 없이, 전체 창모드에서 안정적으로 보이고, 크래시 로그가 없는 방식.

현재 선택: 사람 클라이언트 화면 위에 클릭 투과형 topmost 오버레이를 띄워 `MM:SS APM N`을 표시한다. APM은 StarCraft 포그라운드 상태에서 키다운/마우스다운을 카운트한다. APMAlert는 크래시 이력 때문에 기본 OFF를 유지한다.

## 봇 난이도 / 한국 래더 환산

상태: 결정

| 옵션 | 장점 | 단점/리스크 | 확인할 것 |
| --- | --- | --- | --- |
| SCHNAIL ELO를 그대로 표시 | 즉시 가능, 원본 데이터와 일치 | 한국 서버 래더 체감과 다를 수 있음 | 봇풀 ELO 분포 |
| SCHNAIL ELO -> StarCraft Remastered 래더 MMR/등급 추정표 | 사용자가 체감하기 쉬움 | 공식 환산이 아닐 가능성, 서버/시즌별 차이 | 등급 컷과 근거 출처 |
| 사용자의 실제 전적 기반 보정 | 개인 맞춤 난이도 가능 | 데이터가 쌓이기 전 부정확 | 매치 히스토리 설계 |
| 빌드별 난이도 별도 표기 | 같은 봇 안에서도 체감 난이도 구분 가능 | 빌드 선택이 가능한 봇만 지원 | 봇별 설정 파일 구조 |

결정 기준: UI에는 원본 수치와 추정 수치를 구분해 표시한다.

현재 선택: SCHNAIL ELO를 원본 난이도로 표시하고, 내부 기준표로 SCR 한국 서버 래더 MMR/등급 참고값을 함께 표기한다. 공식 환산이 아니므로 UI 상세 설명에 주의 문구를 함께 보여준다.

## 전체화면/마우스

상태: 부분 결정

| 옵션 | 장점 | 단점/리스크 | 현재 판단 |
| --- | --- | --- | --- |
| W-MODE 창모드 + Win32 borderless 적용 | 해상도 강제 변경 회피, Windows 11/QHD에 적합 | 창 스타일 조정과 좌표 검증 필요 | 1순위 |
| StarCraft 독점 전체화면 | 원래 게임 방식 | 해상도 변경/다른 창 재배치/마우스 감도 문제 | 금지 |
| DX wrapper/외부 렌더러 | 마우스/스케일 제어 가능성 | 추가 의존성과 호환성 부담 | 조사 후순위 |

검증 기준: 실행 중 디스플레이 해상도 변경 없음, 다른 창 위치 변화 없음, `ClipCursor` OFF일 때 커서 가두기 없음.

## BWAPI / W-MODE 설정 파일

상태: 결정

| 옵션 | 장점 | 단점/리스크 | 현재 판단 |
| --- | --- | --- | --- |
| 역할별 `bwapi.ini`/`wmode.ini` 직접 생성 | 테스트 가능, 사람/AI 분리 불변조건을 명확히 보장 | INI 키 호환성을 유지해야 함 | 채택 |
| SCHNAIL 설정 파일 그대로 복사 | 빠름 | 사람 쪽에 봇 AI가 섞일 수 있음 | 불가 |
| ChaosLauncher UI 설정에 맡김 | 수동 확인 가능 | 자동화/회귀 테스트가 어려움 | 보조 수단 |

선택: 사람 런타임과 AI 런타임의 INI를 각각 생성한다. 사람 `ai`는 빈 값, AI `ai`는 준비된 봇 모듈 경로를 쓴다. 두 클라이언트 모두 W-MODE 창모드이며 AI는 `ClipCursor=0`, sound OFF, `MuteNotFocused=1`이다.

## 런타임 자산 복사

상태: 결정

| 옵션 | 장점 | 단점/리스크 | 현재 판단 |
| --- | --- | --- | --- |
| StarAI 내장 `data`에서 실행 시 런타임으로 복사 | 최종 사용자 SCHNAIL 의존 제거, 재현 가능한 카탈로그 | 릴리즈 ZIP 용량 증가, import 관리 필요 | 채택 |
| SCHNAIL 원본 bots/maps에서 실행 시 런타임으로 복사 | 원본 보호, 최신 SCHNAIL 카탈로그 활용 | 제품 런타임이 SCHNAIL 설치에 의존 | 폐기 |
| SCHNAIL 원본 경로를 직접 참조 | 복사 없음 | 원본 수정/잠금/업데이트 충돌 위험 | 금지 |
| 봇/맵을 레포/릴리즈에 vendoring | 재현성, 독립 설치 가능 | 용량/라이선스/업데이트 부담 | 조건부 채택 |

선택: 맵은 양쪽 런타임 `maps\StarAI`, 봇은 AI 런타임 `bwapi-data\AI\StarAI\Bots\<봇명>`에 복사한다. DLL 봇은 복사된 DLL 상대 경로를 AI `bwapi.ini`의 `ai` 값으로 쓴다.

2026-06-08 업데이트: 사용자가 SCHNAIL은 개발 참고 대상일 뿐 제품 의존성이 되면 안 된다고 명확히 했다. 따라서 `scripts\import-schnail-assets.ps1`로 릴리즈 준비 시 필요한 봇/맵/핫키 자산을 StarAI `data`에 import/include하고, 앱은 런타임에 `C:\Program Files (x86)\SCHNAIL Client`를 읽지 않는다.

## 봇-맵 런타임 예외

상태: 결정

선택: SCHNAIL 카탈로그가 호환으로 선언하더라도, 실제 1.16.1/BWAPI 로컬 런타임에서 크래시나 장시간 정지가 확인된 조합은 `PracticeCatalogCompatibility`에서 known-bad 조합으로 차단한다.

현재 예외:

- `Feint` + Fighting Spirit 계열: AI 런타임 `Steamhammer.dll` 접근 위반 크래시 로그 확인.
- `Crazyhammer` / `Randomhammer` / `Steamhammer` + Fighting Spirit 계열: `Steamhammer.dll` 공유 계열이므로 같은 맵 패밀리에서 안전 차단한다.
- `ICELab` + Fighting Spirit 계열: 실제 플레이에서 상대 정지 관찰. 원인 확정 전까지 실전 매칭에서 제외한다.
- `RedRum`: Jade와 Fighting Spirit 계열에서 AI 런타임 `RedRum.dll` 접근 위반 크래시 로그 확인. 현재 표본이 제한적이므로 안전 맵을 단정하지 않고 전체 후보에서 임시 제외한다.

검증 기준: 차단 조합은 특정 봇을 고르면 맵 목록에서 사라지고, 특정 맵을 고르면 봇 목록/래더 후보에서 사라져야 한다.

## 사용자 맵 / 리플레이 / 전적

상태: 결정

| 옵션 | 장점 | 단점/리스크 | 현재 판단 |
| --- | --- | --- | --- |
| SCHNAIL 맵 폴더에 사용자 맵 복사 | SCHNAIL과 동일 경로 | 원본 수정 금지 위반 | 금지 |
| 별도 사용자 맵 폴더 읽기 | 원본 보호, ASL/빠른무한 등 추가 가능 | 봇별 맵 제약을 알 수 없음 | 채택 |
| 게임 결과 자동 판정 | 래더 점수 자동화 가능 | BWAPI/replay 파싱 추가 필요 | 후속 |
| 세션/시간/APM 기록 | 즉시 안정적으로 가능 | 승패는 아직 없음 | 채택 |

현재 선택: 설정 탭에서 사용자 맵 폴더와 리플레이 루트를 저장한다. 사용자 맵은 `.scm/.scx`만 읽고 런타임 `maps\StarAI`에 복사한다. 전적 탭은 실행 세션, 봇/맵/종족, 경과 시간, 마지막 APM/액션 수를 `%APPDATA%\StarAI.PracticeClient\history.json`에 기록한다.

## 보류 항목

- 봇 빌드 선택: 봇마다 설정 파일과 지원 방식이 달라 공통 UI를 바로 적용하기 어렵다. 특정 봇별 설정 파일 구조를 확인한 뒤 지원한다.
- Remastered 직접 실행: BWAPI 기반 1.16.1 스파링과 기술 기반이 다르므로 현재 필수 목표 범위 밖으로 둔다.
## 2026-06-08 Player BWAPI Startup Error

Status: decided and verified.

Observed problem:

- Early smoke trace captured the human client showing:
  - `ERROR: Could not find ai under ai in "C:\starai\SC116AI\bwapi-data\bwapi.ini".`
  - `ERROR: Failed to load the AI Module ""`.
- The red error text also made the screen analyzer classify the first in-game frames as `GameRoom`, delaying the timer/APM overlay.

Decision:

- Keep the human `[ai] ai` value absent/empty. Do not put CoachAI, a selected bot DLL, or a no-op AI DLL into the human runtime.
- Enable SCHNAIL-style player tournament mode instead:
  - human `bwapi.ini`: `tournament = bwapi-data\TM\TournamentModule.dll`
  - AI `bwapi.ini`: selected bot DLL only, with `tournament =`
- When starting the human client, inherit TournamentModule environment variables that disable SCHNAIL/TM drawing overlays:
  - `TM_DISABLE_DRAW_GAME_TIMER=true`
  - `TM_DRAW_TOURNAMENT_INFO=false`
  - `TM_DRAW_UNIT_INFO=false`
  - `TM_DRAW_BOT_NAMES=false`
  - `TM_STATE_FILE=bwapi-data\gameState.txt`

Why:

- SCHNAIL launches the player runtime through BWAPI plus TournamentModule instead of loading a normal AI module for the human player.
- This preserves `auto_menu` room automation without loading a human AI module.
- Actual smoke after the change showed no red `ERROR` chat message; only the AI player's normal BWAPI startup line may appear briefly.

Verification evidence:

- `.\scripts\smoke-app-start.ps1 -BotName 'Dragon' -MapName '(4)Fighting Spirit'`
  - `inGame=True`
  - `aiInGame=True`
  - `timerOverlay=True`
  - `traceMaxRed=0` in the clean run, or non-error red only from the AI player name in earlier trace.
  - Screenshot trace: `artifacts\screenshots\startup-trace\...`

## 2026-06-08 AI Shutdown After Leave

Status: decided and verified.

Decision:

- On session finalization, the AI client should receive the in-game leave sequence (`F10`, `Q`, `Q`) before termination.
- Do not depend on foreground focus for this. The controller posts key messages directly to the captured AI Brood War window handle.
- Because screen capture of a covered AI window can report the player screen, the shutdown path should not block on a fresh covered-window state check.
- After the leave sequence, normal cleanup may still terminate the AI StarCraft process. This is acceptable because the AI has already left the match, avoiding the player-side disconnect wait.

Verification evidence:

- `.\scripts\smoke-app-start.ps1 -BotName 'Dragon' -MapName '(4)Fighting Spirit'`
  - `aiShutdownSent=True`
  - `aiProcessGoneAfterCleanup=True`
  - `playerAfterAiShutdownState=GameRoom`
  - `aiGracefulShutdown=True`

## 2026-06-08 Ladder/Random Compatibility Regression

Status: decided and verified.

Observed evidence before editing:

- AI runtime crash log `C:\starai\SC116AI_ai\Errors\2026 Jun 08.txt` contained access violations for:
  - `Stone.dll` on `(4)Jade.scx`
  - `LetaBot.dll` on `(4)Fighting_Spirit 1.4.scx`
- The launcher and smoke-start path did not verify the Remastered ladder-map catalog, player race, enemy race, or ladder/random selection path.
- `scripts\smoke.ps1` did not propagate a non-zero exit code from `dotnet run -- --smoke`, so launcher UI smoke failures could be missed.

Decision:

- Treat the observed runtime-broken pairs as known-bad compatibility exclusions:
  - `LetaBot` + Fighting Spirit variants
  - `Stone` + Fighting Spirit variants
  - `Stone` + Jade variants
  - Existing `ICELab` and `Feint` Fighting Spirit exclusions remain.
- Keep the exclusions in `PracticeCatalogCompatibility` so bot lists, map lists, ladder candidates, and random pair generation share the same source of truth.
- Mirror selected bot config sidecar files such as `Gems_config.json` into `bwapi-data\AI` in the AI runtime, because some bots look for legacy root-side config paths even when the DLL is loaded from `bwapi-data\AI\StarAI\Bots\<BotName>`.
- `scripts\smoke.ps1` must fail when launcher smoke fails.
- `smoke-app-start.ps1` supports `-Mode`, `-PlayerRace`, and `-EnemyRace` so regressions like Protoss player vs Terran ladder on Remastered Fighting Spirit 1.4 can be checked directly.

Verification evidence:

- `dotnet test .\StarAI.PracticeClient.sln -v:minimal`: 99 passed.
- `.\scripts\smoke.ps1`: warning 0 / error 0.
- `.\scripts\smoke-app-start.ps1 -DryRun -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4 [Remastered Ladder]'`: selected a compatible Terran bot.
- `.\scripts\smoke-app-start.ps1 -DryRun -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4 [Remastered Ladder]' -BotName 'LetaBot'`: failed because the bot was no longer in candidates.
- `.\scripts\smoke-app-start.ps1 -DryRun -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4 [Remastered Ladder]' -BotName 'Stone'`: failed because the bot was no longer in candidates.
- `.\scripts\smoke-app-start.ps1 -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4 [Remastered Ladder]' -BotName 'Dragon'`: passed with `inGame=True`, `aiInGame=True`, `timerOverlay=True`, `aiGracefulShutdown=True`.
- `.\scripts\audit-compatibility.ps1`: passed after the audit was added, with:
  - `declaredDllPairs=1050`
  - `compatibleDllPairs=1041`
  - `blockedDeclaredDllPairs=9`
  - `issues=0`

## 2026-06-08 Map Preview Smoke Guard

Status: decided and verified.

Observed problem:

- The launcher map preview panel had been removed while the details panel grew wider.
- Existing smoke saved screenshots but did not assert that the preview control and image were present.

Decision:

- Restore the map preview panel on the Game tab.
- For Remastered ladder maps without their own preview image, reuse the linked SCHNAIL compatibility map preview when available.
- Launcher smoke now selects a concrete map and fails if the map preview `PictureBox` or image is missing.

Verification evidence:

- `.\scripts\smoke.ps1` first failed with `smoke: map preview box/image was not visible in the launcher.`
- After restoring the preview panel, `.\scripts\smoke.ps1` passed and `artifacts\screenshots\starai-launcher-smoke.png` showed the selected map preview.

## 2026-06-08 Compatibility Audit

Status: decided and verified.

Decision:

- Do not rely only on user screenshots or one-off smoke failures for compatibility filtering.
- Add a full catalog audit command that checks every declared DLL bot-map pair from the same merged catalog the launcher uses.
- The audit writes CSV artifacts under `artifacts\compatibility-audit`:
  - `compatible-pairs.csv`
  - `blocked-declared-pairs.csv`
  - `issues.csv`
  - `runtime-crashes.csv`
- The audit fails if a currently compatible pair has missing bot/map files or runtime crash evidence.
- Runtime crash evidence based only on a shared DLL module name is promoted for every still-compatible candidate using that DLL when the bot directory cannot be identified, so shared-DLL bot families are handled conservatively.

Latest result:

- `bots=86`
- `dllBots=61`
- `maps=31`
- `declaredDllPairs=1050`
- `compatibleDllPairs=1003`
- `blockedDeclaredDllPairs=47`
- `issues=0`
- `runtimeCrashes=14`

Limit:

- This is an exhaustive static/log audit, not an exhaustive in-game boot test for all 1024 currently compatible pairs. A full dynamic boot matrix would take many hours and should be run as a separate overnight-style validation job if needed.
  - no local StarCraft/ChaosLauncher processes remained after smoke cleanup.

## 2026-06-10 Alt+F4 Player Exit Handling

Status: decided and verified.

Observed evidence before editing:

- The issue was reproduced through the actual launcher UI flow, not inferred only from code.
- A stale reproduction showed `Brood War Instance 2: Windows - application error` after sending Alt+F4 to the player window.
- The fresh AI runtime error log also revealed `RedRum.dll` crashing on `(4)Jade.scx`, which is a compatibility problem independent of the Alt+F4 exit path.
- A later release-candidate UI verification selected `Stone.dll` on `(2)Benzene.scx` and reproduced another AI-side access violation.
- Another release-candidate UI verification selected `CUBOT.dll` on `(4)Fighting Spirit.scx` and reproduced an AI-side access violation.
- Another release-candidate UI verification selected `Yuanheng Zhu` / `Juno.dll` on `(4)Andromeda.scx` and reproduced an AI-side access violation.

Decision:

- Intercept Alt+F4 only when the foreground process is the captured player StarCraft PID.
- Do not pass that Alt+F4 directly to StarCraft while a StarAI session is active.
- Convert it into the same safe game-leave sequence used elsewhere: `F10`, `Q`, `Q`.
- After the player leaves, close the player process and then run the existing AI graceful shutdown/finalization path.
- Exclude `RedRum` from the compatible bot pool entirely until runtime safety is proven, because current evidence covers multiple map families and untested maps cannot be assumed safe.
- Block all `Steamhammer.dll` family bots on Fighting Spirit variants, because shared-DLL crash evidence on that map family should not leave sibling bot candidates selectable.
- Exclude `Stone` from the compatible bot pool entirely until runtime safety is proven, because it now has crash evidence across Fighting Spirit, Jade, and Benzene.
- Block `CUBOT` on Fighting Spirit variants because the crash evidence maps to that map family.
- Block `Yuanheng Zhu` on Andromeda variants because the crash evidence maps to that map family.
- Ensure random/sparring candidate filters remove bots with no currently compatible maps and final launch resolution rechecks explicit bot-map compatibility.

Verification evidence:

- `dotnet test .\StarAI.PracticeClient.sln -v:minimal`: 133 passed.
- `.\scripts\smoke.ps1`: warning 0 / error 0.
- `.\scripts\audit-compatibility.ps1`: `compatibleDllPairs=1003`, `blockedDeclaredDllPairs=47`, `issues=0`, `runtimeCrashes=14`.
- Alt+F4 interception is covered by `GlobalInputActionHookTests`; foreground Alt+F4 UI automation was stopped after user safety feedback.
