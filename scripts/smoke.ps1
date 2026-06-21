$ErrorActionPreference = 'Stop'

$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$solution = Join-Path $repo 'StarAI.PracticeClient.sln'
$version = Join-Path $repo 'VERSION'
$appProject = Join-Path $repo 'src\StarAI.PracticeClient.App\StarAI.PracticeClient.App.csproj'

if (-not (Test-Path -LiteralPath $version)) {
    throw 'VERSION file is required.'
}

if (-not (Test-Path -LiteralPath $appProject)) {
    throw 'App project path is missing.'
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
