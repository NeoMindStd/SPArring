$ErrorActionPreference = 'Stop'

$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$solution = Join-Path $repo 'StarAI.PracticeClient.sln'
$version = Join-Path $repo 'VERSION'
$appProject = Join-Path $repo 'src\StarAI.PracticeClient.App\StarAI.PracticeClient.App.csproj'
$taskbarLauncher = 'C:\starai\Start-StarAI-PracticeClient.cmd'

if (-not (Test-Path -LiteralPath $version)) {
    throw 'VERSION file is required because the taskbar launcher reads it.'
}

if (-not (Test-Path -LiteralPath $appProject)) {
    throw 'Taskbar-compatible app project path is missing.'
}

if (-not (Test-Path -LiteralPath $taskbarLauncher)) {
    throw 'Taskbar launcher is missing: C:\starai\Start-StarAI-PracticeClient.cmd'
}

$taskbarText = Get-Content -LiteralPath $taskbarLauncher -Raw
if ($taskbarText -notmatch [regex]::Escape('C:\starai\StarAI.PracticeClient')) {
    throw 'Taskbar launcher no longer points at this repo.'
}

function Invoke-NativeChecked {
    param(
        [Parameter(Mandatory = $true)]
        [scriptblock]$Command,

        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE"
    }
}

$coachMatches = Get-ChildItem -LiteralPath (Join-Path $repo 'src'), (Join-Path $repo 'tests') -Recurse -File |
    Where-Object { $_.Extension -in '.cs', '.csproj', '.resx' } |
    Select-String -Pattern 'CoachAI' -SimpleMatch
if ($coachMatches) {
    throw 'CoachAI reference found in source or tests.'
}

Invoke-NativeChecked -Name 'dotnet build' -Command { dotnet build $solution -c Release --nologo }
Invoke-NativeChecked -Name 'launcher smoke' -Command { dotnet run --project $appProject -c Release -- --smoke }
