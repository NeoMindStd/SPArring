# StarAI Practice Client {{VERSION}} 설치 안내

## 설치 순서

1. StarCraft 1.16.1 원본 클라이언트를 먼저 준비합니다.
   - [SSCAIT/BWAPI](https://sscaitournament.com/index.php?action=tutorial), [Dave Churchill AI 자료실](https://davechurchill.ca/starcraft/resources/), [StarEdit Network 안내 글](https://staredit.net/topic/17625/), [iCCup 시작 페이지](https://iccup.com/sc_start.html) 중 1곳에서 클라이언트를 설치해주세요.
   - 기존에 1.16.1 클라이언트가 설치되어 있었다면 이 단계를 스킵하셔도 무관합니다.
   - 설치 프로그램에서 이 원본 폴더를 직접 지정해야 합니다.
2. GitHub Releases에서 `StarAI-PracticeClient-{{VERSION}}-setup.exe`를 다운로드합니다.
3. 설치 프로그램을 실행합니다.
4. 선택 구성요소를 확인합니다. VC++ x86 런타임은 봇 호환성을 위해 권장하고, Java 런타임은 커스텀 단축키 반영에 필요합니다.
5. 설치 경로를 확인합니다.
6. `StarCraft 1.16.1` 항목에서 원본 폴더를 지정합니다.
7. 필요한 경우 `바탕화면 바로가기 만들기`를 체크하고 설치합니다.

최신 Battle.net/Remastered 클라이언트 폴더를 1.16.1로 자동 다운그레이드하는 기능은 제공하지 않습니다. 별도로 준비한 StarCraft 1.16.1 폴더를 지정해 주세요.

## 설치 프로그램이 확인하는 StarCraft 파일

선택한 원본 폴더에는 아래 파일이 있어야 합니다.

```text
StarCraft.exe
stardat.mpq
broodat.mpq
patch_rt.mpq
```

## 설치 후 만들어지는 주요 경로

```text
C:\starai\SC116AI
C:\starai\SC116AI_ai
```

앱 자체는 설치 프로그램에서 선택한 경로에 설치됩니다. 기본값은 아래와 같습니다.

```text
C:\starai\StarAI.PracticeClient
```

## 참고

- StarCraft 원본 폴더는 수정하지 않습니다.
- 사람 런타임은 `C:\starai\SC116AI`, AI 런타임은 `C:\starai\SC116AI_ai`로 분리됩니다.
- StarAI 봇/맵 카탈로그는 릴리즈 패키지에 포함됩니다.
- SCHNAIL Client는 최종 사용자 설치 요구사항이 아닙니다.
- 기존 ZIP 방식이 필요하면 `StarAI-PracticeClient-{{VERSION}}-win-x64.zip` 안의 `install.cmd`를 사용할 수 있지만, 기본 권장 방식은 Setup EXE입니다.
- 새 설치는 바탕화면/시작 메뉴 바로가기와 `StarAI.PracticeClient.App.exe` 직접 실행을 사용합니다. `C:\starai\Start-StarAI-PracticeClient.cmd`는 1.3.1부터 생성하지 않습니다.

## Windows Defender 안내

일부 봇 DLL/EXE, BWAPI 계열 도구, 오래된 32비트 런타임 파일은 Windows Defender 또는 백신에서 오탐으로 차단될 수 있습니다.

차단이 의심되면 Windows 보안의 `보호 기록`에서 항목을 확인하고, 신뢰 가능한 StarAI 릴리즈 파일/설치 폴더라면 복원 또는 허용 처리해 주세요. 반복 차단되는 경우 [Microsoft Windows 보안 예외 안내](https://support.microsoft.com/windows/add-an-exclusion-to-windows-security-811816c0-4dfd-af4a-47e4-c301afe13b26)를 참고해 StarAI 설치 폴더와 `C:\starai\SC116AI`, `C:\starai\SC116AI_ai`를 예외로 추가할 수 있습니다.

출처가 불분명한 파일이나 공식 StarAI 릴리즈가 아닌 파일은 예외 처리하지 말고 삭제하는 것이 안전합니다.

## English Install Guide

1. Prepare a StarCraft 1.16.1 source folder first. Use one of the linked public setup pages above, or skip this if you already have a valid 1.16.1 folder.
2. Download `StarAI-PracticeClient-{{VERSION}}-setup.exe`.
3. Run the installer.
4. Review optional prerequisites.
5. Choose the StarAI install folder.
6. Select the StarCraft 1.16.1 source folder.
7. Enable the desktop shortcut option if desired, then install.

The installer validates `StarCraft.exe`, `stardat.mpq`, `broodat.mpq`, and `patch_rt.mpq`, then copies the source into separate player and AI runtime folders.
