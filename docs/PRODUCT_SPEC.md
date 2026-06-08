# Product Spec

## 제품명

- 정식/배포 친화 이름: SPArring
- 풀네임: StarCraft Practice with AI
- 레포/기존 앱 이름: StarAI Practice Client

## 한 줄 정의

SPArring은 StarCraft 1.16.1 + BWAPI 환경에서 사람이 다양한 AI 봇과 빠르게 연습 경기할 수 있게 해 주는 로컬 스파링 런처다.

## 핵심 목표

- SCHNAIL의 봇/맵 카탈로그를 읽기 전용으로 활용한다.
- 사람 클라이언트와 AI 클라이언트를 분리된 로컬 런타임으로 실행한다.
- 사람은 전체화면처럼 보이는 borderless/fullscreen 창에서 플레이한다.
- AI는 별도 창으로 실행되며 기본 음소거/관찰 가능 상태를 지향한다.
- 사용자는 봇, 맵, 종족, 난이도 정보를 보고 빠르게 스파링을 시작한다.
- 기존 StarCraft/SCHNAIL/Remastered 원본 설치는 수정하지 않는다.

## 대상 사용자

- StarCraft: Brood War를 연습하는 한국어 사용자.
- SCHNAIL식 봇 스파링 경험은 원하지만 맵/종족/상대 봇 선택권이 더 필요한 사용자.
- 프로그래밍 지식 없이 `C:\starai\Start-StarAI-PracticeClient.cmd` 또는 바로가기로 실행하려는 사용자.

## 필수 사용자 시나리오

1. 런처 실행
   - 사용자는 작업표시줄 또는 바로가기에서 `C:\starai\Start-StarAI-PracticeClient.cmd`를 실행한다.
   - 런처는 SCHNAIL 봇/맵 카탈로그와 로컬 런타임 상태를 읽는다.

2. 스파링 모드
   - 사용자는 내 종족, 상대 종족 필터, 봇, 맵을 고른다.
   - 봇과 맵은 호환되는 조합만 선택 가능해야 한다.
   - `스파링 시작`을 누르면 사람 클라이언트가 방을 만들고 AI 클라이언트가 참가한다.

3. 래더 모드
   - 사용자는 내 종족, 상대 종족, 맵을 고른다.
   - 런처는 해당 조건에서 가능한 봇 후보 중 랜덤 매칭한다.
   - 세션 기록에는 봇, 맵, 종족, 시간, APM 등이 남는다.
   - 봇 로그가 남긴 결과를 우선 판정한다. 로그가 없으면 래더 결과를 추정하지 않고 미확인으로 남긴다.
   - 사용자는 게임 탭에서 래더 점수를 초기화하거나 임의 값으로 조정할 수 있다.

4. 핫키
   - 사용자는 SCHNAIL CSV 또는 Battle.net/Remastered `STR_*` 핫키 파일을 가져와 작업 CSV에 반영하고 편집한다.
   - 런타임 반영은 사람 런타임 `patch_rt.mpq`에만 적용한다.
   - 원본 SCHNAIL 설치 폴더의 MPQ는 수정하지 않는다.

5. 인게임 보조 표시
   - APMAlert는 크래시 이력 때문에 기본 OFF다.
   - 타이머/APM 표시는 외부 클릭 투과 오버레이로 제공한다.
   - 현재 WIP는 StarCraft 인게임 HUD 감지 후 타이머/APM을 시작한다.

## 구현된 기능

- SCHNAIL `bots.dat` / `maps.dat` 카탈로그 파싱.
- 봇/맵 호환성 필터.
- 스파링 모드 UI.
- 래더 후보 선택 UI와 랜덤 봇/맵 후보 선택.
- Remastered 현재 래더맵 폴더 읽기와 SCHNAIL 호환 맵 ID 연결.
- `AI 이름 가리기` 옵션. 기본값은 가림이며, 해제하면 AI 플레이어 이름을 선택 봇 이름으로 표시한다.
- SCHNAIL ELO와 SCR 한국 서버 MMR/등급 참고 표기.
- 사람 래더 점수 저장, 초기화, 임의 조정, ELO 결과 반영.
- 사용자 맵 폴더 `.scm` / `.scx` 읽기.
- 리플레이 저장 루트 설정.
- 사람/AI 런타임 분리 생성 및 설정.
- cnc-ddraw 기반 사람 클라이언트 borderless/fullscreen.
- AI 클라이언트 음소거/별도 창 실행.
- 게임 종료 감지 후 오버레이 정리와 로컬 사람/AI 런타임 자동 종료.
- 핫키 CSV 편집, SCHNAIL/Battle.net 가져오기, SCHNAIL 아이콘 기반 3x3 명령 카드 UI, 사람 런타임 MPQ 반영.
- 승패/모드/MMR/판정 근거를 포함한 세션 히스토리와 APM 기록.
- 실제 실행 smoke와 스크린샷 기반 검증.

## 보류 또는 미완성 기능

- 봇별 빌드오더 강제 선택.
  - BWAPI 봇마다 설정 방식이 달라 공통 규격이 없다.
  - 현재는 봇 자체 전략 설명/메타데이터 보강이 현실적이다.
- 봇 로그가 없는 자연 승패의 고신뢰 판정.
  - 현재는 봇 로그 우선이며, 로그 없는 래더 결과는 미확인으로 둔다.
  - replay/BWAPI 이벤트/스코어 화면 판정 설계가 필요하다.
- Remastered 직접 실행.
  - BWAPI 1.16.1 기반과 기술 기반이 달라 별도 조사 대상이다.
- 사람 클라이언트의 빈 AI 모듈 경고 억제.
  - 사람 `bwapi.ini`의 `ai =` 빈 값 원칙을 깨지 않는 해법이 필요하다.

## 제품 불변조건

- 사람 런타임: `C:\starai\SC116AI`
- AI 런타임: `C:\starai\SC116AI_ai`
- 사람 `bwapi.ini`의 `ai` 값은 비운다.
- AI `bwapi.ini`에만 선택 봇 DLL을 설정한다.
- CoachAI 또는 플레이어 유닛 제어 흐름을 되살리지 않는다.
- SCHNAIL/Remastered 원본 폴더는 읽기 전용이다.
- 독점 전체화면은 금지한다.
- 사용자 진입점 `C:\starai\Start-StarAI-PracticeClient.cmd`는 유지한다.
