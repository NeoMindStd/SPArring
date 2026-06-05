# StarAI PracticeClient 작업 규칙

- 기본 답변과 작업 보고는 한국어 존댓말로 합니다.
- 사용자가 명시적으로 요청하지 않으면 GitHub release, tag, push, installer 업로드, 배포 파일 생성을 하지 않습니다.
- `C:\starai\Start-StarAI-PracticeClient.cmd`는 사용자의 작업표시줄 진입점입니다. 이 경로와 기대 구조를 깨면 안 됩니다.
- StarCraft/SCHNAIL/Remastered 원본 설치 폴더는 읽기 전용 참조 대상입니다. 실행용 변경은 `C:\starai\SC116AI`와 `C:\starai\SC116AI_ai`에만 적용합니다.
- 사람 클라이언트와 AI 클라이언트는 분리된 런타임을 씁니다. 사람 쪽 `bwapi.ini`의 `ai` 값은 비워야 하고, AI 쪽만 선택 봇을 설정합니다.
- CoachAI 또는 플레이어 유닛을 제어하는 오버레이 흐름을 되살리지 않습니다.
- 독점 전체화면은 금지합니다. 전체화면처럼 보이는 모드는 해상도 강제변경 없는 borderless/fullscreen 계열로 구현합니다.
- 코드 변경 시 기본 검증은 `dotnet test .\StarAI.PracticeClient.sln -v:minimal`과 `.\scripts\smoke.ps1`입니다.
- StarCraft/ChaosLauncher 실행 흐름을 건드린 경우에만 `.\scripts\smoke-app-start.ps1` 또는 실제 실행 smoke를 추가로 수행합니다.
