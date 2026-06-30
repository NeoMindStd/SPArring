# Sparring 1.6.1

봇/맵 선택 안정성과 런처 화면 표시를 다듬은 핫픽스입니다.

## 주요 변경 사항

- Fighting Spirit 계열 맵에서 드롭되거나 일꾼이 멈춘다고 제보된 일부 봇 조합을 기본 선택 후보에서 제외했습니다.
- 개발 중인 `NeoProtossF`, `NeoTerranF`, `NeoZergF`는 기존처럼 수동 스파링에서 직접 선택할 수 있습니다.
- 봇 클라이언트가 게임 시작 후 다시 앞으로 뜨는 경우를 줄이고, 스파링 중에는 최소화된 상태를 더 안정적으로 유지하도록 보강했습니다.
- 게임 종료 후 다음 실행 때 이전 종료 로그가 오류처럼 보이는 경우를 줄였습니다.
- 런처와 설치 프로그램의 작은 창, 큰 글꼴, 여러 해상도 표시 상태에서 글자와 버튼이 겹치거나 잘리는 문제를 줄였습니다.

## 알려진 사항

- 일부 PC 환경이나 봇/맵 조합에서는 StarCraft 오류창이 없어도 연결이 끊길 수 있습니다. 계속 발생하면 다른 맵이나 다른 봇으로 스파링을 시작해 주세요.

## 설치 방법

1. StarCraft 1.16.1 원본 클라이언트를 준비합니다. 이미 준비되어 있다면 이 단계는 건너뛰어도 됩니다.
   - 준비가 필요하면 [SSCAIT/BWAPI](https://sscaitournament.com/index.php?action=tutorial), [Dave Churchill AI 자료실](https://davechurchill.ca/starcraft/resources/), [StarEdit Network 안내 글](https://staredit.net/topic/17625/), [iCCup 시작 페이지](https://iccup.com/sc_start.html) 중 1곳의 안내를 참고해 주세요.
   - 준비한 폴더를 열었을 때 `StarCraft.exe`, `stardat.mpq`, `broodat.mpq`, `patch_rt.mpq`가 바로 보이는 폴더를 설치 프로그램에서 선택하면 됩니다.
   - 최신 Battle.net/Remastered 설치 폴더는 그대로 사용할 수 없습니다. 별도로 준비한 StarCraft 1.16.1 폴더가 필요합니다.
2. `Sparring-1.6.1-setup.exe`를 실행합니다.
3. 설치 프로그램에서 StarCraft 1.16.1 원본 폴더를 지정합니다.
4. 설치가 끝나면 `Sparring` 바로가기로 실행합니다.

## 보안 차단 안내

Sparring은 StarCraft 1.16.1, BWAPI, 오래된 32비트 AI 봇, cnc-ddraw 같은 구성요소를 함께 사용하므로 Windows Defender, SmartScreen, 백신에서 설치 또는 실행 중 확인을 요구할 수 있습니다.

SmartScreen이 표시되면 GitHub Releases에서 받은 설치 파일인지 확인한 뒤 `추가 정보`를 눌러 `실행`을 선택하세요.

Windows Defender가 설치 파일이나 실행 파일을 삭제하거나 격리하면 `Windows 보안` -> `바이러스 및 위협 방지` -> `보호 기록`에서 차단 항목을 확인해 주세요. GitHub Releases에서 받은 Sparring 파일이 맞다면 `복원` 또는 `디바이스에서 허용`을 선택한 뒤 다시 실행하면 됩니다.

같은 파일이 반복해서 차단되는 경우 Windows 보안의 제외 설정에서 Sparring 설치 폴더인 `C:\sparring`을 예외로 추가할 수 있습니다.

공식 GitHub Releases에서 받은 파일이 아니거나 출처가 불분명한 파일은 예외 처리하지 말고 삭제하세요.
