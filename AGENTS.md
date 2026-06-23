# StarAI PracticeClient 작업 규칙

- 기본 답변과 작업 보고는 한국어 존댓말로 합니다.
- 사용자가 명시적으로 요청하지 않으면 GitHub release, tag, push, installer 업로드, 배포 파일 생성을 하지 않습니다.
- `C:\starai\Start-StarAI-PracticeClient.cmd`는 1.3.1부터 레거시 진입점으로 제거 대상입니다. 새 설치/안내/검증은 EXE 바로가기와 `StarAI.PracticeClient.App.exe` 직접 실행 기준으로 합니다.
- StarCraft/SCHNAIL/Remastered 원본 설치 폴더는 읽기 전용 참조 대상입니다. 실행용 변경은 `C:\starai\SC116AI`와 `C:\starai\SC116AI_ai`에만 적용합니다.
- 사람 클라이언트와 AI 클라이언트는 분리된 런타임을 씁니다. 사람 쪽 `bwapi.ini`의 `ai` 값은 비워야 하고, AI 쪽만 선택 봇을 설정합니다.
- CoachAI 또는 플레이어 유닛을 제어하는 오버레이 흐름을 되살리지 않습니다.
- 독점 전체화면은 금지합니다. 전체화면처럼 보이는 모드는 해상도 강제변경 없는 borderless/fullscreen 계열로 구현합니다.
- 코드 변경 시 기본 검증은 `dotnet test .\StarAI.PracticeClient.sln -v:minimal`과 `.\scripts\smoke.ps1`입니다.
- StarCraft/ChaosLauncher 실행 흐름을 건드린 경우에만 `.\scripts\smoke-app-start.ps1` 또는 실제 실행 smoke를 추가로 수행합니다.

## 제품/배포 가드레일

- 구현과 검증은 현재 작업 PC의 QHD 환경에 맞춘 임시 해결로 끝내지 않습니다. 최소 FHD, QHD, UHD, 작은 노트북 화면, Windows 10/11, 일반 DPI/큰 글꼴 환경에서 UI가 깨지지 않도록 설계하고 smoke 기준에 반영합니다.
- 런처와 설치 프로그램은 크기 조절에 반응해야 합니다. 고정 좌표만으로 배치해 창 크기 변경 시 일부 UI가 제자리에 남는 구조를 새로 만들지 않습니다.
- 배포 페이지, README, 설치 프로그램, 런처 문구는 이 대화 내용을 모르는 일반 StarCraft 사용자 기준으로 작성합니다. 내부 검증 과정, 에이전트와의 대화 맥락, 불필요한 기술 세부사항은 사용자 노출 문구에 넣지 않습니다.
- 체크섬/무결성 검사는 설치 프로그램과 런처 내부 복구용으로 사용합니다. 릴리즈 페이지나 사용자 안내에는 일반 사용자가 직접 비교해야 하는 SHA256 목록을 기본 노출하지 않습니다.
- 릴리즈 전에는 사용자 관점의 설치 흐름과 실행 흐름을 직접 확인하고, 새 smoke가 이번 회귀를 잡을 수 있는지 함께 점검합니다.
