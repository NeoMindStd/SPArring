param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$Solution = Join-Path $RepoRoot "StarAI.PracticeClient.sln"
$Artifacts = Join-Path $RepoRoot "artifacts"
$PublishDir = Join-Path $RepoRoot "artifacts\publish\AIStarClient"
$BuildDir = Join-Path $Artifacts "smoke-build"
$Version = (Get-Content (Join-Path $RepoRoot "VERSION") -Raw).Trim()

function Invoke-Native {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $FilePath $($Arguments -join ' ')"
    }
}

Write-Host "[smoke] Repo: $RepoRoot"
Write-Host "[smoke] Running unit tests"
Invoke-Native -FilePath "dotnet" -Arguments @("test", $Solution, "-v:quiet")

Write-Host "[smoke] Publishing app without launching StarCraft"
if (Test-Path $PublishDir) {
    Remove-Item -LiteralPath $PublishDir -Recurse -Force
}

if (Test-Path $BuildDir) {
    Remove-Item -LiteralPath $BuildDir -Recurse -Force
}

$publishArgs = @(
    "publish",
    (Join-Path $RepoRoot "src\StarAI.PracticeClient.App\StarAI.PracticeClient.App.csproj"),
    "-c",
    $Configuration,
    "-o",
    $PublishDir,
    "-v:quiet",
    "-p:OutputPath=$BuildDir\app-output\"
)
Invoke-Native -FilePath "dotnet" -Arguments $publishArgs

Write-Host "[smoke] Checking local launch scripts"
$repoLauncher = Join-Path $RepoRoot "Start-AIStarClient.cmd"
$rootLauncher = "C:\starai\Start-StarAI-PracticeClient.cmd"
foreach ($launcher in @($repoLauncher, $rootLauncher)) {
    if (Test-Path $launcher) {
        $content = Get-Content $launcher -Raw
        if ($content -notmatch "artifacts\\run\\AIStarClient-") {
            throw "Launcher does not target artifacts\\run output: $launcher"
        }

        if ($content -notmatch "dotnet publish") {
            throw "Launcher does not rebuild before running: $launcher"
        }
    }
}

Write-Host "[smoke] Checking runtime split safety in source"
$automationSource = Get-Content (Join-Path $RepoRoot "src\StarAI.PracticeClient.Core\ChaosLauncherWindowAutomation.cs") -Raw
if ($automationSource -notmatch "PhysicalClickLock") {
    throw "ChaosLauncher physical click fallback must be guarded by PhysicalClickLock."
}

if ($automationSource -notmatch "starCraftRoot") {
    throw "ChaosLauncher window targeting must include the StarCraft root."
}

$chaosConfiguratorSource = Get-Content (Join-Path $RepoRoot "src\StarAI.PracticeClient.Core\ChaosLauncherConfigurator.cs") -Raw
if ($chaosConfiguratorSource -notmatch "RunScOnStartup") {
    throw "ChaosLauncher startup must use RunScOnStartup instead of relying only on UI button clicks."
}

if ($chaosConfiguratorSource -notmatch "WOW6432Node\\Blizzard Entertainment\\StarCraft") {
    throw "ChaosLauncher startup must set the 1.16.1 StarCraft install path before each launch."
}

$practiceLauncherSource = Get-Content (Join-Path $RepoRoot "src\StarAI.PracticeClient.Core\PracticeLauncher.cs") -Raw
if ($practiceLauncherSource -notmatch "ChaosStartupLock") {
    throw "ChaosLauncher startup must guard the global StarCraft install path while launching multiple clients."
}

$runtimeSource = Get-Content (Join-Path $RepoRoot "src\StarAI.PracticeClient.Core\StarCraftRuntimeRoot.cs") -Raw
if ($runtimeSource -notmatch "catch \(IOException\) when \(target\.Exists\)") {
    throw "AI runtime sync must tolerate locked StarCraft runtime files."
}

$expectedRunDir = Join-Path $Artifacts "run\AIStarClient-$Version"
Write-Host "[smoke] Local run output path: $expectedRunDir"

$forbiddenPatterns = @(
    "StarCraft.exe",
    "StarEdit.exe",
    "*.mpq",
    "Chaoslauncher*.exe",
    "*ChaosLauncher*.exe",
    "BWAPI.dll",
    "*.rep",
    "*.ERR"
)

$forbidden = foreach ($pattern in $forbiddenPatterns) {
    Get-ChildItem -LiteralPath $PublishDir -Recurse -File -Filter $pattern -ErrorAction SilentlyContinue
}

if ($forbidden) {
    $list = ($forbidden | Select-Object -ExpandProperty FullName) -join [Environment]::NewLine
    throw "Forbidden redistributable/runtime files found in publish output:$([Environment]::NewLine)$list"
}

Write-Host "[smoke] OK: tests passed and publish output contains no known game/runtime binaries."
