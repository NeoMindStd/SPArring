param(
    [string] $Version = (Get-Content -LiteralPath (Join-Path $PSScriptRoot '..\VERSION') -Raw).Trim(),
    [string] $DistDir = (Join-Path $PSScriptRoot '..\artifacts\release\dist'),
    [string] $OutputDir = (Join-Path $PSScriptRoot '..\artifacts\security'),
    [string] $InputFile = '',
    [switch] $IncludeAllArtifacts
)

$ErrorActionPreference = 'Stop'

$dist = (Resolve-Path -LiteralPath $DistDir).Path
$output = New-Item -ItemType Directory -Force -Path $OutputDir
$packageRoot = Join-Path $output.FullName "defender-submission-$Version"
$zipPath = Join-Path $output.FullName "StarAI-PracticeClient-$Version-defender-submission.zip"
Remove-Item -LiteralPath $packageRoot, $zipPath -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $packageRoot | Out-Null

$artifacts = New-Object System.Collections.Generic.List[string]
if (-not [string]::IsNullOrWhiteSpace($InputFile)) {
    $artifacts.Add((Resolve-Path -LiteralPath $InputFile).Path)
}
elseif ($IncludeAllArtifacts) {
    foreach ($name in @(
        "StarAI-PracticeClient-$Version-setup.exe",
        "StarAI-PracticeClient-$Version-setup-folder.zip",
        "StarAI-PracticeClient-$Version-win-x64.zip")) {
        $path = Join-Path $dist $name
        if (Test-Path -LiteralPath $path -PathType Leaf) {
            $artifacts.Add($path)
        }
    }
}
else {
    $smallSetup = Join-Path $PSScriptRoot "..\artifacts\release\setup-folder-stage-$Version\StarAI-PracticeClient-$Version-setup.exe"
    $defaultSetup = if (Test-Path -LiteralPath $smallSetup -PathType Leaf) {
        $smallSetup
    }
    else {
        Join-Path $dist "StarAI-PracticeClient-$Version-setup.exe"
    }

    if (Test-Path -LiteralPath $defaultSetup -PathType Leaf) {
        $artifacts.Add($defaultSetup)
    }
}

foreach ($path in $artifacts) {
    Copy-Item -LiteralPath $path -Destination (Join-Path $packageRoot (Split-Path -Leaf $path)) -Force
}

$report = @"
# Microsoft Defender 오탐 제출 설명 템플릿

제품명: StarAI Practice Client
버전: $Version
공식 저장소: https://github.com/NeoMindStd/SPArring
공식 릴리즈: https://github.com/NeoMindStd/SPArring/releases/tag/$Version

## 증상

Windows Defender 또는 SmartScreen이 StarAI Practice Client 설치 파일이나 포함된 BWAPI/AI 봇 런타임 파일을 악성 또는 의심 파일로 탐지합니다.

## 설명

StarAI Practice Client는 StarCraft 1.16.1 + BWAPI 기반 로컬 AI 스파링 런처입니다.
패키지에는 오래된 32비트 AI 봇 DLL/EXE, BWAPI 런타임, cnc-ddraw 구성 파일이 포함될 수 있습니다.
이 파일들은 게임 자동화/AI 대전 실행을 위한 로컬 런타임 구성요소이며, 원격 제어/정보 탈취 목적의 프로그램이 아닙니다.

## 포함 자료

- 탐지된 파일 또는 대표 설치 파일

## 재현/확인 요청

첨부 파일은 공식 GitHub 릴리즈에서 내려받은 StarAI Practice Client 산출물입니다. 오탐 여부를 재분석해 주세요.
"@

Set-Content -LiteralPath (Join-Path $packageRoot 'false-positive-report.md') -Value $report -Encoding UTF8
Compress-Archive -Path (Join-Path $packageRoot '*') -DestinationPath $zipPath -Force
Get-Item -LiteralPath $zipPath
