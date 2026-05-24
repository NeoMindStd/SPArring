# AIStarClient 작업 규칙

이 레포지토리에서 작업할 때는 아래 규칙을 우선합니다. 사용자가 명시적으로 요청하지 않으면 GitHub release, tag, push, installer 업로드, 배포 파일 생성은 하지 않습니다.

## 매번 지켜야 할 확인 순서

1. 실제 실행 중인 프로세스 경로를 먼저 확인합니다.
   - `Get-CimInstance Win32_Process`로 `StarAI.PracticeClient.App.exe`, `Chaoslauncher - MultiInstance.exe`, `StarCraft.exe`를 확인합니다.
   - 사용자가 실행하는 진입점은 `C:\starai\Start-StarAI-PracticeClient.cmd`입니다.
   - 사용자는 작업표시줄에 고정된 바로가기로 계속 실행합니다. 바로가기는 `cmd.exe /c "C:\starai\Start-StarAI-PracticeClient.cmd"`를 가리켜야 하며, `scripts\smoke.ps1`로 검증합니다.
2. 코드 변경 뒤에는 최소 검증을 반드시 실행합니다.
   - `dotnet test StarAI.PracticeClient.sln -v:minimal`
   - `scripts\smoke.ps1`
   - 런처/StarCraft 시작 흐름을 건드렸으면 `scripts\smoke-chaos-autostart.ps1`
   - 최종 사용자 실행 반영은 `C:\starai\Start-StarAI-PracticeClient.cmd`가 publish하는 `artifacts\run\AIStarClient-<VERSION>` 기준으로 확인합니다.
3. 최종 응답 전에 실제 실행 파일 경로와 버전을 확인합니다.
   - `artifacts\run\AIStarClient-<VERSION>\StarAI.PracticeClient.App.exe`
   - `src\StarAI.PracticeClient.App\bin\Release\net8.0-windows\StarAI.PracticeClient.App.exe`

## StarCraft / ChaosLauncher 규칙

- 실제 스파링 실행 root는 `C:\starai\SC116AI` 하나만 사용합니다.
- `C:\starai\SC116AI_ai`는 과거 분리 런타임 참고 폴더입니다. 실제 두 번째 ChaosLauncher 실행 대상으로 쓰면 `Already running`으로 막힙니다.
- ChaosLauncher는 두 개를 띄우지 않습니다. 하나의 `Chaoslauncher - MultiInstance.exe`에서 StarCraft를 두 번 시작합니다.
- `bwapi.ini`는 첫 StarCraft 실행 후 AI용으로 덮어쓰지 않습니다.
  - 활성 스파링 흐름은 `PracticeConfigurator.ApplyMultiInstanceSparring`으로 하나의 INI에 다중 인스턴스 설정을 씁니다.
  - `ai = <CoachAI>,<상대봇>` 형식이어야 합니다.
  - `race = <내 종족>,<상대 봇 종족>` 형식이어야 합니다.
  - `map`과 `game`은 둘 다 채웁니다. 첫 인스턴스는 방을 만들고, 두 번째 인스턴스는 같은 방에 참가합니다.
- 두 번째 StarCraft 시작 전에는 소리만 `OFF`로 바꿀 수 있습니다. AI/race/map/game 설정은 유지해야 합니다.
- `DisableStartupLaunch`는 `RunScOnStartup`만 꺼야 합니다. BWAPI Injector나 W-MODE 플러그인 상태를 바꾸면 두 번째 실행이 깨집니다.
- ChaosLauncher는 실행 폴더보다 `HKLM\SOFTWARE\WOW6432Node\Blizzard Entertainment\StarCraft`의 `InstallPath`/`Program`을 봅니다. 실행 직전에 이 값을 `C:\starai\SC116AI`로 맞춰야 합니다.
- Start 버튼 UI 자동 클릭은 취약합니다. 첫 실행은 `RunScOnStartup=1`, 두 번째 실행은 같은 ChaosLauncher의 Start 버튼으로만 처리합니다.
- W-MODE 마우스 가두기는 UI 체크박스와 `wmode.ini`의 `ClipCursor`가 반드시 일치해야 합니다.
- StarCraft 실행 중 `patch_rt.mpq`가 잠길 수 있습니다. 잠긴 파일은 패치 실패가 아니라 갱신 skip으로 처리합니다.

## CoachAI 규칙

- CoachAI는 조언만 해야 합니다. 사용자 유닛을 대신 조작하면 안 됩니다.
- 금지:
  - 자동 미네랄/가스 채취
  - 자동 일꾼 생산 또는 생산 취소
  - 자동 랠리 변경
  - 자동 서플라이/파일런
  - 상대 정보, 상대 빌드, 상대 자원, 안개 속 정보 표시
- 허용:
  - 내 빌드표 표시
  - 건물 랠리 표시
  - 내 idle worker / idle production / worker cut 조언
- 같은 종류의 알림은 최소 60초 쿨다운을 유지합니다.

## 실패 대응

- 사용자가 “안 고쳐졌다”고 하면 먼저 실제 실행 중인 exe 경로와 taskbar CMD publish 경로를 확인합니다.
- 증상 하나를 고친 뒤 바로 끝내지 말고, 단위 테스트, 기본 스모크, 필요한 라이브 스모크를 모두 통과한 뒤에만 완료로 말합니다.
- 배포 파일은 사용자가 명시적으로 요청할 때만 만들고, 요청 없이 생성된 배포 산출물은 삭제합니다.
