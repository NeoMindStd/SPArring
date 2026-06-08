# AI Agent Guide

이 문서는 Codex/AI 작업자가 SPArring 프로젝트를 이어받을 때 지켜야 할 해석 규칙이다.

## 처음 할 일

1. `AGENTS.md`를 읽는다.
2. `docs\THREAD_HANDOFF.md`를 읽는다.
3. `docs\README.md`와 필요한 docs 문서를 읽는다.
4. `git status --short`로 현재 작업트리를 확인한다.
5. 사용자의 최신 요청 범위만 수행한다.

## 절대 금지

- 사용자가 요청하지 않은 release/tag/push/installer 업로드.
- 원본 SCHNAIL 폴더 수정.
- StarCraft Remastered 원본 폴더 수정.
- `C:\starai\Start-StarAI-PracticeClient.cmd` 진입점 파괴.
- 사람 런타임 `bwapi.ini`에 봇 DLL 설정.
- CoachAI 재도입.
- 독점 전체화면 설정.
- 사용자 Remastered 프로세스 종료.
- 작업 범위를 넘는 임의 리팩터링.

## 작업 범위 해석

사용자가 “꺼줘”, “원복해”, “그것만 해”라고 말하면 정말 그 범위만 수행한다.

예:

- “오버레이 꺼줘”는 현재 떠 있는 오버레이/프로세스를 끄라는 뜻일 수 있다.
- 별도 요청 없이 자동 종료 감시자, 신규 기능, 테스트 하네스 개선까지 진행하지 않는다.
- 이미 과한 변경을 했다면 즉시 해당 변경만 원복한다.

## 코드 변경 시 기본 흐름

1. 관련 파일을 읽는다.
2. 최소 변경을 설계한다.
3. `apply_patch`로 수정한다.
4. 위험도에 맞는 테스트를 실행한다.
5. 결과를 한국어로 짧고 정확히 보고한다.

기본 테스트:

```powershell
dotnet test .\StarAI.PracticeClient.sln -v:minimal
.\scripts\smoke.ps1
```

실행 흐름을 건드린 경우:

```powershell
.\scripts\smoke-app-start.ps1
```

단, 사용자가 “테스트 돌리지 말라”고 명시하면 실행하지 않는다.

## 런타임 작업 주의

로컬 런타임:

```text
C:\starai\SC116AI
C:\starai\SC116AI_ai
```

원본 참조:

```text
C:\Program Files (x86)\SCHNAIL Client
StarCraft Remastered 설치 폴더
```

원본 참조 대상은 읽기만 한다. 실행용 파일 복사/INI/MPQ 수정은 로컬 런타임에만 적용한다.

## 봇 로드 판단

AI 봇 로드 성공 여부는 AI 런타임 기준으로 판단한다.

확인할 것:

- `C:\starai\SC116AI_ai\bwapi-data\bwapi.ini`
- `ai = bwapi-data\AI\StarAI\Bots\<Bot>\<bot>.dll`
- 실제 AI 클라이언트가 인게임에 들어왔는지
- AI 유닛/건물이 동작하는지

사람 클라이언트의 빈 AI 모듈 경고는 별도 문제다.

## 문서 역할

- `docs/*`: 장기 스펙과 운영 문서.
- `docs/THREAD_HANDOFF.md`: 현재 스레드/WIP 인수인계.
- `docs/TECH_DECISIONS.md`: 구현 옵션과 선택 이유.
- `docs/ROADMAP.md`: 기능 단계와 방향.
- `docs/EXECUTION_CHECKLIST.md`: 완료/검증 체크리스트.

새로운 구조적 결정을 내리면 `docs/TECH_DECISIONS.md` 또는 관련 `docs` 문서에 반영한다. 단순 버그 수정은 `THREAD_HANDOFF.md`에 최신 상태를 남기는 정도로 충분하다.
