# StarAI Practice Client

StarAI Practice Client는 StarCraft 1.16.1 + BWAPI 기반의 로컬 AI 스파링 런처입니다. StarAI에 포함된 봇/맵 자산을 사용하고, 사람 클라이언트와 AI 클라이언트를 분리해서 `나 vs AI` 연습을 빠르게 시작합니다.

## 쉬운 설치

아래 안내가 기본 설치 방법입니다. 영어 안내가 필요하면 바로 아래의 `English Install Guide`를 참고하세요.

1. GitHub Releases에서 최신 `StarAI-PracticeClient-1.2.1-win-x64.zip`을 다운로드합니다.
2. ZIP 파일의 압축을 풉니다.
3. 압축을 푼 폴더 안의 `install.cmd`를 더블클릭합니다.
4. 설치가 끝나면 바탕화면의 `StarAI Practice Client` 바로가기를 실행합니다.
5. 런처에서 봇, 맵, 종족을 고른 뒤 `스파링 시작`을 누릅니다.

## 실제 플레이 예시

래더 모드에서 투혼 맵으로 진행한 Protoss vs Terran 24분 경기입니다. 봇/맵/종족을 고르고 래더 매칭으로 스파링하는 흐름을 볼 수 있습니다.

[![StarAI 래더 플레이 예시 - 투혼 Protoss vs Terran 24분 경기](https://img.youtube.com/vi/LJhL1WCl8wE/hqdefault.jpg)](https://www.youtube.com/watch?v=LJhL1WCl8wE)

설치 후 실행 파일은 아래 위치에 만들어집니다.

```text
C:\starai\Start-StarAI-PracticeClient.cmd
C:\starai\StarAI.PracticeClient\StarAI.PracticeClient.App.exe
```

## 설치 전 준비물

- Windows 10/11 64비트
- StarCraft 1.16.1 + BWAPI 런타임: `C:\starai\SC116AI`

봇/맵/핫키 기본 자산은 릴리즈 ZIP의 `data` 폴더에 포함됩니다. 최종 사용자 PC에 SCHNAIL Client를 설치할 필요는 없습니다.

## English Install Guide

1. Download `StarAI-PracticeClient-1.2.1-win-x64.zip` from GitHub Releases.
2. Extract the ZIP file.
3. Double-click `install.cmd` in the extracted folder.
4. Run the desktop shortcut named `StarAI Practice Client`.
5. Pick bot, map, and races, then press the sparring start button.

Requirements:

- Windows 10/11 64-bit
- StarCraft 1.16.1 + BWAPI runtime: `C:\starai\SC116AI`

## 현재 기능

- StarAI 내장 봇/맵 카탈로그 읽기
- 봇/맵/종족 선택과 호환성 필터
- 스파링 모드와 래더 후보 선택 UI
- 사람 런타임과 AI 런타임 분리
- 사람 `bwapi.ini`는 `ai =` 빈 값 유지, AI 런타임에만 선택 봇 적용
- cnc-ddraw 기반 사람 클라이언트 borderless/fullscreen 실행
- AI 클라이언트 별도 실행, 음소거, 합류 후 최소화
- StarAI 기본 핫키 CSV 가져오기/편집과 런타임 MPQ 반영
- 타이머/APM 오버레이
- 사용자 맵 폴더 `.scm/.scx` 추가
- 리플레이 저장 루트 설정
- 전적/APM 세션 기록
- 내장 봇 ELO와 SCR 한국 서버 래더 MMR/등급 참고 표기

## 핫키 반영 참고

핫키 탭에서 `CSV 저장`은 StarAI 작업 파일만 저장합니다. `런타임 반영`은 실제 사람 런타임의 `patch_rt.mpq`에 핫키를 반영합니다.

`런타임 반영`에는 StarAI에 포함된 TBL 컴파일러와 Java 11 이상이 필요할 수 있습니다. Java가 없다면 기본 스파링 실행은 가능하지만, 핫키 MPQ 반영은 실패할 수 있습니다.

## 고정 원칙

- StarAI 내장 `data` 폴더와 StarCraft Remastered 원본은 런타임에서 수정하지 않습니다.
- 사람 런타임은 `C:\starai\SC116AI`, AI 런타임은 `C:\starai\SC116AI_ai`를 사용합니다.
- 사람 `bwapi.ini`에는 봇 DLL을 넣지 않습니다.
- 독점 전체화면 대신 해상도 강제변경 없는 borderless/fullscreen 계열 실행을 사용합니다.
- 사용자 실행 진입점 `C:\starai\Start-StarAI-PracticeClient.cmd`를 유지합니다.

## 문제가 생겼을 때

- 설치 후 실행이 안 되면 `C:\starai\Start-StarAI-PracticeClient.cmd`를 다시 실행해 보세요.
- 봇/맵 목록이 비어 있으면 설치 폴더의 `C:\starai\StarAI.PracticeClient\data`가 함께 복사됐는지 확인하세요.
- 스파링 시작이 안 되면 `C:\starai\SC116AI\StarCraft.exe`가 존재하는지 확인하세요.
- AI 창은 합류 후 자동 최소화됩니다. 작업 표시줄에서 `Brood War Instance 2`를 복원하면 AI 화면을 볼 수 있습니다.
