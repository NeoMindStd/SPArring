# AIStarClient 작업 규칙

이 레포에서 작업할 때는 아래 규칙을 우선한다. 사용자가 명시적으로 배포를 요청하지 않는 한 GitHub release, tag, push, installer 업로드를 하지 않는다.

## 매번 지켜야 할 확인 순서

1. 수정 전후 실제 실행 중인 프로세스 경로를 확인한다.
   - `Get-CimInstance Win32_Process`로 `StarAI.PracticeClient.App.exe`, `Chaoslauncher - MultiInstance.exe`, `StarCraft.exe`를 확인한다.
   - 사용자가 실행하는 진입점은 `C:\starai\Start-StarAI-PracticeClient.cmd`다. repo 안 `Start-AIStarClient.cmd`만 확인하고 끝내지 않는다.
   - 사용자는 작업표시줄에 고정한 `StarAI.PracticeClient.lnk`로 실행한다. 이 바로가기가 `cmd.exe /c "C:\starai\Start-StarAI-PracticeClient.cmd"`를 가리키는지 `scripts\smoke.ps1`로 확인하고, 틀어졌으면 `scripts\ensure-taskbar-shortcut.ps1`로 복구한다.
2. 코드 변경 후 최소 검증을 반드시 실행한다.
   - `dotnet test StarAI.PracticeClient.sln -v:minimal`
   - `scripts\smoke.ps1`
   - `dotnet publish src\StarAI.PracticeClient.App\StarAI.PracticeClient.App.csproj -c Release`
   - `dotnet publish ... -o artifacts\run\AIStarClient-$(Get-Content VERSION)`
3. 최종 답변 전에 실제 실행본 버전을 확인한다.
   - `artifacts\run\AIStarClient-<VERSION>\StarAI.PracticeClient.App.exe`
   - `src\StarAI.PracticeClient.App\bin\Release\net8.0-windows\StarAI.PracticeClient.App.exe`
   - 둘 다 최신 커밋의 `ProductVersion`을 가져야 한다.

## 런처/StarCraft 회귀 방지

- 플레이어 클라이언트와 AI 클라이언트는 별도 StarCraft 폴더를 사용한다.
  - 플레이어: `C:\starai\SC116AI`
  - AI: `C:\starai\SC116AI_ai`
- `bwapi.ini` 분리 조건을 깨면 안 된다.
  - 플레이어: CoachAI DLL, 플레이어 종족, 선택 맵, sound ON
  - AI: 선택 봇 DLL, 봇 종족, map 빈 값, sound OFF, character_name `StarAIBot`
- ChaosLauncher는 실행 폴더가 아니라 `HKLM\SOFTWARE\WOW6432Node\Blizzard Entertainment\StarCraft`의 `InstallPath`/`Program`을 본다. 플레이어/AI 런처를 열기 직전에 이 값을 해당 root로 바꿔야 한다.
- Start 버튼 UI 자동 클릭은 회귀하기 쉽다. 기본 실행 경로는 ChaosLauncher의 `RunScOnStartup=1`로 StarCraft를 자동 시작하는 방식이어야 한다.
- `WarnNoAdmin=0`을 유지해서 `SeDebugPrivilege` 경고 모달이 자동 시작을 막지 않게 한다.
- "병렬 실행"은 두 태스크를 동시에 요청하되, 전역 StarCraft install path를 바꾸는 짧은 구간만 lock으로 보호한다. 방 생성/참가는 BWAPI `auto_menu`가 기다리게 한다.
- W-MODE 커서 클립은 앱 체크박스와 `wmode.ini`의 `ClipCursor`가 같이 맞아야 한다.
- StarCraft가 실행 중일 때 `patch_rt.mpq`는 잠길 수 있다. 잠금은 실행 실패가 아니라 패치 갱신 skip으로 처리한다.

## CoachAI 회귀 방지

- CoachAI는 조언만 해야 한다. 유닛/일꾼/가스/생산/랠리를 대신 조작하면 안 된다.
- 허용: 빌드표, 랠리 표시, 내 idle worker/idle production류 조언
- 금지: 자동 미네랄/가스, 자동 일꾼 생산/취소, 자동 서플라이, 상대 정보/치트성 정보 표시
- 같은 알림은 60초 쿨다운을 유지한다.

## 실패 대응

- 사용자가 "안 고쳐졌다"고 하면 먼저 실제 실행 중인 exe 경로와 ProductVersion을 확인한다.
- 실행 경로가 오래된 `bin\Release`인지 `artifacts\run`인지 확인하기 전에는 코드가 반영됐다고 말하지 않는다.
- 증상 하나를 고친 뒤 바로 끝내지 말고, 위 회귀 체크를 통과시킨 뒤 답한다.
