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
$ExpectedRunDir = Join-Path $Artifacts "run\AIStarClient-$Version"

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

$appStartSmoke = Join-Path $RepoRoot "scripts\smoke-app-start.ps1"
if (-not (Test-Path -LiteralPath $appStartSmoke)) {
    throw "App start smoke script is missing: $appStartSmoke"
}

Write-Host "[smoke] Checking taskbar shortcut entrypoint"
$taskbarShortcut = Join-Path $env:APPDATA "Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\StarAI.PracticeClient.lnk"
if (-not (Test-Path $taskbarShortcut)) {
    throw "Taskbar shortcut is missing: $taskbarShortcut"
}

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($taskbarShortcut)
$expectedCmd = Join-Path $env:WINDIR "System32\cmd.exe"
$expectedArguments = "/c ""$rootLauncher"""
$expectedIconPrefix = Join-Path $ExpectedRunDir "StarAI.PracticeClient.App.exe"
if ($shortcut.TargetPath -ne $expectedCmd) {
    throw "Taskbar shortcut target is stale. Expected '$expectedCmd', got '$($shortcut.TargetPath)'."
}

if ($shortcut.Arguments -ne $expectedArguments) {
    throw "Taskbar shortcut arguments are stale. Expected '$expectedArguments', got '$($shortcut.Arguments)'."
}

if ($shortcut.WorkingDirectory -ne "C:\starai") {
    throw "Taskbar shortcut working directory is stale. Expected 'C:\starai', got '$($shortcut.WorkingDirectory)'."
}

if (-not $shortcut.IconLocation.StartsWith($expectedIconPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Taskbar shortcut icon should point at the current artifacts run exe. Expected prefix '$expectedIconPrefix', got '$($shortcut.IconLocation)'."
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

if ($practiceLauncherSource -notmatch "StopExistingLocalRuntime") {
    throw "Practice launcher must clean stale local StarCraft/ChaosLauncher processes before a new sparring launch."
}

if ($practiceLauncherSource -notmatch "StartAdditionalStarCraft") {
    throw "Practice launcher must support starting the second StarCraft instance."
}

$startAdditionalMatch = [regex]::Match($practiceLauncherSource, "public void StartAdditionalStarCraft[\s\S]*?\n    \}")
if (-not $startAdditionalMatch.Success) {
    throw "Could not inspect StartAdditionalStarCraft implementation."
}

if ($startAdditionalMatch.Value -notmatch "ClickStart" -or $startAdditionalMatch.Value -notmatch "WaitForCompletedStart") {
    throw "The active second StarCraft launch must request ChaosLauncher Start and verify that a second StarCraft start completed."
}

if ($practiceLauncherSource -notmatch "SetRunStarCraftOnStartup") {
    throw "Disabling ChaosLauncher startup must not disable the BWAPI plugin."
}

$mainFormSource = Get-Content (Join-Path $RepoRoot "src\StarAI.PracticeClient.App\MainForm.cs") -Raw
if ($mainFormSource -match "OpenChaosAndStartStarCraft\(botSettings\.StarCraftRoot") {
    throw "The AI client must not be launched as a second ChaosLauncher process; ChaosLauncher refuses with 'Already running'."
}

if ($mainFormSource -match "ApplyMultiInstanceSparring") {
    throw "The active sparring flow must not use one shared multi-instance bwapi.ini; it causes both clients to create/host instead of host/join."
}

if ($mainFormSource -notmatch "ApplyPlayerHost" -or $mainFormSource -notmatch "ApplyBotJoin" -or $mainFormSource -notmatch "StartAdditionalStarCraft") {
    throw "The active sparring flow must configure player host first, then bot join before starting the second StarCraft instance."
}

if ($mainFormSource -notmatch "StopExistingLocalRuntime") {
    throw "The app start flow must clean stale local StarCraft/ChaosLauncher processes before configuring a new match."
}

$practiceConfiguratorSource = Get-Content (Join-Path $RepoRoot "src\StarAI.PracticeClient.Core\PracticeConfigurator.cs") -Raw
if ($practiceConfiguratorSource -notmatch "ApplyPlayerHost" -or $practiceConfiguratorSource -notmatch "ApplyBotJoin") {
    throw "PracticeConfigurator must keep explicit player-host and bot-join role configuration methods."
}

$runtimeSource = Get-Content (Join-Path $RepoRoot "src\StarAI.PracticeClient.Core\StarCraftRuntimeRoot.cs") -Raw
if ($runtimeSource -notmatch "catch \(IOException\) when \(target\.Exists\)") {
    throw "AI runtime sync must tolerate locked StarCraft runtime files."
}

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
