# StarAI Practice Client 1.2.0

StarAI를 SCHNAIL 설치본 없이 독립 실행할 수 있게 하고, 래더 매칭을 실제 MMR 래더에 가깝게 다듬은 마이너 릴리즈입니다.

## 주요 변경

- StarAI 봇/맵/핫키 자산을 `data` 폴더에 포함해 최종 사용자가 SCHNAIL Client를 설치하지 않아도 됩니다.
- 릴리즈 패키징에 내장 `data` 폴더를 포함하고, 필수 봇/맵 카탈로그가 없으면 빌드가 실패하도록 했습니다.
- 래더 매칭은 현재 플레이어 MMR을 기준으로 비슷한 점수대의 봇이 더 높은 확률로 선택됩니다.
- 랜덤 맵 래더는 먼저 MMR에 가까운 봇을 고른 뒤, 해당 봇과 호환되는 맵 중 하나를 선택합니다.
- 봇이 결과 로그를 남기지 않는 경우에도 사람 런타임의 TournamentModule `gameState.txt`로 래더 결과를 보정할 수 있습니다.
- 표준 Elo 반올림 결과가 0점이어도 래더 승리 시 최소 +1점을 보장합니다.
- TournamentModule 근거를 바탕으로 최신 로컬 Halo 래더 승리 기록을 `1454 (+1)`로 보정했습니다.

## 설치 방법

1. `StarAI-PracticeClient-1.2.0-win-x64.zip`을 다운로드합니다.
2. ZIP 파일의 압축을 풉니다.
3. 압축을 푼 폴더에서 `install.cmd`를 실행합니다.
4. 설치 후 `C:\starai\Start-StarAI-PracticeClient.cmd`로 실행합니다.
