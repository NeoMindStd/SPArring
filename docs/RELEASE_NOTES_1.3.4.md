# StarAI Practice Client 1.3.4

설치 파일이 백신 또는 Windows 보안에 의해 일부 제거되는 상황을 더 잘 복구하도록 개선한 핫픽스입니다.

## 변경 사항

- 런처 시작 시 설치 파일과 필수 런타임 파일을 확인합니다.
- 누락되거나 손상된 앱/데이터 파일은 설치 시 저장된 복구 캐시에서 자동 복구합니다.
- StarCraft/BWAPI 런타임 파일이 빠진 경우 가능한 범위에서 런타임 복구를 시도합니다.
- 복구할 수 없는 파일이 있으면 Windows 보안 보호 기록 확인과 예외 처리 방법을 안내합니다.
- 작은 Setup EXE와 `payload.zip`을 함께 담은 보조 설치 패키지를 추가했습니다.

## 설치 방법

1. StarCraft 1.16.1 원본 폴더를 준비합니다.
2. `StarAI-PracticeClient-1.3.4-setup.exe`를 실행합니다.
3. 설치 프로그램에서 StarCraft 1.16.1 원본 폴더를 지정합니다.
4. 설치가 끝나면 `StarAI Practice Client` 바로가기로 실행합니다.
