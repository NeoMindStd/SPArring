# Sparring 1.8.0

Neo 내장 연습 봇에 E급 3종을 추가한 정규 업데이트입니다.

## 주요 변경

- `NeoProtossE`, `NeoTerranE`, `NeoZergE`가 추가되었습니다.
- NeoE 봇은 기존 NeoF보다 병력 생산과 공격 전환이 조금 빠른 한 단계 높은 난이도의 연습 상대입니다.
- 봇끼리 연습 경기를 준비하고 실행하는 흐름을 더 안정적으로 다듬었습니다.
- NeoE도 기존 NeoF와 같은 보수적인 맵 풀에서만 선택됩니다.

## 알려진 사항

- Neo 계열 봇은 아직 개발 중인 연습 봇입니다. 래더/랜덤 매칭에는 포함되지 않으며, 스파링 모드에서 직접 선택해 사용해 주세요.
- 일부 봇끼리의 경기는 장기전이 될 수 있습니다. 사람과 연습할 때는 종족, 맵, 빌드를 바꿔가며 난이도를 맞춰 보시면 좋습니다.

## 설치 방법

1. StarCraft 1.16.1 원본 클라이언트를 준비합니다. 이미 준비되어 있다면 이 단계는 건너뛰어도 됩니다.
   - 준비가 필요하면 [SSCAIT/BWAPI](https://sscaitournament.com/index.php?action=tutorial), [Dave Churchill AI 자료실](https://davechurchill.ca/starcraft/resources/), [StarEdit Network 안내 글](https://staredit.net/topic/17625/), [iCCup 시작 페이지](https://iccup.com/sc_start.html) 중 한 곳의 안내를 참고해 주세요.
   - 준비한 폴더를 열었을 때 `StarCraft.exe`, `stardat.mpq`, `broodat.mpq`, `patch_rt.mpq`가 바로 보이는 폴더를 설치 프로그램에서 선택하면 됩니다.
   - 최신 Battle.net/Remastered 설치 폴더를 그대로 사용하는 것은 지원하지 않습니다. 별도로 준비한 StarCraft 1.16.1 폴더가 필요합니다.
2. `Sparring-1.8.0-setup.exe`를 실행합니다.
3. 설치 프로그램에서 StarCraft 1.16.1 원본 폴더를 지정합니다.
4. 설치가 끝나면 `Sparring` 바로가기로 실행합니다.

## Windows 보안 안내

Sparring은 StarCraft 1.16.1, BWAPI, 오래된 32비트 AI 봇, cnc-ddraw 같은 구성요소를 함께 사용하므로 Windows Defender, SmartScreen, 백신에서 설치 또는 실행 중 확인을 요구할 수 있습니다.

SmartScreen이 표시되면 GitHub Releases에서 받은 설치 파일인지 확인한 뒤 `추가 정보`를 눌러 `실행`을 선택해 주세요.

Windows Defender가 설치 파일이나 실행 파일을 제거하거나 격리하면 `Windows 보안` -> `바이러스 및 위협 방지` -> `보호 기록`에서 차단 항목을 확인해 주세요. GitHub Releases에서 받은 Sparring 파일이 맞다면 `복원` 또는 `디바이스에서 허용`을 선택한 뒤 다시 실행하면 됩니다.

같은 파일이 반복해서 차단되는 경우 Windows 보안의 제외 설정에서 Sparring 설치 폴더와 `C:\sparring`을 제외로 추가할 수 있습니다.

공식 GitHub Releases에서 받은 파일이 아니거나 출처가 불분명한 파일은 예외 처리하지 말고 삭제해 주세요.
