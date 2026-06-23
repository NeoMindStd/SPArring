# Sparring 1.4.0

런처와 설치 프로그램을 더 일반적인 PC 환경에서 쓰기 좋게 다듬은 정규 릴리즈입니다.

## 주요 변경 사항

- 런처 창 크기를 조절해도 주요 영역이 자연스럽게 따라가도록 개선했습니다.
- 드롭다운을 StarCraft 분위기에 맞는 어두운 스타일로 바꿨습니다.
- 게임 내 타이머/APM 오버레이가 해상도와 글자 길이에 맞춰 잘리지 않도록 조정했습니다.
- 마지막으로 선택한 모드, 종족, 봇, 맵을 다음 실행 때 다시 불러옵니다.
- 래더 맵 파일과 미리보기가 기본 설치 패키지에 포함되는지 더 엄격하게 확인합니다.
- 설치 프로그램의 진행률 표시를 실제 파일 복사/다운로드 진행에 맞게 바꿨습니다.
- StarCraft 첫 실행 인트로와 팁 화면을 자동으로 건너뛰도록 했습니다.
- 봇 설명이 영어 원문뿐인 경우에도 성향과 예상 빌드를 한국어로 알아보기 쉽게 요약합니다.
- 일부 봇이 필요한 작업 폴더가 없어서 멈출 가능성을 줄이도록 런타임 준비 과정을 보강했습니다.

## 설치 방법

1. [SSCAIT/BWAPI](https://sscaitournament.com/index.php?action=tutorial), [Dave Churchill AI 자료실](https://davechurchill.ca/starcraft/resources/), [StarEdit Network 안내 글](https://staredit.net/topic/17625/), [iCCup 시작 페이지](https://iccup.com/sc_start.html) 중 1곳에서 StarCraft 1.16.1 원본 클라이언트를 준비합니다. 이미 1.16.1 폴더가 있다면 이 단계는 건너뛰어도 됩니다.
2. `Sparring-1.4.0-setup.exe`를 실행합니다.
3. 설치 프로그램에서 StarCraft 1.16.1 원본 폴더를 지정합니다.
4. 설치가 끝나면 `Sparring` 바로가기로 실행합니다.

## 보안 차단 안내

Sparring는 BWAPI, 오래된 32비트 AI 봇 DLL/EXE, cnc-ddraw 같은 구성요소를 사용하므로 Windows Defender, SmartScreen, 다른 백신에서 오탐 차단될 수 있습니다.

차단될 수 있는 파일 예시는 아래와 같습니다.

- `Sparring-1.4.0-setup.exe`
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
