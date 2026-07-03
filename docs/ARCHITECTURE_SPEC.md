# Architecture Spec

## 솔루션 구조

```text
Sparring.sln
src/
  Sparring.Core/
  Sparring.Client/
tests/
  Sparring.Tests/
scripts/
docs/
docs/
```

## Core 프로젝트

`src/Sparring.Core`는 UI와 분리된 정책/모델/파일 작업을 담당한다.

주요 책임:

- 경로 정책: `PracticePaths`, `RuntimeWritePolicy`
- Sparring 내장 자산 카탈로그 로딩: `PracticeAssetCatalogReader`
- SCHNAIL 호환 JSON 파싱: `SchnailCatalogReader`
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

## 래더 매칭

- 래더 후보는 `LadderBotSelector`가 고른다.
- 먼저 봇-맵 호환성, DLL 봇 여부, 상대 종족 필터를 적용한다.
- 맵이 지정된 경우 해당 맵의 호환 봇 중 현재 사람 MMR과 봇 ELO/MMR이 가까운 봇을 거리 기반 가중 랜덤으로 선택한다.
- 맵이 `Random`인 경우 모든 활성 호환 맵을 대상으로 현재 사람 MMR에 가까운 봇을 먼저 가중 선택하고, 그 봇이 지원하는 맵 중 하나를 고른다.
- 따라서 낮은 MMR 봇이 완전히 배제되지는 않지만, 현재 MMR과 멀수록 매칭 확률은 급격히 낮아진다.

## 내장 자산

제품 실행 시 봇/맵/핫키 기본값은 레포/설치 폴더의 `data` 아래에서 읽는다.

```text
data/
  bots/
    bots.dat
    <BotName>/
  maps/
    maps.dat
    *.scm / *.scx / preview images
  res/
    sc_hotkeys.csv
    messages_kr.properties
    hotkey_data/
    hotkey_icons/
  tools/
    mpq/
```

`scripts\import-schnail-assets.ps1`는 릴리즈 준비 또는 개발 마이그레이션 시 로컬 SCHNAIL 설치본에서 위 구조로 자산을 복사한다. 이 스크립트는 원본 SCHNAIL 폴더를 수정하지 않으며, 최종 사용자 실행 경로는 SCHNAIL 설치 여부와 무관해야 한다.

## App 프로젝트

`src/Sparring.Client`는 WinForms 런처, 오버레이, 실제 실행 smoke를 담당한다.

주요 책임:

- 메인 UI: `MainForm`
- 사용자 설정 저장: `SparringSettings`
- 인게임 타이머/APM 오버레이: `PracticeOverlayForm`
- 글로벌 입력 카운트: `GlobalInputActionHook`
- StarCraft 창 조정: `StarCraftBorderlessWindow`, `StarCraftBorderlessKeeper`
- AI 창 최소화 유지: `StarCraftWindowMinimizeKeeper`
- 화면 상태 감지 WIP: `StarCraftScreenState`
- smoke entrypoint: `SmokeChecks`, `Program`

## 런타임 분리

SPArring은 두 개의 StarCraft 1.16.1 런타임을 사용한다.

```text
C:\sparring\SC116AI      사람 클라이언트
C:\sparring\SC116AI_ai   AI 클라이언트
```

역할별 핵심 차이:

| 항목 | 사람 런타임 | AI 런타임 |
| --- | --- | --- |
| `bwapi.ini` `ai` | 빈 값 | 선택 봇 DLL |
| 사운드 | ON | OFF |
| APMAlert | 기본 OFF | OFF |
| 화면 | borderless/fullscreen 지향 | 창모드/관찰 가능 지향 |
| 사용 목적 | 플레이어 조작 | 봇 실행 |

Sparring `data` 폴더는 런타임 자산의 읽기 전용 출처이며 실행 중 쓰기 대상이 아니다. SCHNAIL 설치본은 개발/릴리즈 준비 시 import 소스로만 사용할 수 있고, 제품 실행 필수 조건이 아니다.

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
- 일반 연습 세션의 AiOpponent는 맵을 비우고 `game = JOIN_FIRST`로 첫 방에 참가한다.
- 봇-vs-봇 smoke에서는 병렬 검증 중 잘못된 방을 고르지 않도록 오른쪽 클라이언트가 왼쪽 호스트의 캐릭터명 기반 방 이름을 지정한다.
- ChaosLauncher는 레지스트리/전역 플러그인 상태를 공유하므로 시작 구간을 전역 mutex로 직렬화한다. 이 직렬화는 시작 설정 충돌을 막기 위한 것이며, 같은 PC에서 Local PC LAN 경기를 여러 개 동시에 유지할 수 있다는 보장은 아니다.

## 런타임 자산 흐름

1. Sparring `data` 카탈로그에서 봇/맵 원본 경로를 읽는다.
2. 맵은 두 런타임의 `maps\Sparring`로 복사한다.
3. 봇은 AI 런타임의 `bwapi-data\AI\Sparring\Bots\<BotName>`로 복사한다.
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
- `Plugins\APMAlert.bwl` 또는 `.sparring-disabled`
- `patch_rt.mpq`

사용자 데이터:

- `%APPDATA%\Sparring\settings.json`
- `%APPDATA%\Sparring\history.json`
- `%APPDATA%\Sparring\ladder-rating.json`

## 핫키 적용 구조

1. Sparring 기본 CSV 또는 작업 CSV를 읽는다.
2. Battle.net/Remastered 가져오기는 `STR_* = key` 파일을 탐지해 Sparring command id로 매핑한 뒤 작업 CSV 엔트리의 핫키만 갱신한다.
3. `stat_txt.txt`의 명령 문자열을 패치한다.
4. `sctblcmp.exe`로 `stat_txt.tbl`을 컴파일한다.
5. Sparring에 포함된 SFmpq 기반 writer로 사람 런타임 `patch_rt.mpq`에 `rez\stat_txt.tbl`을 삽입한다.

금지:

- Sparring 내장 `data` 수정.
- 원본 Remastered/Battle.net 설치 폴더 수정.
- `JMpqEditor` 직접 쓰기 방식.
- AI 런타임에 사람 핫키 패치를 우선 적용하는 흐름.

UI:

- 핫키 탭은 Sparring 내장 `data\res\hotkey_icons` 이미지를 읽기 전용으로 로드한다.
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
- 봇 로그가 없으면 사람 런타임 TournamentModule의 `bwapi-data\gameState.txt`가 해당 세션 중 갱신됐는지 확인하고, `gameOver`, `victorious`, `defeated` 플래그로 승패를 판정한다.
- 봇 로그와 TournamentModule 결과가 모두 없는 래더 세션은 플레이어 이탈/프로세스 종료만으로 승패를 추정하지 않고 `미확인`으로 둔다.
- 스파링 세션의 같은 이탈은 래더 점수를 건드리지 않고 `중단`으로 기록한다.
- 사람 래더 점수만 ELO 공식으로 갱신한다. AI의 ELO/MMR은 카탈로그 값을 고정 상대 점수로 사용한다.
- Sparring 커스텀 래더 룰로 승리 시 점수 변화가 0으로 반올림되면 최소 +1점을 보장한다.
- 전적 탭은 시작 시각, 모드, 결과, AI, 맵, 종족, APM, 액션, 시간, 래더 점수, 판정 근거를 표시한다.

## 봇-맵 호환성 예외

- 기본 호환성은 Sparring 내장 `maps.dat` / `bots.dat` 선언과 Remastered 래더맵의 `EffectiveCompatibilityMapIds`를 따른다.
- `bots.dat`의 `mapGuids`는 봇별 허용 맵 whitelist로 해석한다. 목록에 없는 맵은 지원 맵으로 간주하지 않는다.
- 단, 실제 런타임 로그/관찰로 깨지는 조합은 `PracticeCatalogCompatibility`의 known-bad 예외로 막는다.
- 2026-06-07 확인된 예외:
  - `Feint` + `(4)Fighting Spirit` / `(4)Fighting Spirit 1.4 [Remastered Ladder]`: AI 런타임 `Steamhammer.dll` 접근 위반 크래시 확인.
  - `Crazyhammer` / `Randomhammer` / `Steamhammer` + `(4)Fighting Spirit` / `(4)Fighting Spirit 1.4 [Remastered Ladder]`: 같은 `Steamhammer.dll` 계열이므로 투혼 계열에서 안전 차단.
  - `ICELab` + `(4)Fighting Spirit` / `(4)Fighting Spirit 1.4 [Remastered Ladder]`: 사용자 실제 플레이에서 상대 정지 재현. 크래시는 없지만 로컬 런타임에서 실전 불가 조합으로 차단.
  - `CUBOT` + `(4)Fighting Spirit` / `(4)Fighting Spirit 1.4 [Remastered Ladder]`: 2026-06-10 릴리즈 후보 UI 검증 중 AI 런타임 `CUBOT.dll` 접근 위반 크래시 확인.
  - `Stone`: 2026-07-02 bot-vs-bot 창모드 재검증에서 `(2)Benzene`, `(2)Destination`, `(2)Heartbreak Ridge`, `(3)Neo Moon Glaive`, `(3)Tau Cross`, `(4)Circuit Breaker`, `(4)Electric Circuit`, `(4)Empire of the Sun`, `(4)Fighting Spirit`, `(4)Icarus`, `(4)La Mancha1.1`, `(4)Python`, `(4)Roadrunner`는 인게임/무오류를 통과했다. `(4)Andromeda`, `(4)Jade`는 `Stone` 쪽 접근 위반 크래시가 재현되어 차단한다.
  - `RedRum`: 로컬 카탈로그/봇 패키지/외부 봇 설명에서 안전하게 허용할 수 있는 지원 맵 목록을 확인하지 못했고, `(4)Fighting Spirit` 계열, `(4)Jade`, `(2)Benzene`, `(2)Destination`, `(2)Heartbreak Ridge`, `(3)Neo Moon Glaive`, `(3)Tau Cross`에서 `RedRum.dll` 접근 위반이 확인되어 검증된 whitelist가 생길 때까지 전체 후보에서 제외한다.
  - `Chris Coxe`, `Pineapple Cactus`, `Sijia Xu`, `Crona`, `BananaBrain`, `Locutus`, `ZNZZBot`, `DaQin` + `(4)Fighting Spirit` 계열: 2026-06-30 사용자 제보에서 중간 AI drop 또는 일꾼 정지 상태가 확인되어 해당 맵 계열에서 차단한다.
  - `ABCDxyz`, `BananaBrain`, `BetaStar`, `Brainiac` + `(4)Electric Circuit`: 2026-07-02 검증용 `--allow-incompatible` Runtime 재검증에서 AI가 게임룸 상태로 돌아가거나 정상 종료되지 않아 해당 맵 조합에서 차단한다.
  - `AILien` + `(3)Neo Moon Glaive`: 2026-07-02 검증용 `--allow-incompatible` Runtime 재검증에서 AI가 관찰 중 게임룸 상태로 돌아가고 정상 종료되지 않아 해당 맵 조합에서 차단한다.
  - `ABCDxyz` + `(4)Andromeda`, `Arrakhammer` + `(3)Neo Moon Glaive`, `BananaBrain` + `(3)Neo Moon Glaive` / `(4)Jade`, `BetaStar` + `(4)Andromeda` / `(3)Neo Moon Glaive`, `Brainiac` + `(2)Match Point` / `(3)Power Bond)` / `(4)Jade`: 2026-07-02 검증용 `--allow-incompatible` Runtime 재검증에서 인게임, AI 활동, 오류 없음, 정상 종료 조건을 통과하여 차단에서 해제한다.
  - `Crazyhammer` + `(3)Neo Moon Glaive`: 2026-07-01 Runtime 매트릭스 검증과 재시도에서 인게임 진입 후 AI 화면이 `BlockedDialog`로 전환되어 해당 맵 조합에서 차단한다.
  - `DaQin` + `(3)Neo Moon Glaive` / `(4)Andromeda` / `(4)Electric Circuit`: 2026-07-01 Runtime 매트릭스 검증과 독립 재시도에서 인게임 진입 후 AI 화면이 `BlockedDialog`로 전환되어 해당 맵 조합에서 차단한다.
  - `Feint` + `(2)Benzene` / `(2)Destination` / `(2)Heartbreak Ridge` / `(3)Neo Moon Glaive` / `(3)Tau Cross` / `(4)Andromeda` / `(4)Circuit Breaker` / `(4)Electric Circuit` / `(4)Empire of the Sun` / `(4)Icarus` / `(4)Jade` / `(4)La Mancha1.1` / `(4)Python` / `(4)Roadrunner`: 2026-07-01 Runtime 매트릭스와 대표 독립 재시도에서 AI 프로세스 소실, AI 활동 0, AI 런타임 오류 파일, 플레이어 측 `Drop Players` 화면이 반복되어 해당 맵 조합에서 차단한다.
  - `Iron bot`: 2026-07-02 bot-vs-bot 창모드 재검증에서 `(2)Benzene`, `(2)Destination`, `(2)Heartbreak Ridge`, `(3)Neo Moon Glaive`, `(3)Tau Cross`, `(4)Electric Circuit`, `(4)Empire of the Sun`, `(4)Fighting Spirit`, `(4)Icarus`, `(4)La Mancha1.1`, `(4)Python`, `(4)Roadrunner`는 인게임/무오류를 통과했다. `(4)Andromeda`, `(4)Circuit Breaker`, `(4)Jade`는 `Iron bot` 쪽 접근 위반 크래시가 재현되어 차단한다.
  - `LetaBot` + `(2)Benzene` / `(2)Destination` / `(2)Heartbreak Ridge` / `(2)Overwatch(n)` / `(2)Tres Pass` / `(3)Neo Moon Glaive` / `(3)Power Bond)` / `(3)Tau Cross` / `(4)Andromeda` / `(4)Circuit Breaker` / `(4)Electric Circuit` / `(4)Empire of the Sun` / `(4)Gladiator1.1` / `(4)Icarus` / `(4)Jade` / `(4)La Mancha1.1` / `(4)Python` / `(4)Roadrunner`: 2026-07-02 Runtime 매트릭스와 대표 독립 재시도에서 AI 런타임 `LetaBot.dll` 접근 위반 크래시가 반복되어 해당 맵 조합에서 차단한다.
  - `NeoProtossF`, `NeoTerranF`, `NeoZergF`: 개발 중인 Sparring 내장 연습 봇이므로 래더/랜덤 후보에는 넣지 않지만, 수동 스파링 노출은 유지한다. 제보된 Neo 계열 미숙 동작은 known-bad 맵 차단으로 처리하지 않는다.
  - `Yuanheng Zhu` + `(4)Andromeda`: 2026-06-10 릴리즈 후보 UI 검증 중 AI 런타임 `Juno.dll` 접근 위반 크래시 확인.

주의:

- 사람 `bwapi.ini`의 `ai`가 빈 값이면 BWAPI가 `Failed to load the AI Module ""` 경고를 표시할 수 있다.
- 이 경고는 AI 런타임의 봇 DLL 로드 실패와 구분해야 한다.
