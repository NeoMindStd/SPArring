# Sparring 1.6.3

봇/맵 선택 안정성을 다듬은 핫픽스입니다.

## 주요 변경

- 정상 실행 가능한 일부 봇/맵 조합이 선택 목록에서 빠지던 문제를 수정했습니다.
- 특정 맵에서 바로 종료되거나 진행이 불안정한 오래된 봇 조합은 기본 선택 후보에서 제외했습니다.
- 봇/맵 호환성 판단을 더 세분화해서, 가능한 조합은 살리고 문제가 반복되는 조합만 숨기도록 조정했습니다.
- 게임 시작과 종료 흐름에서 AI 쪽 StarCraft 창이 사용자 플레이를 방해하지 않도록 안정성을 보강했습니다.

## 알려진 사항

- 일부 오래된 BWAPI 봇은 특정 맵에서 중간에 드롭되거나 제대로 플레이하지 못할 수 있습니다. 문제가 반복되면 다른 맵이나 다른 봇으로 스파링을 시작해 주세요.

## 설치 방법

1. StarCraft 1.16.1 원본 클라이언트를 준비합니다. 이미 준비되어 있다면 이 단계는 건너뛰어도 됩니다.
   - 준비가 필요하면 [SSCAIT/BWAPI](https://sscaitournament.com/index.php?action=tutorial), [Dave Churchill AI 자료실](https://davechurchill.ca/starcraft/resources/), [StarEdit Network 안내 글](https://staredit.net/topic/17625/), [iCCup 시작 페이지](https://iccup.com/sc_start.html) 중 한 곳의 안내를 참고해 주세요.
   - 준비한 폴더를 열었을 때 `StarCraft.exe`, `stardat.mpq`, `broodat.mpq`, `patch_rt.mpq`가 바로 보이는 폴더를 설치 프로그램에서 선택하면 됩니다.
   - 최신 Battle.net/Remastered 설치 폴더는 그대로 사용할 수 없습니다. 별도로 준비한 StarCraft 1.16.1 폴더가 필요합니다.
2. `Sparring-1.6.3-setup.exe`를 실행합니다.
3. 설치 프로그램에서 StarCraft 1.16.1 원본 폴더를 지정합니다.
4. 설치가 끝나면 `Sparring` 바로가기로 실행합니다.

## Windows 보안 안내

Sparring은 StarCraft 1.16.1, BWAPI, 오래된 32비트 AI 봇, cnc-ddraw 같은 구성요소를 함께 사용하므로 Windows Defender, SmartScreen, 백신에서 설치 또는 실행 중 확인을 요구할 수 있습니다.

SmartScreen이 표시되면 GitHub Releases에서 받은 설치 파일인지 확인한 뒤 `추가 정보`를 눌러 `실행`을 선택해 주세요.

Windows Defender가 설치 파일이나 실행 파일을 제거하거나 격리하면 `Windows 보안` -> `바이러스 및 위협 방지` -> `보호 기록`에서 차단 항목을 확인해 주세요. GitHub Releases에서 받은 Sparring 파일이 맞다면 `복원` 또는 `디바이스에서 허용`을 선택한 뒤 다시 실행하면 됩니다.

같은 파일이 반복해서 차단되는 경우 Windows 보안의 제외 설정에서 Sparring 설치 폴더인 `C:\sparring`을 예외로 추가할 수 있습니다.

공식 GitHub Releases에서 받은 파일이 아니거나 출처가 불분명한 파일은 예외 처리하지 말고 삭제하세요.
