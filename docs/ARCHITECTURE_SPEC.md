# Architecture Spec

## 솔루션 구조

```text
StarAI.PracticeClient.sln
src/
  StarAI.PracticeClient.Core/
  StarAI.PracticeClient.App/
tests/
  StarAI.PracticeClient.Tests/
scripts/
docs/
docs/
```

## Core 프로젝트

`src/StarAI.PracticeClient.Core`는 UI와 분리된 정책/모델/파일 작업을 담당한다.

주요 책임:

- 경로 정책: `PracticePaths`, `RuntimeWritePolicy`
- SCHNAIL 카탈로그 파싱: `SchnailCatalogReader`
- 봇/맵 모델: `Models`
- 봇-맵 호환성: `PracticeCatalogCompatibility`
- 실행 계획 생성: `PracticeLaunchPlanBuilder`
- 런타임 자산 준비: `RuntimeProvisioner`
- INI/W-MODE/cnc-ddraw/플러그인 설정: `RuntimeConfigurators`
- ChaosLauncher 실행: `ChaosLauncher`, `PracticeSessionLauncher`
- 핫키 CSV/Remastered import/MPQ 적용: `Hotkeys`
- 사용자 맵: `UserMaps`
- Remastered 래더맵 탐지: `RemasteredLadderMapCatalogReader`
- 난이도 추정: `LadderDifficultyEstimator`
- 래더 후보 선택: `LadderBotSelector`
- 세션 기록/APM/결과/MMR 계산: `SessionHistory`, `SessionMetrics`, `SessionResults`

Core는 가능한 한 테스트 가능해야 하며, WinForms UI에 의존하지 않는다.

## App 프로젝트

`src/StarAI.PracticeClient.App`는 WinForms 런처, 오버레이, 실제 실행 smoke를 담당한다.

주요 책임:

- 메인 UI: `MainForm`
- 사용자 설정 저장: `PracticeClientSettings`
- 인게임 타이머/APM 오버레이: `PracticeOverlayForm`
- 글로벌 입력 카운트: `GlobalInputActionHook`
- StarCraft 창 조정: `StarCraftBorderlessWindow`, `StarCraftBorderlessKeeper`
- AI 창 최소화 유지: `StarCraftWindowMinimizeKeeper`
- 화면 상태 감지 WIP: `StarCraftScreenState`
- smoke entrypoint: `SmokeChecks`, `Program`

## 런타임 분리

SPArring은 두 개의 StarCraft 1.16.1 런타임을 사용한다.

```text
C:\starai\SC116AI      사람 클라이언트
C:\starai\SC116AI_ai   AI 클라이언트
```

역할별 핵심 차이:

| 항목 | 사람 런타임 | AI 런타임 |
| --- | --- | --- |
| `bwapi.ini` `ai` | 빈 값 | 선택 봇 DLL |
| 사운드 | ON | OFF |
| APMAlert | 기본 OFF | OFF |
| 화면 | borderless/fullscreen 지향 | 창모드/관찰 가능 지향 |
| 사용 목적 | 플레이어 조작 | 봇 실행 |

원본 SCHNAIL/Remastered 폴더는 런타임 자산의 읽기 전용 출처이며 실행 중 쓰기 대상이 아니다.

## 실행 계획 생성

`PracticeLaunchPlanBuilder`는 사용자의 선택을 두 클라이언트 설정으로 변환한다.

입력:

- 봇 ID
- 맵 ID
- 플레이어 종족
- 게임 이름
- borderless/fullscreen 여부
- APMAlert 허용 여부

출력:

- `ClientLaunchSettings Player`
- `ClientLaunchSettings Ai`
- 선택 봇/맵 메타데이터

중요 규칙:

- PlayerHost `AiModule`은 항상 빈 문자열이어야 한다.
- AiOpponent `AiModule`은 선택 봇이 BWAPI DLL 방식일 때 복사된 상대 경로를 사용한다.
- PlayerHost는 맵 파일을 지정해 방을 만든다.
- AiOpponent는 맵을 비우고 `game = JOIN_FIRST`로 첫 방에 참가한다.

## 런타임 자산 흐름

1. SCHNAIL 카탈로그에서 봇/맵 원본 경로를 읽는다.
2. 맵은 두 런타임의 `maps\StarAI`로 복사한다.
3. 봇은 AI 런타임의 `bwapi-data\AI\StarAI\Bots\<BotName>`로 복사한다.
4. 사람/AI 각각 `bwapi.ini`, `wmode.ini`, `ddraw.ini` 등을 생성/갱신한다.
5. ChaosLauncher로 사람 클라이언트를 실행한다.
6. 짧은 지연 후 ChaosLauncher로 AI 클라이언트를 실행한다.
7. 사람 창은 borderless/fullscreen, AI 창은 창모드/음소거 방향으로 유지한다.

## 설정 파일

주요 런타임 파일:

- `bwapi-data\bwapi.ini`
- `wmode.ini`
- `ddraw.ini`
- `Plugins\wmode.bwl`
- `Plugins\APMAlert.bwl` 또는 `.starai-disabled`
- `patch_rt.mpq`

사용자 데이터:

- `%APPDATA%\StarAI.PracticeClient\settings.json`
- `%APPDATA%\StarAI.PracticeClient\history.json`
- `%APPDATA%\StarAI.PracticeClient\ladder-rating.json`

## 핫키 적용 구조

1. SCHNAIL 또는 작업 CSV를 읽는다.
2. Battle.net/Remastered 가져오기는 `STR_* = key` 파일을 탐지해 SCHNAIL command id로 매핑한 뒤 작업 CSV 엔트리의 핫키만 갱신한다.
3. `stat_txt.txt`의 명령 문자열을 패치한다.
4. `sctblcmp.exe`로 `stat_txt.tbl`을 컴파일한다.
5. SCHNAIL과 같은 SFmpq 기반 writer로 사람 런타임 `patch_rt.mpq`에 `rez\stat_txt.tbl`을 삽입한다.

금지:

- 원본 SCHNAIL MPQ 수정.
- 원본 Remastered/Battle.net 설치 폴더 수정.
- `JMpqEditor` 직접 쓰기 방식.
- AI 런타임에 사람 핫키 패치를 우선 적용하는 흐름.

UI:

- 핫키 탭은 SCHNAIL 원본 `res\hotkey_icons` 이미지를 읽기 전용으로 로드한다.
- 선택 항목 목록은 대표 아이콘을 표시하고, 선택된 항목의 명령은 StarCraft 명령 카드에 맞춘 3x3 슬롯으로 표시한다.
- CSV/MPQ 쓰기 대상은 계속 사람 런타임뿐이다.

## 화면/오버레이 구조

현재 방향:

- 독점 전체화면 대신 cnc-ddraw borderless/fullscreen.
- 외부 topmost 투명 오버레이로 `MM:SS APM N` 표시.
- APM은 StarCraft가 포그라운드일 때 키다운/마우스다운을 카운트한다.
- 인게임 HUD 감지 후 오버레이와 APM 기록을 시작한다.
- 게임 종료/방 복귀/로컬 프로세스 종료를 감지하면 오버레이와 입력 hook을 정리하고, 캡처한 로컬 사람/AI 런타임 프로세스만 종료한다.
- 게임 시작 후 양쪽 런타임의 auto_menu를 OFF로 돌려 인게임 나가기 뒤 자동으로 원래 방/흐름으로 복귀하는 동작을 막는다.

## 결과와 래더 점수

- 결과 판정은 AI 봇 로그(`.txt`, `.log`, `.json`)에서 `WIN/LOSS/DRAW`, `is_winner`, `result` 패턴을 찾는 것을 우선한다.
- 봇 로그가 없는 래더 세션은 플레이어 이탈/프로세스 종료만으로 승패를 추정하지 않고 `미확인`으로 둔다.
- 스파링 세션의 같은 이탈은 래더 점수를 건드리지 않고 `중단`으로 기록한다.
- 사람 래더 점수만 ELO 공식으로 갱신한다. AI의 ELO/MMR은 카탈로그 값을 고정 상대 점수로 사용한다.
- 전적 탭은 시작 시각, 모드, 결과, AI, 맵, 종족, APM, 액션, 시간, 래더 점수, 판정 근거를 표시한다.

## 봇-맵 호환성 예외

- 기본 호환성은 SCHNAIL `maps.dat` / `bots.dat` 선언과 Remastered 래더맵의 `EffectiveCompatibilityMapIds`를 따른다.
- 단, 실제 런타임 로그/관찰로 깨지는 조합은 `PracticeCatalogCompatibility`의 known-bad 예외로 막는다.
- 2026-06-07 확인된 예외:
  - `Feint` + `(4)Fighting Spirit` / `(4)Fighting Spirit 1.4 [Remastered Ladder]`: AI 런타임 `Steamhammer.dll` 접근 위반 크래시 확인.
  - `ICELab` + `(4)Fighting Spirit` / `(4)Fighting Spirit 1.4 [Remastered Ladder]`: 사용자 실제 플레이에서 상대 정지 재현. 크래시는 없지만 로컬 런타임에서 실전 불가 조합으로 차단.

주의:

- 사람 `bwapi.ini`의 `ai`가 빈 값이면 BWAPI가 `Failed to load the AI Module ""` 경고를 표시할 수 있다.
- 이 경고는 AI 런타임의 봇 DLL 로드 실패와 구분해야 한다.
