# StarAI Practice Client 1.0.0

첫 공개 릴리즈입니다.

## 설치 방법

1. `StarAI-PracticeClient-1.0.0-win-x64.zip`을 다운로드합니다.
2. ZIP 파일의 압축을 풉니다.
3. 압축을 푼 폴더 안의 `install.cmd`를 더블클릭합니다.
4. 설치가 끝나면 바탕화면의 `StarAI Practice Client` 바로가기를 실행합니다.
5. 런처에서 봇/맵/종족을 고르고 `스파링 시작`을 누릅니다.

## 필수 준비물

- Windows 10/11 64비트
- StarCraft 1.16.1 + BWAPI 런타임: `C:\starai\SC116AI`
- SCHNAIL Client 기본 설치 위치: `C:\Program Files (x86)\SCHNAIL Client`

## 포함 기능

- SCHNAIL 봇/맵 카탈로그 기반 스파링 런처
- 사람/AI 런타임 분리
- 사람 클라이언트 borderless/fullscreen 실행
- AI 클라이언트 합류 후 자동 최소화와 음소거 설정
- 핫키 CSV 가져오기/편집/런타임 반영
- 타이머/APM 오버레이
- 리플레이 저장 경로 설정
- 전적/APM 기록
- SCHNAIL ELO 및 SCR MMR/등급 참고 표기

## 참고

- StarAI는 SCHNAIL 원본 설치 폴더를 수정하지 않습니다.
- 사람 런타임 `bwapi.ini`의 `ai` 값은 비워두고, AI 런타임에만 선택 봇을 적용합니다.
- 핫키 `런타임 반영` 기능에는 Java 11 이상이 필요할 수 있습니다.
