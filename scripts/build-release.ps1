$ErrorActionPreference = 'Stop'

$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$version = (Get-Content -LiteralPath (Join-Path $repo 'VERSION') -Raw).Trim()
$appProject = Join-Path $repo 'src\StarAI.PracticeClient.App\StarAI.PracticeClient.App.csproj'
$releaseRoot = Join-Path $repo 'artifacts\release'
$publishDir = Join-Path $releaseRoot "publish-$version"
$setupStage = Join-Path $releaseRoot "setup-stage-$version"
$distDir = Join-Path $releaseRoot "dist"
$zipPath = Join-Path $distDir "StarAI-PracticeClient-$version-win-x64.zip"

Remove-Item -LiteralPath $publishDir, $setupStage -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $publishDir, $setupStage, $distDir | Out-Null

dotnet publish $appProject `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $publishDir

Copy-Item -LiteralPath (Join-Path $publishDir 'StarAI.PracticeClient.App.exe') -Destination (Join-Path $setupStage 'StarAI.PracticeClient.App.exe') -Force
Copy-Item -LiteralPath (Join-Path $repo 'VERSION') -Destination (Join-Path $setupStage 'VERSION') -Force
Copy-Item -LiteralPath (Join-Path $repo 'README.md') -Destination (Join-Path $setupStage 'README.md') -Force
$installGuideTemplate = Get-Content -LiteralPath (Join-Path $repo 'docs\INSTALL_GUIDE.md') -Raw -Encoding UTF8
$readmeInstall = $installGuideTemplate.Replace('{{VERSION}}', $version)

$installCmd = @'
@echo off
setlocal

set "TARGET=C:\starai\StarAI.PracticeClient"
set "LAUNCHER=C:\starai\Start-StarAI-PracticeClient.cmd"

echo Starting StarAI Practice Client installation.
echo Install path: %TARGET%
echo.

mkdir "C:\starai" >nul 2>nul
mkdir "%TARGET%" >nul 2>nul

copy /Y "%~dp0StarAI.PracticeClient.App.exe" "%TARGET%\StarAI.PracticeClient.App.exe" >nul
copy /Y "%~dp0VERSION" "%TARGET%\VERSION" >nul
copy /Y "%~dp0README.md" "%TARGET%\README.md" >nul

> "%LAUNCHER%" echo @echo off
>> "%LAUNCHER%" echo start "StarAI Practice Client" "%TARGET%\StarAI.PracticeClient.App.exe"

> "%TARGET%\Start-StarAI-PracticeClient.cmd" echo @echo off
>> "%TARGET%\Start-StarAI-PracticeClient.cmd" echo start "StarAI Practice Client" "%TARGET%\StarAI.PracticeClient.App.exe"

powershell -NoProfile -ExecutionPolicy Bypass -Command "$desktop=[Environment]::GetFolderPath('Desktop'); $shortcut=(New-Object -ComObject WScript.Shell).CreateShortcut((Join-Path $desktop 'StarAI Practice Client.lnk')); $shortcut.TargetPath='%LAUNCHER%'; $shortcut.WorkingDirectory='C:\starai'; $shortcut.Save()" >nul 2>nul

echo.
echo Installation completed.
echo Run the desktop shortcut "StarAI Practice Client" or this file:
echo %LAUNCHER%
echo.

if not exist "C:\Program Files (x86)\SCHNAIL Client" (
  echo [Warning] SCHNAIL Client was not found in the default folder.
  echo           StarAI reads SCHNAIL bot and map data, so SCHNAIL is required.
  echo.
)

if not exist "C:\starai\SC116AI\StarCraft.exe" (
  echo [Warning] C:\starai\SC116AI\StarCraft.exe was not found.
  echo           StarCraft 1.16.1 + BWAPI runtime setup is required.
  echo.
)

start "StarAI Practice Client" "%TARGET%\StarAI.PracticeClient.App.exe"
pause
'@

Set-Content -LiteralPath (Join-Path $setupStage 'install.cmd') -Value $installCmd -Encoding Default
Set-Content -LiteralPath (Join-Path $setupStage 'README-INSTALL.txt') -Value $readmeInstall -Encoding UTF8

Remove-Item -LiteralPath $zipPath -Force -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $setupStage '*') -DestinationPath $zipPath -Force
Get-Item -LiteralPath $zipPath | Select-Object FullName, Length, LastWriteTime
