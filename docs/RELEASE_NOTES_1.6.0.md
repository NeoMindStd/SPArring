# Sparring 1.6.0

Sparring 내장 연습 봇을 추가하고, 개발 중인 봇이 래더/랜덤 매칭에 섞이지 않도록 정리한 정규 릴리즈입니다.

## 주요 변경 사항

- Sparring 내장 연습 봇 `NeoProtossF`, `NeoTerranF`, `NeoZergF`를 제공합니다.
- Neo 봇은 종족별 오프닝 빌드를 선택해서 연습할 수 있습니다.
- Neo 봇은 아직 개발 중인 연습용 봇이므로 중반 이후 판단이나 세부 컨트롤이 어색할 수 있습니다.
- Neo 봇은 수동 스파링에서 직접 선택할 수 있지만, 래더와 Random 매칭에는 포함되지 않습니다.
- Neo 봇의 공개 ELO 표기는 보류했습니다.
- 런처의 래더/랜덤 후보 선택 기준을 더 명확히 분리했습니다.

## 설치 방법

1. [SSCAIT/BWAPI](https://sscaitournament.com/index.php?action=tutorial), [Dave Churchill AI 자료실](https://davechurchill.ca/starcraft/resources/), [StarEdit Network 안내 글](https://staredit.net/topic/17625/), [iCCup 시작 페이지](https://iccup.com/sc_start.html) 중 1곳에서 StarCraft 1.16.1 원본 클라이언트를 준비합니다. 이미 1.16.1 폴더가 있다면 이 단계는 건너뛰어도 됩니다.
2. `Sparring-1.6.0-setup.exe`를 실행합니다.
3. 설치 프로그램에서 StarCraft 1.16.1 원본 폴더를 지정합니다.
4. 설치가 끝나면 `Sparring` 바로가기로 실행합니다.

## 보안 차단 안내

Sparring는 BWAPI, 오래된 32비트 AI 봇 DLL/EXE, cnc-ddraw 같은 구성요소를 사용하므로 Windows Defender, SmartScreen, 다른 백신에서 오탐 차단될 수 있습니다.

차단될 수 있는 파일 예시는 아래와 같습니다.

- `Sparring-1.6.0-setup.exe`
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
