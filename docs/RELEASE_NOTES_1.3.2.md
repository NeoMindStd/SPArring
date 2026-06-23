# Sparring 1.3.2

설치 프로그램 사용성을 다듬은 핫픽스입니다.

## 바뀐 점

- 설치 프로그램을 익숙한 Windows 마법사 형태로 정리했습니다.
- 설치 위치, StarCraft 1.16.1 원본 폴더, 선택 구성 요소를 단계별로 확인합니다.
- 설치 중 현재 단계와 진행 표시, 로그가 보입니다.
- 기본 설치 폴더를 `C:\sparring`로 변경했습니다.
- 설치 후 파일 누락 또는 손상 여부를 확인해, 백신 격리 등으로 파일이 사라진 경우 알기 쉽게 안내합니다.
- ZIP 패키지의 오래된 `install.cmd` fallback을 제거했습니다. 일반 설치는 Setup EXE를 사용해 주세요.

## 설치 방법

1. [SSCAIT/BWAPI](https://sscaitournament.com/index.php?action=tutorial), [Dave Churchill AI 자료실](https://davechurchill.ca/starcraft/resources/), [StarEdit Network 안내 글](https://staredit.net/topic/17625/), [iCCup 시작 페이지](https://iccup.com/sc_start.html) 중 1곳에서 StarCraft 1.16.1 원본 클라이언트를 준비합니다. 이미 1.16.1 폴더가 있다면 이 단계는 건너뛰어도 됩니다.
2. `Sparring-1.3.2-setup.exe`를 실행합니다.
3. 설치 폴더를 확인합니다. 기본값은 `C:\sparring`입니다.
4. StarCraft 1.16.1 원본 폴더를 지정합니다.
5. 선택 구성 요소를 확인한 뒤 설치합니다.
6. 설치가 끝나면 `Sparring` 바로가기로 실행합니다.

## 보안 차단 안내

Sparring는 BWAPI, 오래된 32비트 AI 봇 DLL/EXE, cnc-ddraw 같은 구성요소를 사용하므로 Windows Defender, SmartScreen, 다른 백신에서 오탐 차단될 수 있습니다.

차단될 수 있는 파일 예시는 아래와 같습니다.

- `Sparring-1.3.2-setup.exe`
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
