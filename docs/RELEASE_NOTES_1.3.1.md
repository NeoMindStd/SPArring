# Sparring 1.3.1

설치 안내와 Windows 설치 경험을 다듬은 패치 릴리즈입니다.

## 바뀐 점

- 설치 안내에서 StarCraft 1.16.1 준비 경로를 여러 공개 참고 페이지로 안내합니다.
- 최신 Battle.net/Remastered 클라이언트 폴더는 1.16.1로 자동 다운그레이드하지 않는다는 점을 명확히 안내합니다.
- Windows Defender 또는 백신이 일부 봇/런타임 파일을 오탐 차단할 수 있다는 주의사항과 조치 방법을 추가했습니다.
- 새 설치에서는 `C:\sparring\Start-Sparring.cmd`를 더 이상 생성하지 않습니다.
- 바탕화면/시작 메뉴 바로가기는 `Sparring.Client.exe`를 직접 실행합니다.

## 설치 방법

1. [SSCAIT/BWAPI](https://sscaitournament.com/index.php?action=tutorial), [Dave Churchill AI 자료실](https://davechurchill.ca/starcraft/resources/), [StarEdit Network 안내 글](https://staredit.net/topic/17625/), [iCCup 시작 페이지](https://iccup.com/sc_start.html) 중 1곳에서 StarCraft 1.16.1 원본 클라이언트를 준비합니다. 기존에 1.16.1 클라이언트가 설치되어 있었다면 이 단계를 스킵하셔도 됩니다.
2. `Sparring-1.3.1-setup.exe`를 실행합니다.
3. 선택 구성요소와 설치 경로를 확인합니다.
4. StarCraft 1.16.1 원본 폴더를 지정합니다.
5. 설치가 끝나면 `Sparring` 바로가기로 실행합니다.

## 보안 위협/차단 안내

Sparring는 StarCraft 1.16.1, BWAPI, 오래된 32비트 AI 봇 DLL/EXE를 함께 쓰기 때문에 Windows Defender, SmartScreen, 다른 백신 프로그램에서 오탐으로 막을 수 있습니다. 특히 처음 내려받은 설치 파일은 아직 많이 알려진 프로그램이 아니라서 SmartScreen이 “Windows의 PC 보호” 화면을 띄울 수 있고, 일부 봇 파일은 백신이 격리하면서 사라질 수 있습니다.

차단될 수 있는 파일 예시는 아래와 같습니다.

- `Sparring-1.3.1-setup.exe`
- `Sparring.Client.exe`
- `C:\sparring\SC116AI\Chaoslauncher - MultiInstance.exe`
- `C:\sparring\SC116AI\Plugins\BWAPI_PluginInjector.bwl`
- `C:\sparring\SC116AI\bwapi-data\BWAPI.dll`
- `C:\sparring\SC116AI\bwapi-data\TM\TournamentModule.dll`
- `C:\sparring\SC116AI\ddraw.dll`
- `C:\sparring\SC116AI_ai\bwapi-data\AI\Sparring\Bots\...\*.dll`
- 예: `Dragon\dragon.dll`, `Sapphire\Gems.dll`, `BananaBrain\BananaBrain.dll`, `Locutus\Locutus.dll`, `Steamhammer\Steamhammer.dll`

SmartScreen에서 설치 파일 실행이 막힌 경우:

1. 다운로드한 파일명이 `Sparring-1.3.1-setup.exe`인지 확인합니다.
2. 이 GitHub 릴리즈 페이지에서 받은 파일인지 확인합니다. 다른 사이트나 메신저로 받은 파일이면 실행하지 마세요.
3. 파란색 또는 회색 경고창에 `Windows의 PC 보호`가 뜨면 `추가 정보`를 누릅니다.
4. 파일 이름이 맞는지 다시 확인한 뒤 `실행` 또는 `실행 허용`을 누릅니다.

Windows Defender가 파일을 삭제하거나 격리한 경우:

1. 시작 메뉴에서 `Windows 보안`을 엽니다.
2. `바이러스 및 위협 방지`를 누릅니다.
3. `보호 기록`을 누릅니다.
4. 방금 차단된 항목을 찾습니다. 파일 경로에 `C:\sparring\Sparring`, `C:\sparring\SC116AI`, `C:\sparring\SC116AI_ai`가 들어 있는지 확인합니다.
5. 항목을 펼친 뒤 `작업` 버튼이 있으면 `복원` 또는 `디바이스에서 허용`을 선택합니다.
6. 복원한 뒤 설치 프로그램을 다시 실행하거나, 이미 설치했다면 `Sparring` 바로가기를 다시 실행합니다.

계속 같은 파일이 지워지는 경우에는 Sparring 폴더를 예외로 추가할 수 있습니다.

1. `Windows 보안`을 엽니다.
2. `바이러스 및 위협 방지`를 누릅니다.
3. `바이러스 및 위협 방지 설정` 아래의 `설정 관리`를 누릅니다.
4. 아래쪽의 `제외` 항목에서 `제외 추가 또는 제거`를 누릅니다.
5. `제외 사항 추가` -> `폴더`를 선택합니다.
6. 아래 폴더를 하나씩 추가합니다.

```text
C:\sparring\Sparring
C:\sparring\SC116AI
C:\sparring\SC116AI_ai
```

설치 경로를 바꿨다면 `C:\sparring\Sparring` 대신 직접 선택한 설치 폴더를 추가하면 됩니다.

주의할 점:

- 공식 릴리즈 페이지에서 받은 파일이 아닌 경우에는 예외 처리하지 말고 삭제하세요.
- 회사/학교 PC처럼 보안 설정이 관리되는 컴퓨터에서는 버튼이 비활성화될 수 있습니다. 이 경우 PC 관리자에게 문의해야 합니다.
- Windows 보안을 완전히 끄는 방식은 권장하지 않습니다. 필요한 Sparring 폴더만 예외로 추가하는 쪽이 안전합니다.
