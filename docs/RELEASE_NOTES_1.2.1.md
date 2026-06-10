# StarAI Practice Client 1.2.1

Alt+F4 종료 경로에서 발생할 수 있는 크래시를 막고, 재현 중 확인된 봇-맵 호환성 구멍을 보강한 핫픽스 릴리즈입니다.

## 수정 사항

- StarAI 매치 중 플레이어가 Alt+F4로 종료해도 정상 인게임 나가기 흐름으로 정리됩니다.
- 종료 중 Windows 응용 프로그램 오류 창이 남을 수 있던 문제를 줄였습니다.
- 게임 중 튕길 수 있는 `RedRum` + `(4)Jade` 조합을 차단했습니다.
- 여러 맵에서 불안정한 `Stone`은 호환 봇 풀에서 제외했습니다.
- 게임 중 튕길 수 있는 `CUBOT` + Fighting Spirit 계열 조합을 차단했습니다.
- 게임 중 튕길 수 있는 `Yuanheng Zhu` + Andromeda 조합을 차단했습니다.
- 랜덤/스파링 후보 필터는 현재 호환 가능한 맵이 없는 봇을 제거하고, 실행 직전에도 명시 봇-맵 호환성을 다시 확인합니다.

## 설치 방법

1. `StarAI-PracticeClient-1.2.1-win-x64.zip`을 다운로드합니다.
2. ZIP 파일의 압축을 풉니다.
3. 압축을 푼 폴더에서 `install.cmd`를 실행합니다.
4. 설치 후 `C:\starai\Start-StarAI-PracticeClient.cmd`로 실행합니다.
