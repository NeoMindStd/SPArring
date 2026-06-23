$ErrorActionPreference = 'Stop'

$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$solution = Join-Path $repo 'Sparring.sln'
$version = Join-Path $repo 'VERSION'
$appProject = Join-Path $repo 'src\Sparring.Client\Sparring.Client.csproj'
$setupProject = Join-Path $repo 'src\Sparring.Setup\Sparring.Setup.csproj'
$screenshotRoot = Join-Path $repo 'artifacts\screenshots'

if (-not (Test-Path -LiteralPath $version)) {
    throw 'VERSION file is required.'
}

if (-not (Test-Path -LiteralPath $appProject)) {
    throw 'App project path is missing.'
}

if (-not (Test-Path -LiteralPath $setupProject)) {
    throw 'Setup project path is missing.'
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

$setupForm = Join-Path $repo 'src\Sparring.Setup\SetupForm.cs'
if (Select-String -LiteralPath $setupForm -Pattern 'ProgressBarStyle.Marquee|SetBusy' -Quiet) {
    throw 'Setup progress must use determinate progress, not marquee/busy animation.'
}

$buildRelease = Join-Path $repo 'scripts\build-release.ps1'
if (Select-String -LiteralPath $buildRelease -Pattern 'checksums\.txt' -Quiet) {
    throw 'Release build must not generate a public checksum text asset.'
}

if (Test-Path -LiteralPath (Join-Path $repo 'scripts\update-github-release-checksums.ps1')) {
    throw 'Release checksum updater script must not be restored.'
}

Invoke-NativeChecked -Name 'dotnet build' -Command { dotnet build $solution -c Release --nologo }
Invoke-NativeChecked -Name 'launcher smoke' -Command { dotnet run --project $appProject -c Release -- --smoke }

New-Item -ItemType Directory -Force -Path $screenshotRoot | Out-Null
$setupDll = Join-Path $repo 'src\Sparring.Setup\bin\Release\net8.0-windows\Sparring.Setup.dll'
if (-not (Test-Path -LiteralPath $setupDll)) {
    throw "Setup smoke target is missing: $setupDll"
}
Invoke-NativeChecked -Name 'setup UI smoke' -Command {
    dotnet $setupDll --ui-smoke (Join-Path $screenshotRoot 'setup-ui-default.png') --validate
}
Invoke-NativeChecked -Name 'setup UI large-font smoke' -Command {
    dotnet $setupDll --ui-smoke (Join-Path $screenshotRoot 'setup-ui-large-font.png') --font-size 15 --validate
}
Invoke-NativeChecked -Name 'setup UI extra-large-font smoke' -Command {
    dotnet $setupDll --ui-smoke (Join-Path $screenshotRoot 'setup-ui-extra-large-font.png') --font-size 18 --validate
}
