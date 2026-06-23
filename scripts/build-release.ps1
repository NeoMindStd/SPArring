$ErrorActionPreference = 'Stop'

$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$version = (Get-Content -LiteralPath (Join-Path $repo 'VERSION') -Raw).Trim()
$appProject = Join-Path $repo 'src\Sparring.Client\Sparring.Client.csproj'
$setupProject = Join-Path $repo 'src\Sparring.Setup\Sparring.Setup.csproj'
$releaseRoot = Join-Path $repo 'artifacts\release'
$publishDir = Join-Path $releaseRoot "publish-app-$version"
$setupPublishDir = Join-Path $releaseRoot "publish-setup-$version"
$setupExternalPublishDir = Join-Path $releaseRoot "publish-setup-external-$version"
$payloadStage = Join-Path $releaseRoot "payload-stage-$version"
$externalSetupStage = Join-Path $releaseRoot "setup-folder-stage-$version"
$payloadZip = Join-Path $releaseRoot "payload-$version.zip"
$distDir = Join-Path $releaseRoot "dist"
$zipPath = Join-Path $distDir "Sparring-$version-win-x64.zip"
$setupExePath = Join-Path $distDir "Sparring-$version-setup.exe"
$setupFolderZipPath = Join-Path $distDir "Sparring-$version-setup-folder.zip"
$dataRoot = Join-Path $repo 'data'
$versionParts = $version.Split('.')
while ($versionParts.Count -lt 4) {
    $versionParts += '0'
}

$assemblyVersion = ($versionParts[0..3] -join '.')
$versionProperties = @(
    "-p:Version=$version",
    "-p:AssemblyVersion=$assemblyVersion",
    "-p:FileVersion=$assemblyVersion",
    "-p:InformationalVersion=$version"
)

if (-not (Test-Path -LiteralPath (Join-Path $dataRoot 'bots\bots.dat')) -or
    -not (Test-Path -LiteralPath (Join-Path $dataRoot 'maps\maps.dat'))) {
    throw "Sparring bundled assets were not found. Run .\scripts\import-schnail-assets.ps1 before building a release package."
}

if (-not (Test-Path -LiteralPath (Join-Path $repo 'scripts\setup-runtime.ps1'))) {
    throw "Runtime setup script is missing: scripts\setup-runtime.ps1"
}

function Convert-ToRelativePath([string] $Root, [string] $Path) {
    $rootFullPath = [IO.Path]::GetFullPath($Root)
    if (-not $rootFullPath.EndsWith([IO.Path]::DirectorySeparatorChar)) {
        $rootFullPath += [IO.Path]::DirectorySeparatorChar
    }

    $pathFullPath = [IO.Path]::GetFullPath($Path)
    $rootUri = [Uri]$rootFullPath
    $pathUri = [Uri]$pathFullPath
    return [Uri]::UnescapeDataString($rootUri.MakeRelativeUri($pathUri).ToString()).Replace('/', [IO.Path]::DirectorySeparatorChar)
}

Remove-Item -LiteralPath $publishDir, $setupPublishDir, $setupExternalPublishDir, $payloadStage, $externalSetupStage, $payloadZip -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $publishDir, $setupPublishDir, $setupExternalPublishDir, $payloadStage, $externalSetupStage, $distDir | Out-Null

dotnet publish $appProject `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    $versionProperties `
    -o $publishDir

Copy-Item -LiteralPath (Join-Path $publishDir 'Sparring.Client.exe') -Destination (Join-Path $payloadStage 'Sparring.Client.exe') -Force
Copy-Item -LiteralPath (Join-Path $repo 'VERSION') -Destination (Join-Path $payloadStage 'VERSION') -Force
Copy-Item -LiteralPath (Join-Path $repo 'README.md') -Destination (Join-Path $payloadStage 'README.md') -Force
New-Item -ItemType Directory -Force -Path (Join-Path $payloadStage 'scripts') | Out-Null
Copy-Item -LiteralPath (Join-Path $repo 'scripts\setup-runtime.ps1') -Destination (Join-Path $payloadStage 'scripts\setup-runtime.ps1') -Force

& robocopy $dataRoot (Join-Path $payloadStage 'data') /E /R:2 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -gt 7) {
    throw "Failed to copy bundled data into release stage. robocopy exit code: $LASTEXITCODE"
}

$installGuideTemplate = Get-Content -LiteralPath (Join-Path $repo 'docs\INSTALL_GUIDE.md') -Raw -Encoding UTF8
$readmeInstall = $installGuideTemplate.Replace('{{VERSION}}', $version)
Set-Content -LiteralPath (Join-Path $payloadStage 'README-INSTALL.txt') -Value $readmeInstall -Encoding UTF8

$manifestPath = Join-Path $payloadStage 'install-manifest.json'
$manifestEntries = Get-ChildItem -LiteralPath $payloadStage -Recurse -File |
    ForEach-Object {
        $relativePath = Convert-ToRelativePath $payloadStage $_.FullName
        if (-not (
            $relativePath -ieq 'install-manifest.json' -or
            $relativePath -ieq 'install-state.json' -or
            $relativePath.StartsWith("install-cache$([IO.Path]::DirectorySeparatorChar)", [StringComparison]::OrdinalIgnoreCase))) {
            [pscustomobject]@{
                RelativePath = $relativePath
                Sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
            }
        }
    } |
    Sort-Object RelativePath
@($manifestEntries) | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

Remove-Item -LiteralPath $zipPath, $setupExePath, $setupFolderZipPath -Force -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $payloadStage '*') -DestinationPath $payloadZip -Force
Compress-Archive -Path (Join-Path $payloadStage '*') -DestinationPath $zipPath -Force

dotnet publish $setupProject `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    $versionProperties `
    "-p:PayloadZipPath=$payloadZip" `
    -o $setupPublishDir

Copy-Item -LiteralPath (Join-Path $setupPublishDir 'Sparring.Setup.exe') -Destination $setupExePath -Force

$setupObjRoot = Join-Path $repo 'src\Sparring.Setup\obj\Release'
Remove-Item -LiteralPath $setupObjRoot -Recurse -Force -ErrorAction SilentlyContinue
dotnet publish $setupProject `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    $versionProperties `
    -o $setupExternalPublishDir

Copy-Item -LiteralPath (Join-Path $setupExternalPublishDir 'Sparring.Setup.exe') -Destination (Join-Path $externalSetupStage "Sparring-$version-setup.exe") -Force
Copy-Item -LiteralPath $payloadZip -Destination (Join-Path $externalSetupStage 'payload.zip') -Force
Copy-Item -LiteralPath (Join-Path $payloadStage 'README-INSTALL.txt') -Destination (Join-Path $externalSetupStage 'README-INSTALL.txt') -Force
Compress-Archive -Path (Join-Path $externalSetupStage '*') -DestinationPath $setupFolderZipPath -Force

Get-Item -LiteralPath $setupExePath, $zipPath, $setupFolderZipPath | Select-Object FullName, Length, LastWriteTime
