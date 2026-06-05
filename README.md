# StarAI Practice Client

StarAI Practice Client는 StarCraft 1.16.1 + BWAPI 기반의 로컬 AI 스파링 런처입니다. SCHNAIL의 봇/맵 데이터를 읽기 전용으로 활용하고, 사람 클라이언트와 AI 클라이언트를 분리해서 `나 vs AI` 연습을 빠르게 시작합니다.

## 쉬운 설치

1. GitHub Releases에서 최신 `StarAI-PracticeClient-1.0.0-win-x64.zip`을 다운로드합니다.
2. ZIP 파일의 압축을 풉니다.
3. 압축을 푼 폴더 안의 `install.cmd`를 더블클릭합니다.
4. 설치가 끝나면 바탕화면의 `StarAI Practice Client` 바로가기를 실행합니다.
5. 런처에서 봇, 맵, 종족을 고른 뒤 `스파링 시작`을 누릅니다.

설치 후 실행 파일은 아래 위치에 만들어집니다.

```text
C:\starai\Start-StarAI-PracticeClient.cmd
C:\starai\StarAI.PracticeClient\StarAI.PracticeClient.App.exe
```

## 설치 전 준비물

- Windows 10/11 64비트
- StarCraft 1.16.1 + BWAPI 런타임: `C:\starai\SC116AI`
- SCHNAIL Client 기본 설치 위치: `C:\Program Files (x86)\SCHNAIL Client`

SCHNAIL은 봇과 맵 목록을 읽는 기준 데이터로만 사용합니다. StarAI는 SCHNAIL 원본 설치 폴더를 수정하지 않습니다.

## 현재 기능

- SCHNAIL 봇/맵 카탈로그 읽기
- 봇/맵/종족 선택과 호환성 필터
- 스파링 모드와 래더 후보 선택 UI
- 사람 런타임과 AI 런타임 분리
- 사람 `bwapi.ini`는 `ai =` 빈 값 유지, AI 런타임에만 선택 봇 적용
- cnc-ddraw 기반 사람 클라이언트 borderless/fullscreen 실행
- AI 클라이언트 별도 실행, 음소거, 합류 후 최소화
- SCHNAIL 핫키 CSV 가져오기/편집과 런타임 MPQ 반영
- 타이머/APM 오버레이
- 사용자 맵 폴더 `.scm/.scx` 추가
- 리플레이 저장 루트 설정
- 전적/APM 세션 기록
- SCHNAIL ELO와 SCR 한국 서버 래더 MMR/등급 참고 표기

## 핫키 반영 참고

핫키 탭에서 `CSV 저장`은 StarAI 작업 파일만 저장합니다. `런타임 반영`은 실제 사람 런타임의 `patch_rt.mpq`에 핫키를 반영합니다.

`런타임 반영`에는 SCHNAIL에 포함된 TBL 컴파일러와 Java 11 이상이 필요할 수 있습니다. Java가 없다면 기본 스파링 실행은 가능하지만, 핫키 MPQ 반영은 실패할 수 있습니다.

## 고정 원칙

- SCHNAIL 설치 폴더와 StarCraft Remastered 원본은 수정하지 않습니다.
- 사람 런타임은 `C:\starai\SC116AI`, AI 런타임은 `C:\starai\SC116AI_ai`를 사용합니다.
- 사람 `bwapi.ini`에는 봇 DLL을 넣지 않습니다.
- 독점 전체화면 대신 해상도 강제변경 없는 borderless/fullscreen 계열 실행을 사용합니다.
- 사용자 실행 진입점 `C:\starai\Start-StarAI-PracticeClient.cmd`를 유지합니다.

## 문제가 생겼을 때

- 설치 후 실행이 안 되면 `C:\starai\Start-StarAI-PracticeClient.cmd`를 다시 실행해 보세요.
- 봇/맵 목록이 비어 있으면 SCHNAIL Client가 기본 위치에 설치되어 있는지 확인하세요.
- 스파링 시작이 안 되면 `C:\starai\SC116AI\StarCraft.exe`가 존재하는지 확인하세요.
- AI 창은 합류 후 자동 최소화됩니다. 작업 표시줄에서 `Brood War Instance 2`를 복원하면 AI 화면을 볼 수 있습니다.

## 개발 검증

```powershell
dotnet test .\StarAI.PracticeClient.sln -v:minimal
.\scripts\smoke.ps1
.\scripts\smoke-app-start.ps1
```
