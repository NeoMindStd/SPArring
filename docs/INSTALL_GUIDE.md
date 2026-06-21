# StarAI Practice Client {{VERSION}} 설치 안내

## 설치 순서

1. StarCraft 1.16.1 원본 클라이언트를 먼저 준비합니다.
   - 준비 안내: https://sscaitournament.com/index.php?action=tutorial
   - 설치 프로그램에서 이 원본 폴더를 직접 지정해야 합니다.
2. GitHub Releases에서 `StarAI-PracticeClient-{{VERSION}}-setup.exe`를 다운로드합니다.
3. 설치 프로그램을 실행합니다.
4. 설치 경로를 확인합니다.
5. `StarCraft 1.16.1` 항목에서 원본 폴더를 지정합니다.
6. 필요한 경우 `바탕화면 바로가기 만들기`를 체크하고 설치합니다.

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
C:\starai\Start-StarAI-PracticeClient.cmd
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

## English Install Guide

1. Prepare a StarCraft 1.16.1 source folder first.
   - Guide: https://sscaitournament.com/index.php?action=tutorial
2. Download `StarAI-PracticeClient-{{VERSION}}-setup.exe`.
3. Run the installer.
4. Choose the StarAI install folder.
5. Select the StarCraft 1.16.1 source folder.
6. Enable the desktop shortcut option if desired, then install.

The installer validates `StarCraft.exe`, `stardat.mpq`, `broodat.mpq`, and `patch_rt.mpq`, then copies the source into separate player and AI runtime folders.
