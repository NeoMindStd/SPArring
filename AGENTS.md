# AIStarClient 작업 규칙

사용자가 명시적으로 요청하지 않으면 GitHub release, tag, push, installer 업로드, 배포 파일 생성은 하지 않습니다.

## 매번 지킬 점검 순서

1. 실제 실행 중인 프로세스 경로를 먼저 확인합니다.
   - `Get-CimInstance Win32_Process`로 `StarAI.PracticeClient.App.exe`, `Chaoslauncher - MultiInstance.exe`, `StarCraft.exe`를 확인합니다.
   - 사용자의 진입점은 `C:\starai\Start-StarAI-PracticeClient.cmd`입니다.
   - 사용자는 작업표시줄에 고정한 바로가기로 계속 실행합니다. 바로가기는 `cmd.exe /c "C:\starai\Start-StarAI-PracticeClient.cmd"`를 가리켜야 하며, `scripts\smoke.ps1`에서 검증합니다.
2. 코드 변경 뒤 최소 검증을 반드시 실행합니다.
   - `dotnet test StarAI.PracticeClient.sln -v:minimal`
   - `scripts\smoke.ps1`
   - StarCraft/ChaosLauncher 시작 흐름을 건드렸으면 `scripts\smoke-chaos-autostart.ps1`
   - 앱 버튼의 실제 `스파링 시작` 흐름을 건드렸으면 `scripts\smoke-app-start.ps1`
3. 사용자에게 완료라고 말하기 전 실제 실행 파일 경로와 버전을 확인합니다.
   - `artifacts\run\AIStarClient-<VERSION>\StarAI.PracticeClient.App.exe`
   - `src\StarAI.PracticeClient.App\bin\Release\net8.0-windows\StarAI.PracticeClient.App.exe`

## StarCraft / ChaosLauncher 규칙

- 실제 스파링 실행 root는 `C:\starai\SC116AI` 하나만 사용합니다.
- `C:\starai\SC116AI_ai`는 과거 분리 폴더이자 참고 폴더입니다. 실제 두 번째 ChaosLauncher 실행 대상으로 쓰면 `Already running` 계열 문제가 납니다.
- `스파링 시작`은 이전 실패로 남은 로컬 `C:\starai` 1.16.1 `StarCraft.exe` / `Chaoslauncher - MultiInstance.exe`를 먼저 정리해야 합니다.
- Program Files의 StarCraft Remastered는 종료하지 않습니다.
- ChaosLauncher를 두 개 동시에 띄우지 않습니다. 하나의 `Chaoslauncher - MultiInstance.exe`에서 StarCraft를 두 번 시작합니다.
- 활성 스파링 흐름은 단일 `ai = CoachAI,bot` 다중 인스턴스 INI를 쓰지 않습니다. 이 방식은 두 클라이언트가 같은 역할로 움직여 각각 방 생성 화면에 멈추는 회귀를 만들었습니다.
- 올바른 활성 흐름:
  1. `PracticeConfigurator.ApplyPlayerHost`로 사람 클라이언트 INI를 씁니다.
  2. `OpenChaosAndStartStarCraft`로 첫 번째 StarCraft를 시작합니다.
  3. 방 생성이 끝나도록 짧게 기다립니다.
  4. `PracticeConfigurator.ApplyBotJoin`으로 같은 `bwapi.ini`를 AI 참가 전용으로 바꿉니다.
  5. 같은 ChaosLauncher의 Start 버튼을 눌러 두 번째 StarCraft를 시작합니다.
- 첫 번째 클라이언트: `ai = CoachAI`, `map = <선택 맵>`, `race = <내 종족>`, `character_name = StarAIHuman`, sound ON.
- 두 번째 클라이언트: `ai = <상대 봇>`, `map = ` 빈 값, `game = <방 이름>`, `race = <봇 종족>`, `character_name = StarAIBot`, sound OFF.
- `DisableStartupLaunch`는 `RunScOnStartup`만 꺼야 합니다. BWAPI Injector나 W-MODE 플러그인 상태를 바꾸면 두 번째 실행이 깨집니다.
- ChaosLauncher는 실행 폴더보다 `HKLM\SOFTWARE\WOW6432Node\Blizzard Entertainment\StarCraft`의 `InstallPath`/`Program`을 봅니다. 실행 직전에 이 값을 `C:\starai\SC116AI`로 맞춰야 합니다.
- `patch_rt.mpq`가 사용 중이면 갱신 실패가 날 수 있습니다. 런타임 파일 잠금은 패치 실패가 아니라 skip 가능 상태로 처리합니다.

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

- 사용자가 “안 고쳐졌다”고 말하면 먼저 실제 실행 중인 exe 경로와 taskbar CMD publish 경로를 확인합니다.
- 하나의 증상만 고친 뒤 완료라고 말하지 않습니다. 단위 테스트, 기본 smoke, 필요한 live smoke를 모두 통과한 뒤에만 완료라고 말합니다.
- 배포 파일은 사용자가 명시적으로 요청할 때만 만들고, 요청 없이 생성된 배포 산출물은 삭제합니다.
