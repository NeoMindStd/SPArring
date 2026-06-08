# SPArring Docs

SPArring은 "StarCraft Practice with AI"의 줄임말이며, 현재 배포/앱상의 이름인 StarAI Practice Client와 같은 프로젝트를 가리킨다.

이 디렉터리는 사람과 AI 작업자가 프로젝트의 목적, 구조, 불변조건, 실행 흐름을 빠르게 해석하도록 만든 장기 스펙 문서 모음이다. 스레드별 최신 작업 상태는 `docs/THREAD_HANDOFF.md`를 먼저 확인하고, 제품/구조 해석은 이 `docs` 디렉터리의 스펙 문서를 기준으로 삼는다.

## 문서 목록

- `PRODUCT_SPEC.md`
  - 제품 목표, 사용자 경험, 기능 범위, 모드, 현재 구현/보류 항목.
- `ARCHITECTURE_SPEC.md`
  - 프로젝트 모듈, 런타임 분리, 설정 파일, 실행 오케스트레이션, 데이터 흐름.
- `OPERATIONS_AND_VERIFICATION.md`
  - 설치/실행 경로, 개발 검증 명령, smoke 기준, 문제 진단 규칙.
- `AI_AGENT_GUIDE.md`
  - AI 작업자가 놓치면 안 되는 규칙, 작업 순서, 주의해야 할 오해.

## 우선순위

충돌하는 정보가 있을 때는 아래 순서로 판단한다.

1. 사용자의 최신 명시 지시
2. `AGENTS.md`
3. `docs/THREAD_HANDOFF.md`
4. `docs/*`
5. 오래된 README 또는 릴리즈 문서

릴리즈 태그, 푸시, installer 업로드는 사용자가 명시적으로 요청할 때만 수행한다.
