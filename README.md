# StarAI Practice Client

StarAI Practice Client는 StarCraft 1.16.1 + BWAPI 기반의 로컬 AI 스파링 런처입니다. StarAI에 포함된 봇/맵 카탈로그를 사용하고, 사람 클라이언트와 AI 클라이언트를 분리해서 `나 vs AI` 연습을 빠르게 시작합니다.

## 쉬운 설치

1. StarCraft 1.16.1 원본 클라이언트를 먼저 준비합니다. 자세한 기준은 아래 [StarCraft 1.16.1 준비](#starcraft-1161-준비)를 참고하세요.
2. GitHub Releases에서 최신 `StarAI-PracticeClient-1.3.0-setup.exe`를 다운로드합니다.
3. 설치 프로그램을 실행합니다.
4. 선택 구성요소를 확인합니다. VC++ x86 런타임은 봇 호환성을 위해 권장하고, Java 런타임은 커스텀 단축키 반영에 필요합니다.
5. 설치 경로를 확인하고, StarCraft 1.16.1 원본 폴더를 지정합니다.
6. `바탕화면 바로가기 만들기`를 선택한 뒤 설치를 진행합니다.
7. 설치가 끝나면 `StarAI Practice Client` 바로가기로 실행합니다.

설치 프로그램은 원본 StarCraft 폴더를 수정하지 않고, 아래 런타임 폴더를 새로 구성합니다.

```text
C:\starai\SC116AI
C:\starai\SC116AI_ai
C:\starai\Start-StarAI-PracticeClient.cmd
```

## StarCraft 1.16.1 준비

StarAI 릴리즈 패키지는 StarCraft 게임 본체를 포함하지 않습니다. StarCraft가 무료로 공개된 적이 있더라도, StarAI가 게임 파일을 GitHub 릴리즈에 재배포할 권한까지 자동으로 생기는 것은 아니기 때문입니다.

설치 프로그램에는 사용자가 합법적으로 보유했거나 권한 있는 경로에서 확보한 StarCraft 1.16.1 폴더를 지정해 주세요. 필요한 파일은 `StarCraft.exe`, `stardat.mpq`, `broodat.mpq`, `patch_rt.mpq`입니다. 최신 Battle.net/Remastered 설치 폴더는 일반적으로 BWAPI 4.4 기반 1.16.1 런타임 소스로 그대로 사용할 수 없습니다.

참고용으로 [SSCAIT/BWAPI 설치 안내](https://sscaitournament.com/index.php?action=tutorial)에 StarCraft 1.16.1 준비 방법이 정리되어 있지만, 오래된 미러 링크는 403/404로 막힐 수 있습니다. 그런 경우 StarAI 설치 프로그램에서 이미 준비된 1.16.1 폴더를 직접 지정하는 방식을 사용해 주세요.

## 선택 구성요소

- `VC++ x86 런타임 설치`: 권장입니다. 포함된 32비트 DLL/EXE 봇 중 일부는 VC++ 2008/2013/2015-2022 x86 런타임이 없으면 조용히 로드 실패할 수 있습니다.
- `Java 런타임 준비`: 커스텀 단축키를 `patch_rt.mpq`에 반영할 때 필요합니다. 설치 프로그램은 OpenJDK를 앱 폴더 아래에 준비하며 시스템 Java 설정은 바꾸지 않습니다.
- `.NET 런타임`: 별도 설치가 필요 없습니다. StarAI 앱과 설치 프로그램은 self-contained 배포입니다.

## 실제 플레이 예시

래더 모드에서 투혼 맵으로 진행한 Protoss vs Terran 24분 경기입니다. 봇, 맵, 종족을 고른 뒤 래더 매칭으로 스파링하는 흐름을 볼 수 있습니다.

[![StarAI 래더 플레이 예시 - 투혼 Protoss vs Terran 24분 경기](https://img.youtube.com/vi/LJhL1WCl8wE/hqdefault.jpg)](https://www.youtube.com/watch?v=LJhL1WCl8wE)

## 설치 전 준비물

- Windows 10/11 64비트
- StarCraft 1.16.1 원본 폴더
- 인터넷 연결: 설치 중 BWAPI, cnc-ddraw, 선택 구성요소 등 공개 런타임 구성요소를 내려받습니다.

StarAI 봇/맵 기본 카탈로그는 릴리즈 패키지에 포함됩니다. 최종 사용자 PC에 SCHNAIL Client를 설치할 필요는 없습니다.

## 현재 기능

- StarAI 내장 봇/맵 카탈로그
- 봇/맵/종족 선택과 호환성 필터
- 스파링 모드와 래더 후보 선택
- 사람 런타임 `C:\starai\SC116AI`, AI 런타임 `C:\starai\SC116AI_ai` 분리
- 사람 `bwapi.ini`에는 봇 DLL을 넣지 않고, AI 쪽에만 선택 봇 DLL 적용
- cnc-ddraw 기반 borderless/fullscreen 실행
- 타이머/APM 오버레이
- 사용자 맵 `.scm/.scx` 추가
- 리플레이 저장 루트 설정
- 전적/APM/래더 점수 기록
- StarAI 기본 핫키 CSV 편집과 런타임 반영

## 고정 원칙

- StarCraft 원본 폴더는 읽기 전용 소스로만 사용합니다.
- SCHNAIL 설치 폴더는 최종 사용자 런타임 의존성이 아닙니다.
- 사람/AI 런타임만 StarAI가 구성합니다.
- 독점 전체화면 대신 W-MODE/cnc-ddraw 기반 borderless/fullscreen 방향을 사용합니다.
- 기존 작업표시줄 진입점 `C:\starai\Start-StarAI-PracticeClient.cmd`는 유지합니다.

## English Install Guide

1. Prepare a legally obtained StarCraft 1.16.1 source folder first. StarAI does not redistribute the game files.
2. Download `StarAI-PracticeClient-1.3.0-setup.exe` from GitHub Releases.
3. Run the installer.
4. Review optional prerequisites. VC++ x86 is recommended for older native bots; Java is needed for custom hotkey MPQ patching.
5. Choose the install folder and select the StarCraft 1.16.1 source folder.
6. Keep the desktop shortcut option enabled if desired, then install.
7. Launch `StarAI Practice Client` from the desktop shortcut.

The installer copies the StarCraft source into separate local player/AI runtimes and does not modify the original source folder.
