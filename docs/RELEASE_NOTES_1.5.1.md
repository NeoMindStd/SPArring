# Sparring 1.5.1

고배율/작은 화면 PC에서 런처가 더 안정적으로 보이도록 다듬고, AI 클라이언트 시작 창 처리 회귀를 고친 핫픽스입니다.

## 주요 변경 사항

- Surface 계열처럼 배율이 큰 화면에서 런처 기본 창이 너무 작게 열려 게임/단축키 화면이 잘리던 문제를 줄였습니다.
- 게임, 설정, Hotkeys, 전적 탭이 창 크기 변경과 최대화 상태에서 더 안정적으로 배치되도록 조정했습니다.
- 설치 프로그램의 경로 선택 화면이 큰 글꼴 환경에서도 스크롤로 자연스럽게 표시되도록 다듬었습니다.
- Hotkeys 탭의 우측 기능 버튼 문구가 잘리지 않도록 짧고 읽기 쉬운 라벨로 정리했습니다.
- 게임 시작 중 AI 클라이언트가 이미 게임 화면에 들어간 경우에도 한 번 최소화되도록 수정했습니다.

## 설치 방법

1. [SSCAIT/BWAPI](https://sscaitournament.com/index.php?action=tutorial), [Dave Churchill AI 자료실](https://davechurchill.ca/starcraft/resources/), [StarEdit Network 안내 글](https://staredit.net/topic/17625/), [iCCup 시작 페이지](https://iccup.com/sc_start.html) 중 1곳에서 StarCraft 1.16.1 원본 클라이언트를 준비합니다. 이미 1.16.1 폴더가 있다면 이 단계는 건너뛰어도 됩니다.
2. `Sparring-1.5.1-setup.exe`를 실행합니다.
3. 설치 프로그램에서 StarCraft 1.16.1 원본 폴더를 지정합니다.
4. 설치가 끝나면 `Sparring` 바로가기로 실행합니다.

## 보안 차단 안내

Sparring는 BWAPI, 오래된 32비트 AI 봇 DLL/EXE, cnc-ddraw 같은 구성요소를 사용하므로 Windows Defender, SmartScreen, 다른 백신에서 오탐 차단될 수 있습니다.

SmartScreen에서 설치 파일 실행이 막히면 GitHub 릴리즈 페이지에서 받은 파일인지 확인한 뒤, 경고창의 `추가 정보`를 눌러 `실행`을 선택합니다.

Windows Defender가 파일을 삭제하거나 격리하면 `Windows 보안` -> `바이러스 및 위협 방지` -> `보호 기록`에서 차단 항목을 확인하고, 신뢰한 Sparring 릴리즈 파일이면 `복원` 또는 `디바이스에서 허용`을 선택합니다.
