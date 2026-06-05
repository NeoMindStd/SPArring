# Execution Checklist

Last updated: 2026-06-05

## 필수 목표

- [x] 기존 구현 제거 후 새 프로젝트 골격 생성
- [x] SCHNAIL 원본/런타임 분리/작업표시줄 진입점 보호 규칙 문서화
- [x] SCHNAIL `bots.dat` / `maps.dat` 파싱
- [x] 봇-맵 호환성 필터 Core 테스트
- [x] 구현 옵션 비교/결정 로그 도입
- [x] 실제 봇/맵/종족 선택 UI
- [x] 핫키 옵션 비교 후 가져오기와 커스터마이징
- [x] 사람/AI `bwapi.ini` 분리 생성
- [x] W-MODE 기반 테두리 없는 전체 창모드 설정
- [x] 마우스 클립/감도 회피 설정 검증
- [x] 사람 클라이언트 + AI 클라이언트 실행 오케스트레이션
- [x] 타이머 옵션 비교 후 인게임 타이머/APM 오버레이
- [x] 전체 smoke와 실제 앱 스크린샷 검증

## 선택 목표

- [x] 사용자 맵 추가
- [x] 한글 런처
- [x] 리플레이 자동 저장 경로 설정
- [x] 래더/전적 기록
- [x] APM 기록
- [x] AI 클라이언트 창모드/음소거 관찰
- [ ] 봇 빌드 선택
- [x] 봇 난이도와 한국 서버 래더 MMR/등급 환산 표시
- [ ] Remastered 직접 실행 가능성 조사

## 최근 검증 증거

- `dotnet test .\StarAI.PracticeClient.sln -v:minimal`: 36개 통과
- `.\scripts\smoke.ps1`: Release 빌드 경고 0 / 오류 0
- 실제 실행 스크린샷: `artifacts\screenshots\human-starcraft-borderless-overlay-clean.png`
- 런처 UI 스크린샷: `artifacts\screenshots\starai-launcher-history-tab.png`
- 창 좌표 확인: 사람 StarCraft `0,0 2560x1440`, AI StarCraft `2333,287 650x517`, 런처 최소화, 오버레이 `230x38`
