param(
    [string] $PlayerRuntimeRoot = 'C:\starai\SC116AI',
    [string] $AiRuntimeRoot = 'C:\starai\SC116AI_ai',
    [string] $AppRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path,
    [string] $StarCraftSourceRoot = '',
    [string] $DependencyCacheRoot = (Join-Path $env:LOCALAPPDATA 'StarAI.PracticeClient\deps'),
    [switch] $SkipDownloads,
    [switch] $OpenSetupLinks,
    [switch] $NonInteractive
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$bwapiVersion = 'v4.4.0'
$bwapiUrl = 'https://github.com/bwapi/bwapi/releases/download/v4.4.0/BWAPI.7z'
$cncDdrawVersion = 'v7.1.0.0'
$cncDdrawUrl = 'https://github.com/FunkyFr3sh/cnc-ddraw/releases/download/v7.1.0.0/cnc-ddraw.zip'
$bwapiTmVersion = 'v5.0.4'
$bwapiTmUrl = 'https://github.com/chriscoxe/bwapi-tm/releases/download/v5.0.4/bwapi-tm-bin-v5.0.4.zip'
$starCraftGuideUrl = 'https://github.com/NeoMindStd/SPArring#starcraft-1161-%EC%A4%80%EB%B9%84'

function Convert-FullPath([string] $Path) {
    return [IO.Path]::GetFullPath($Path)
}

function Assert-SafeChildPath([string] $Path, [string] $AllowedRoot) {
    $fullPath = Convert-FullPath $Path
    $fullRoot = Convert-FullPath $AllowedRoot
    if (-not $fullPath.StartsWith($fullRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify path outside expected root: $fullPath"
    }
}

function Reset-Directory([string] $Path, [string] $AllowedRoot) {
    Assert-SafeChildPath $Path $AllowedRoot
    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $Path | Out-Null
}

function Copy-Tree([string] $Source, [string] $Destination, [string[]] $ExtraArguments = @()) {
    if (-not (Test-Path -LiteralPath $Source -PathType Container)) {
        throw "Source directory not found: $Source"
    }

    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    $arguments = @($Source, $Destination, '/E', '/R:2', '/W:1', '/NFL', '/NDL', '/NJH', '/NJS', '/NP') + $ExtraArguments
    & robocopy @arguments | Out-Null
    if ($LASTEXITCODE -ge 8) {
        throw "robocopy failed from '$Source' to '$Destination' with exit code $LASTEXITCODE"
    }
}

function Invoke-Download([string] $Url, [string] $Destination) {
    if (Test-Path -LiteralPath $Destination) {
        return
    }

    if ($SkipDownloads) {
        throw "Required dependency archive was not found and -SkipDownloads was set: $Destination"
    }

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Destination) | Out-Null
    Write-Host "Downloading $Url"
    Invoke-WebRequest -Uri $Url -OutFile $Destination -UseBasicParsing
}

function Expand-TarArchive([string] $ArchivePath, [string] $ExtractRoot, [string] $ExpectedRelativePath) {
    $expectedPath = Join-Path $ExtractRoot $ExpectedRelativePath
    if (Test-Path -LiteralPath $expectedPath) {
        return
    }

    Reset-Directory $ExtractRoot $DependencyCacheRoot
    & tar -xf $ArchivePath -C $ExtractRoot
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to extract archive: $ArchivePath"
    }

    if (-not (Test-Path -LiteralPath $expectedPath)) {
        throw "Expected file was not found after extraction: $expectedPath"
    }
}

function Expand-ZipArchive([string] $ArchivePath, [string] $ExtractRoot, [string] $ExpectedFileName) {
    $existing = Get-ChildItem -LiteralPath $ExtractRoot -Recurse -Filter $ExpectedFileName -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($existing) {
        return
    }

    Reset-Directory $ExtractRoot $DependencyCacheRoot
    Expand-Archive -LiteralPath $ArchivePath -DestinationPath $ExtractRoot -Force
    $expanded = Get-ChildItem -LiteralPath $ExtractRoot -Recurse -Filter $ExpectedFileName -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $expanded) {
        throw "Expected file '$ExpectedFileName' was not found after extracting: $ArchivePath"
    }
}

function Test-StarCraftRoot([string] $Root) {
    if ([string]::IsNullOrWhiteSpace($Root)) {
        return $false
    }

    foreach ($relative in @('StarCraft.exe', 'stardat.mpq', 'broodat.mpq', 'patch_rt.mpq')) {
        if (-not (Test-Path -LiteralPath (Join-Path $Root $relative))) {
            return $false
        }
    }

    return $true
}

function Resolve-StarCraftSource {
    if (Test-StarCraftRoot $PlayerRuntimeRoot) {
        return $null
    }

    if (-not [string]::IsNullOrWhiteSpace($StarCraftSourceRoot) -and (Test-StarCraftRoot $StarCraftSourceRoot)) {
        return (Convert-FullPath $StarCraftSourceRoot)
    }

    if ($OpenSetupLinks) {
        Start-Process $starCraftGuideUrl
    }

    if ($NonInteractive) {
        throw "StarCraft 1.16.1 source folder is required. Provide -StarCraftSourceRoot. Guide: $starCraftGuideUrl"
    }

    Write-Host ""
    Write-Host "StarCraft 1.16.1 game files were not found at $PlayerRuntimeRoot"
    Write-Host "Enter a folder that contains StarCraft.exe, stardat.mpq, broodat.mpq, and patch_rt.mpq."
    Write-Host "The source folder will be copied; it will not be modified."
    $entered = Read-Host "StarCraft 1.16.1 folder"
    if ([string]::IsNullOrWhiteSpace($entered) -or -not (Test-StarCraftRoot $entered)) {
        throw "A valid StarCraft 1.16.1 folder was not provided. Setup guide: $starCraftGuideUrl"
    }

    return (Convert-FullPath $entered)
}

function Copy-StarCraftRuntime([string] $SourceRoot) {
    if ([string]::IsNullOrWhiteSpace($SourceRoot)) {
        return
    }

    $source = Convert-FullPath $SourceRoot
    $destination = Convert-FullPath $PlayerRuntimeRoot
    if ($source.Equals($destination, [StringComparison]::OrdinalIgnoreCase)) {
        return
    }

    Write-Host "Copying StarCraft 1.16.1 runtime into $destination"
    Copy-Tree $source $destination @('/XD', 'Errors', 'logs', 'write', 'replays', 'Replays', '/XF', '*.rep', '*.log')
}

function Install-Bwapi {
    $cache = Join-Path $DependencyCacheRoot "bwapi\$bwapiVersion"
    $archive = Join-Path $cache 'BWAPI.7z'
    $extract = Join-Path $cache 'extracted'
    Invoke-Download $bwapiUrl $archive
    Expand-TarArchive $archive $extract 'Release_Binary\Starcraft\bwapi-data\BWAPI.dll'

    Copy-Tree (Join-Path $extract 'Release_Binary\Starcraft') $PlayerRuntimeRoot
    Copy-Tree (Join-Path $extract 'Release_Binary\Chaoslauncher') $PlayerRuntimeRoot
}

function Install-CncDdraw {
    $cache = Join-Path $DependencyCacheRoot "cnc-ddraw\$cncDdrawVersion"
    $archive = Join-Path $cache 'cnc-ddraw.zip'
    $extract = Join-Path $cache 'extracted'
    Invoke-Download $cncDdrawUrl $archive
    Expand-ZipArchive $archive $extract 'ddraw.dll'

    $ddraw = Get-ChildItem -LiteralPath $extract -Recurse -Filter 'ddraw.dll' | Select-Object -First 1
    Copy-Tree $ddraw.Directory.FullName $PlayerRuntimeRoot

    $appExtract = Join-Path $AppRoot "artifacts\deps\cnc-ddraw\$cncDdrawVersion\extracted"
    Copy-Tree $ddraw.Directory.FullName $appExtract
}

function Install-TournamentModule {
    $cache = Join-Path $DependencyCacheRoot "bwapi-tm\$bwapiTmVersion"
    $archive = Join-Path $cache 'bwapi-tm-bin-v5.0.4.zip'
    $extract = Join-Path $cache 'extracted'
    Invoke-Download $bwapiTmUrl $archive
    Expand-ZipArchive $archive $extract 'TournamentModule.dll'

    $tm = Get-ChildItem -LiteralPath $extract -Recurse -Filter 'TournamentModule.dll' |
        Where-Object { $_.FullName -match 'BWAPI_440' } |
        Select-Object -First 1
    if (-not $tm) {
        throw "BWAPI 4.4.0 TournamentModule.dll was not found in $archive"
    }

    $target = Join-Path $PlayerRuntimeRoot 'bwapi-data\TM\TournamentModule.dll'
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
    Copy-Item -LiteralPath $tm.FullName -Destination $target -Force
}

function Disable-OptionalChaosPlugin([string] $RuntimeRoot, [string] $PluginFileName) {
    $activePath = Join-Path $RuntimeRoot "Plugins\$PluginFileName"
    $disabledPath = "$activePath.starai-disabled"
    if (-not (Test-Path -LiteralPath $activePath)) {
        return
    }

    if (Test-Path -LiteralPath $disabledPath) {
        Remove-Item -LiteralPath $disabledPath -Force
    }

    Move-Item -LiteralPath $activePath -Destination $disabledPath -Force
}

function Seed-AiRuntime {
    Write-Host "Preparing AI runtime at $AiRuntimeRoot"
    Copy-Tree $PlayerRuntimeRoot $AiRuntimeRoot @('/XD', 'Errors', 'logs', 'write', 'replays', 'Replays', '/XF', '*.rep', '*.log')
}

function Assert-RequiredFile([string] $Root, [string] $RelativePath) {
    $path = Join-Path $Root $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required runtime file is missing: $path"
    }
}

function Test-Runtime {
    foreach ($root in @($PlayerRuntimeRoot, $AiRuntimeRoot)) {
        Assert-RequiredFile $root 'StarCraft.exe'
        Assert-RequiredFile $root 'Chaoslauncher - MultiInstance.exe'
        Assert-RequiredFile $root 'Plugins\BWAPI_PluginInjector.bwl'
        Assert-RequiredFile $root 'bwapi-data\BWAPI.dll'
        Assert-RequiredFile $root 'bwapi-data\bwapi.ini'
        Assert-RequiredFile $root 'bwapi-data\TM\TournamentModule.dll'
        Assert-RequiredFile $root 'ddraw.dll'
    }
}

$PlayerRuntimeRoot = Convert-FullPath $PlayerRuntimeRoot
$AiRuntimeRoot = Convert-FullPath $AiRuntimeRoot
$AppRoot = Convert-FullPath $AppRoot
$DependencyCacheRoot = Convert-FullPath $DependencyCacheRoot

New-Item -ItemType Directory -Force -Path $DependencyCacheRoot | Out-Null
$starCraftSource = Resolve-StarCraftSource
Copy-StarCraftRuntime $starCraftSource
Install-Bwapi
Install-TournamentModule
Install-CncDdraw
Disable-OptionalChaosPlugin $PlayerRuntimeRoot 'wmode.bwl'
Disable-OptionalChaosPlugin $PlayerRuntimeRoot 'APMAlert.bwl'
Seed-AiRuntime
Disable-OptionalChaosPlugin $AiRuntimeRoot 'wmode.bwl'
Disable-OptionalChaosPlugin $AiRuntimeRoot 'APMAlert.bwl'
Test-Runtime

Write-Host ""
Write-Host "StarAI runtime setup completed."
Write-Host "Player runtime: $PlayerRuntimeRoot"
Write-Host "AI runtime: $AiRuntimeRoot"
Write-Host "App root: $AppRoot"
