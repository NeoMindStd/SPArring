# AIStarClient 작업 규칙

사용자가 명시적으로 요청하지 않으면 GitHub release, tag, push, installer 업로드, 배포 파일 생성을 하지 않습니다.

## 반드시 지킬 기준

1. 기본 답변과 작업 보고는 한국어 존댓말로 합니다.
2. 빌드가 가능한 변경은 `dotnet test`와 `scripts\smoke.ps1`를 통과시킨 뒤 완료로 보고합니다.
3. StarCraft/ChaosLauncher 실제 실행 흐름을 건드렸으면 가능할 때 `scripts\smoke-chaos-autostart.ps1` 또는 `scripts\smoke-app-start.ps1`까지 확인합니다.
4. 사용자는 `C:\starai\Start-StarAI-PracticeClient.cmd`를 작업표시줄에서 실행합니다. 이 경로와 동작을 깨뜨리면 안 됩니다.
5. 배포 산출물은 사용자가 요청한 때에만 만들고, 임시 publish/smoke 산출물은 테스트 뒤 정리합니다.

## 현재 제품 방향

- 이전 코치 오버레이는 제품에서 제거합니다.
- 활성 소스, 테스트, 스모크, README에 예전 코치 오버레이 코드나 다중 AI INI 흐름을 되살리지 않습니다.
- 기본 UX는 SCHNAIL처럼 원터치 스파링 실행에 가깝게 유지합니다.
- 추가 기능은 봇/맵/종족/난이도/빌드 선택, ELO 표시, 한글 봇 메모, 핫키 편집/가져오기, 전적/리플레이, 창모드/전체화면, APM/게임 시간 표시, 선택값 저장입니다.

## StarCraft / ChaosLauncher 흐름

- 실제 사람 클라이언트 root는 기본적으로 `C:\starai\SC116AI`를 씁니다.
- 봇 클라이언트는 `StarCraftRuntimeRoot.EnsureAiRoot`로 만든 `C:\starai\SC116AI_ai`를 씁니다. 사람 클라가 봇 DLL을 읽지 않게 하려면 두 `bwapi.ini`가 물리적으로 분리되어야 합니다.
- Program Files의 StarCraft Remastered 프로세스는 종료하지 않습니다.
- 스파링 시작 전 로컬 `C:\starai` 아래의 1.16.1 `StarCraft.exe`와 `Chaoslauncher - MultiInstance.exe`만 정리합니다.
- ChaosLauncher는 동시에 두 개를 띄우지 않습니다. 첫 StarCraft 시작 후 런처 창만 닫고, 같은 런처를 다시 열어 두 번째 StarCraft를 `RunScOnStartup`으로 시작합니다.

정상 스파링 순서:

1. `PracticeConfigurator.ApplyPlayerHost`가 사람 클라이언트용 INI를 `C:\starai\SC116AI`에 씁니다.
   - `ai =` 빈 값
   - `map = <선택 맵>`
   - `race = <내 종족>`
   - `enemy_race = <봇 종족>`
   - `character_name = StarAIHuman`
   - sound ON
2. ChaosLauncher `RunScOnStartup`으로 첫 번째 StarCraft를 시작해 Local PC 방을 만듭니다.
3. 방 생성 대기 뒤 `PracticeConfigurator.ApplyBotJoin`이 `C:\starai\SC116AI_ai`의 `bwapi.ini`를 봇 참가용으로 씁니다.
   - `ai = <선택 봇 DLL>`
   - `map =` 빈 값
   - `game = <방 이름>`
   - `race = <봇 종족>`
   - `enemy_race = <내 종족>`
   - `character_name = StarAIBot`
   - sound OFF
4. 첫 런처 창을 닫고 AI runtime의 런처를 열어 두 번째 StarCraft를 시작합니다. Start 버튼 자동 클릭은 핵심 경로에서 쓰지 않습니다.

## 마우스/화면 규칙

- 저장된 선택값은 앱을 켰을 때 UI 초기값으로만 씁니다. 앱이 켜진 뒤 스파링 시작 시에는 현재 화면의 콤보박스/체크박스 값이 항상 우선입니다.
- `스타 마우스 가두기` 체크가 꺼져 있으면 앱 자체 `ClipCursor`도 풀고, `wmode.ini`의 `ClipCursor`도 0이어야 합니다.
- 사람 클라이언트는 독점 전체화면을 쓰지 않습니다. `테두리 없는 전체 창모드` 체크가 켜져 있으면 W-MODE를 켠 채로 창 테두리만 제거해 모니터에 맞춥니다.
- AI 클라이언트는 항상 창모드, 소리 OFF, APMAlert OFF입니다.
- APMAlert는 사람 클라이언트에만 선택적으로 켭니다. 최근 APMAlert 크래시 로그가 있으면 기본/실행 모두 OFF로 둡니다. CoachAI를 되살리거나 봇/AI 클라이언트에 켜면 안 됩니다.
- 창 이동이 가능한 설정(`EnableWindowMove=1`)은 유지합니다.

## 회귀 방지

- 하나의 증상만 고쳤다고 완료 보고하지 않습니다.
- 최소 단위 테스트, 기본 smoke, 필요 시 live smoke까지 통과해야 합니다.
- `scripts\smoke.ps1`는 제거된 오버레이 재유입, taskbar 진입점, 단일 ChaosLauncher 흐름, 금지 배포 파일을 검사합니다.
- 실패를 본 뒤에는 먼저 실제 실행 중인 exe 경로와 taskbar CMD 경로를 확인합니다.
