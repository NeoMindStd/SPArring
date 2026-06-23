# Sparring 1.5.0

런처 사용성과 설치 후 첫 실행 흐름을 다듬고, Sparring 이름으로 정리한 정규 릴리즈입니다.

## 주요 변경 사항

- 앱 이름, 실행 파일, 설치 산출물 표기를 `Sparring`으로 통일했습니다.
- 런처의 게임/설정/단축키/전적 화면이 창 크기에 맞춰 더 자연스럽게 배치되도록 개선했습니다.
- StarCraft 분위기에 맞는 어두운 드롭다운과 더 읽기 쉬운 입력 UI를 적용했습니다.
- 단축키 설정 화면을 유닛/건물별 명령 카드 배치에 가깝게 정리했습니다.
- 게임 시작 전 게임 속도, 마우스 스크롤, 키보드 스크롤 값을 조절할 수 있습니다.
- 마지막으로 선택한 모드, 종족, 봇, 맵을 다음 실행 때 다시 불러옵니다.
- 내장 맵과 미리보기 구성을 보강하고, 1.16.1에서 바로 실행 가능한 맵만 기본 선택지로 제공합니다.
- 실행 시 새 GitHub 릴리즈가 있으면 업데이트 안내를 표시합니다.
- 설치와 실행 중 누락된 필수 파일을 더 알기 쉽게 안내하고 복구할 수 있도록 개선했습니다.

## 설치 방법

1. [SSCAIT/BWAPI](https://sscaitournament.com/index.php?action=tutorial), [Dave Churchill AI 자료실](https://davechurchill.ca/starcraft/resources/), [StarEdit Network 안내 글](https://staredit.net/topic/17625/), [iCCup 시작 페이지](https://iccup.com/sc_start.html) 중 1곳에서 StarCraft 1.16.1 원본 클라이언트를 준비합니다. 이미 1.16.1 폴더가 있다면 이 단계는 건너뛰어도 됩니다.
2. `Sparring-1.5.0-setup.exe`를 실행합니다.
3. 설치 프로그램에서 StarCraft 1.16.1 원본 폴더를 지정합니다.
4. 설치가 끝나면 `Sparring` 바로가기로 실행합니다.

## 보안 차단 안내

Sparring는 BWAPI, 오래된 32비트 AI 봇 DLL/EXE, cnc-ddraw 같은 구성요소를 사용하므로 Windows Defender, SmartScreen, 다른 백신에서 오탐 차단될 수 있습니다.

차단될 수 있는 파일 예시는 아래와 같습니다.

- `Sparring-1.5.0-setup.exe`
- `Sparring.Client.exe`
- `C:\sparring\SC116AI\Chaoslauncher - MultiInstance.exe`
- `C:\sparring\SC116AI\Plugins\BWAPI_PluginInjector.bwl`
- `C:\sparring\SC116AI\bwapi-data\BWAPI.dll`
- `C:\sparring\SC116AI\bwapi-data\TM\TournamentModule.dll`
- `C:\sparring\SC116AI\ddraw.dll`
- `C:\sparring\SC116AI_ai\bwapi-data\AI\Sparring\Bots\...\*.dll`

SmartScreen에서 설치 파일 실행이 막히면 GitHub 릴리즈 페이지에서 받은 파일인지 확인한 뒤, 경고창의 `추가 정보`를 눌러 `실행`을 선택합니다.

Windows Defender가 파일을 삭제하거나 격리하면 `Windows 보안` -> `바이러스 및 위협 방지` -> `보호 기록`에서 차단 항목을 확인하고, 신뢰한 Sparring 릴리즈 파일이면 `복원` 또는 `디바이스에서 허용`을 선택합니다.

같은 파일이 반복해서 지워지면 Windows 보안의 제외 설정에서 아래 폴더를 예외로 추가할 수 있습니다.

```text
C:\sparring
C:\sparring\SC116AI
C:\sparring\SC116AI_ai
```

공식 릴리즈 페이지에서 받은 파일이 아니거나 출처가 불분명한 파일은 예외 처리하지 말고 삭제하세요.
