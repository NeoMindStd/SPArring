param(
    [string] $BwapiSdkRoot = '',
    [switch] $SkipDownloads
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$neoBots = @(
    [pscustomobject]@{
        Name = 'NeoProtossF'
        Project = Join-Path $repo 'src\Sparring.Bots\NeoProtossF\NeoProtossF.vcxproj'
        Output = Join-Path $repo 'src\Sparring.Bots\NeoProtossF\Release\NeoProtossF.dll'
        Destination = Join-Path $repo 'data\bots\NeoProtossF\NeoProtossF.dll'
    },
    [pscustomobject]@{
        Name = 'NeoTerranF'
        Project = Join-Path $repo 'src\Sparring.Bots\NeoTerranF\NeoTerranF.vcxproj'
        Output = Join-Path $repo 'src\Sparring.Bots\NeoTerranF\Release\NeoTerranF.dll'
        Destination = Join-Path $repo 'data\bots\NeoTerranF\NeoTerranF.dll'
    },
    [pscustomobject]@{
        Name = 'NeoZergF'
        Project = Join-Path $repo 'src\Sparring.Bots\NeoZergF\NeoZergF.vcxproj'
        Output = Join-Path $repo 'src\Sparring.Bots\NeoZergF\Release\NeoZergF.dll'
        Destination = Join-Path $repo 'data\bots\NeoZergF\NeoZergF.dll'
    },
    [pscustomobject]@{
        Name = 'NeoProtossE'
        Project = Join-Path $repo 'src\Sparring.Bots\NeoProtossE\NeoProtossE.vcxproj'
        Output = Join-Path $repo 'src\Sparring.Bots\NeoProtossE\Release\NeoProtossE.dll'
        Destination = Join-Path $repo 'data\bots\NeoProtossE\NeoProtossE.dll'
    },
    [pscustomobject]@{
        Name = 'NeoTerranE'
        Project = Join-Path $repo 'src\Sparring.Bots\NeoTerranE\NeoTerranE.vcxproj'
        Output = Join-Path $repo 'src\Sparring.Bots\NeoTerranE\Release\NeoTerranE.dll'
        Destination = Join-Path $repo 'data\bots\NeoTerranE\NeoTerranE.dll'
    },
    [pscustomobject]@{
        Name = 'NeoZergE'
        Project = Join-Path $repo 'src\Sparring.Bots\NeoZergE\NeoZergE.vcxproj'
        Output = Join-Path $repo 'src\Sparring.Bots\NeoZergE\Release\NeoZergE.dll'
        Destination = Join-Path $repo 'data\bots\NeoZergE\NeoZergE.dll'
    }
)
$bwapiUrl = 'https://github.com/bwapi/bwapi/releases/download/v4.4.0/BWAPI.7z'

function Test-BwapiSdk([string] $Root) {
    return -not [string]::IsNullOrWhiteSpace($Root) -and
        (Test-Path -LiteralPath (Join-Path $Root 'include\BWAPI.h')) -and
        (Test-Path -LiteralPath (Join-Path $Root 'BWAPILIB\BWAPILIB.vcxproj'))
}

function Resolve-BwapiSdk {
    if (Test-BwapiSdk $BwapiSdkRoot) {
        return (Resolve-Path -LiteralPath $BwapiSdkRoot).Path
    }

    $candidates = @(
        (Join-Path $repo 'artifacts\setup-runtime-smoke\deps\bwapi\v4.4.0\extracted\Release_Binary'),
        (Join-Path $repo 'artifacts\deps\bwapi\v4.4.0\extracted\Release_Binary'),
        (Join-Path $env:LOCALAPPDATA 'Sparring\deps\bwapi\v4.4.0\extracted\Release_Binary')
    )

    foreach ($candidate in $candidates) {
        if (Test-BwapiSdk $candidate) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    if ($SkipDownloads) {
        throw "BWAPI SDK was not found and -SkipDownloads was set. Expected a Release_Binary folder with include\BWAPI.h and BWAPILIB\BWAPILIB.vcxproj."
    }

    $cacheRoot = Join-Path $repo 'artifacts\deps\bwapi\v4.4.0'
    $archive = Join-Path $cacheRoot 'BWAPI.7z'
    $extract = Join-Path $cacheRoot 'extracted'
    New-Item -ItemType Directory -Force -Path $cacheRoot | Out-Null

    if (-not (Test-Path -LiteralPath $archive)) {
        Write-Host "Downloading BWAPI SDK: $bwapiUrl"
        Invoke-WebRequest -Uri $bwapiUrl -OutFile $archive -UseBasicParsing
    }

    if (-not (Test-BwapiSdk (Join-Path $extract 'Release_Binary'))) {
        New-Item -ItemType Directory -Force -Path $extract | Out-Null
        & tar -xf $archive -C $extract
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to extract BWAPI SDK archive: $archive"
        }
    }

    $sdk = Join-Path $extract 'Release_Binary'
    if (-not (Test-BwapiSdk $sdk)) {
        throw "BWAPI SDK extraction did not contain the expected Release_Binary layout: $sdk"
    }

    return (Resolve-Path -LiteralPath $sdk).Path
}

function Use-ExistingNeoBotBinaries([string] $Reason) {
    $missing = @($neoBots | Where-Object { -not (Test-Path -LiteralPath $_.Destination) })
    if ($missing.Count -gt 0) {
        $names = ($missing | ForEach-Object { $_.Name }) -join ', '
        throw "$Reason Existing Neo bot binaries were not found for: $names"
    }

    $stale = @($neoBots | Where-Object {
        $bot = $_
        $sourceRoot = Split-Path -Parent $bot.Project
        $latestSource = Get-ChildItem -LiteralPath $sourceRoot -File |
            Where-Object { $_.Extension -in '.cpp', '.h', '.vcxproj' } |
            Sort-Object LastWriteTimeUtc -Descending |
            Select-Object -First 1

        $sourceGitStatus = @()
        $binaryGitStatus = @()
        if (Test-Path -LiteralPath (Join-Path $repo '.git')) {
            $sourceGitStatus = @(& git -C $repo status --porcelain -- $sourceRoot)
            $binaryGitStatus = @(& git -C $repo status --porcelain -- $bot.Destination)
        }

        ($sourceGitStatus.Count -gt 0 -and $binaryGitStatus.Count -eq 0) -or
            ($latestSource -and ((Get-Item -LiteralPath $bot.Destination).LastWriteTimeUtc -lt $latestSource.LastWriteTimeUtc))
    })

    if ($stale.Count -gt 0) {
        $names = ($stale | ForEach-Object { $_.Name }) -join ', '
        throw "$Reason Existing Neo bot binaries are older than edited source files for: $names. Install MSVC Build Tools or copy freshly built MSVC binaries before packaging."
    }

    Write-Warning "$Reason Reusing existing bundled Neo bot binaries."
    foreach ($bot in $neoBots) {
        Get-Item -LiteralPath $bot.Destination | Select-Object FullName, Length, LastWriteTime
    }
}

$sdkRoot = Resolve-BwapiSdk
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path -LiteralPath $vswhere)) {
    Use-ExistingNeoBotBinaries "Visual Studio vswhere.exe was not found."
    return
}

$vs = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
if (-not $vs) {
    Use-ExistingNeoBotBinaries "Visual Studio Build Tools with MSBuild were not found."
    return
}

$env:BWAPI_SDK_ROOT = $sdkRoot
foreach ($bot in $neoBots) {
    $command = '"{0}\Common7\Tools\VsDevCmd.bat" -arch=x86 >nul && msbuild "{1}" /p:Configuration=Release /p:Platform=Win32 /p:PlatformToolset=v143 /m /v:minimal' -f $vs, $bot.Project
    cmd.exe /d /s /c $command
    if ($LASTEXITCODE -ne 0) {
        throw "$($bot.Name) native build failed."
    }

    if (-not (Test-Path -LiteralPath $bot.Output)) {
        throw "$($bot.Name).dll was not produced: $($bot.Output)"
    }

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $bot.Destination) | Out-Null
    Copy-Item -LiteralPath $bot.Output -Destination $bot.Destination -Force
    Get-Item -LiteralPath $bot.Destination | Select-Object FullName, Length, LastWriteTime
}
