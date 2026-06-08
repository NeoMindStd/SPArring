# Architecture

## 프로젝트 구조

- `src/StarAI.PracticeClient.Core`: 경로 정책, StarAI 내장 자산 카탈로그 로딩, 사용자 맵, 선택 호환성, 실행 계획 생성, 핫키/전적 모델
- `src/StarAI.PracticeClient.App`: WinForms 런처 UI, 설정/히스토리 저장, 오버레이, smoke entrypoint
- `tests/StarAI.PracticeClient.Tests`: Core 단위 테스트
- `scripts`: 반복 검증 스크립트

## 보호 대상

읽기 전용 제품 자산:

- `C:\starai\StarAI.PracticeClient\data`

개발/릴리즈 import 참조 대상:

- `C:\Program Files (x86)\SCHNAIL Client`
- StarCraft Remastered 원본 설치 폴더

쓰기 허용 대상:

- 레포 내부 빌드/설정 산출물
- 사람 런타임 `C:\starai\SC116AI`
- AI 런타임 `C:\starai\SC116AI_ai`

## 실행 계획 원칙

- PlayerHost는 `ai` 값을 비운다.
- AiOpponent는 선택 봇을 적용하고 sound/APMAlert를 끈다.
- 양쪽 모두 독점 전체화면을 쓰지 않는다.

## 사용자 데이터

- 설정: `%APPDATA%\StarAI.PracticeClient\settings.json`
- 전적/APM 기록: `%APPDATA%\StarAI.PracticeClient\history.json`
- 사용자 맵 원본 폴더는 읽기만 하고, 실행 시 런타임 `maps\StarAI`에 복사한다.

## MPQ 정책

- StarAI 내장 `data`와 원본 Remastered/Battle.net 파일은 런타임에서 수정하지 않는다.
- 핫키 적용은 사람 런타임 `patch_rt.mpq`에만 수행한다.
- `JMpqEditor` 직접 쓰기는 금지하고, StarAI에 포함된 `SFmpq` writer를 사용한다.
