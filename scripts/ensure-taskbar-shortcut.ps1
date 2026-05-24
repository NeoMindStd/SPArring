param(
    [string]$RootLauncher = "C:\starai\Start-StarAI-PracticeClient.cmd",
    [string]$WorkingDirectory = "C:\starai"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$version = (Get-Content (Join-Path $repoRoot "VERSION") -Raw).Trim()
$runExe = Join-Path $repoRoot "artifacts\run\AIStarClient-$version\StarAI.PracticeClient.App.exe"
$taskbarShortcut = Join-Path $env:APPDATA "Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\StarAI.PracticeClient.lnk"

if (-not (Test-Path $RootLauncher)) {
    throw "Root launcher not found: $RootLauncher"
}

$shortcutDir = Split-Path -Parent $taskbarShortcut
if (-not (Test-Path $shortcutDir)) {
    New-Item -ItemType Directory -Path $shortcutDir -Force | Out-Null
}

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($taskbarShortcut)
$shortcut.TargetPath = Join-Path $env:WINDIR "System32\cmd.exe"
$shortcut.Arguments = "/c ""$RootLauncher"""
$shortcut.WorkingDirectory = $WorkingDirectory
if (Test-Path $runExe) {
    $shortcut.IconLocation = "$runExe,0"
}

$shortcut.Save()

Write-Host "Taskbar shortcut updated: $taskbarShortcut"
