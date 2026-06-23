# 설치/재배포 기준

## StarCraft 1.16.1

- Sparring 릴리즈 패키지는 StarCraft 게임 본체를 포함하지 않는다.
- 사용자는 합법적으로 보유했거나 권한 있는 경로에서 확보한 StarCraft 1.16.1 폴더를 설치 프로그램에 지정한다.
- 설치 프로그램은 지정된 원본 폴더를 수정하지 않고 `C:\sparring\SC116AI`, `C:\sparring\SC116AI_ai`로 복사해 분리 런타임을 만든다.
- 앱 파일과 내장 봇/맵 데이터의 기본 설치 폴더는 `C:\sparring`이다.
- 필수 확인 파일은 `StarCraft.exe`, `stardat.mpq`, `broodat.mpq`, `patch_rt.mpq`이다.
- StarCraft 1.16.1 준비 참고 링크는 `[SSCAIT/BWAPI](https://sscaitournament.com/index.php?action=tutorial)`, `[Dave Churchill AI 자료실](https://davechurchill.ca/starcraft/resources/)`, `[StarEdit Network 안내 글](https://staredit.net/topic/17625/)`, `[iCCup 시작 페이지](https://iccup.com/sc_start.html)`를 안내한다. 오래된 미러 파일 링크는 403/404로 막힐 수 있다.
- StarCraft가 무료 공개된 적이 있더라도, Sparring가 게임 파일을 GitHub 릴리즈에 재배포할 권한까지 자동으로 생기는 것은 아니므로 직접 포함하지 않는다.
- 현재 Battle.net/Remastered 1.23.x 계열 설치 폴더를 Sparring 설치 프로그램이 1.16.1로 자동 다운그레이드하는 기능은 제공하지 않는다.

## SCHNAIL 관찰

- 로컬 SCHNAIL 설치에는 `starcraft_bundled`, `starcraft_bundled_forAI`, `jre`, `redists`가 들어 있는 것이 확인됐다.
- 이 관찰은 SCHNAIL이 어떤 방식을 택했는지에 대한 참고일 뿐, Sparring가 동일한 게임 파일/런타임 파일을 그대로 재배포해도 된다는 근거로 쓰지 않는다.
- Sparring 런타임은 `data` 폴더와 공개 소스/공식 다운로드 가능한 구성요소로 독립 구성한다.

## 선택 구성요소

- VC++ x86 런타임: 권장. 포함된 봇 DLL/EXE에서 `MSVCP90/MSVCR90`, `MSVCP120/MSVCR120`, `VCRUNTIME140/MSVCP140`, `api-ms-win-crt-*` 의존성이 확인됐다.
- Java 런타임: 커스텀 핫키 MPQ 반영에 필요. 설치 프로그램은 OpenJDK를 앱 설치 폴더의 `runtime\jdk`에 준비하고 시스템 Java 설정은 변경하지 않는다.
- .NET 런타임: Sparring 앱/설치 프로그램이 self-contained 배포이므로 별도 설치 대상이 아니다.

## Windows Defender / 백신 오탐 안내

- 일부 봇 DLL/EXE, BWAPI 계열 도구, 오래된 32비트 런타임 파일은 Windows Defender 또는 백신에서 오탐으로 차단될 수 있다.
- 사용자 문서에는 Windows 보안 `보호 기록`에서 차단 항목을 확인하고, 신뢰 가능한 Sparring 릴리즈 파일/설치 폴더라면 복원 또는 허용 처리하라고 안내한다.
- 반복 차단 시 Microsoft 공식 예외 안내 링크를 제공하고, `C:\sparring`, `C:\sparring\SC116AI`, `C:\sparring\SC116AI_ai`를 예외 후보로 안내한다.
- 출처가 불분명한 파일이나 공식 Sparring 릴리즈가 아닌 파일은 예외 처리하지 않도록 안내한다.

## 이전 봇 로드 실패와 VC++ 관계

- 로컬 `C:\sparring\SC116AI_ai\Errors`의 최근 실패 기록은 대부분 `EXCEPTION_ACCESS_VIOLATION`으로 남아 있으며, 단순한 VC++ DLL 누락 메시지는 확인되지 않았다.
- 따라서 기존 특정 맵-봇 조합 크래시를 VC++ 미설치 문제로 단정하지 않는다.
- 다만 새 PC에서는 VC++ x86 런타임 누락으로 일부 봇이 조용히 로드 실패할 수 있으므로 설치 단계에서 권장 옵션으로 제공한다.
