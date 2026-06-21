$ErrorActionPreference = 'Stop'

$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$version = (Get-Content -LiteralPath (Join-Path $repo 'VERSION') -Raw).Trim()
$appProject = Join-Path $repo 'src\StarAI.PracticeClient.App\StarAI.PracticeClient.App.csproj'
$setupProject = Join-Path $repo 'src\StarAI.PracticeClient.Setup\StarAI.PracticeClient.Setup.csproj'
$releaseRoot = Join-Path $repo 'artifacts\release'
$publishDir = Join-Path $releaseRoot "publish-app-$version"
$setupPublishDir = Join-Path $releaseRoot "publish-setup-$version"
$payloadStage = Join-Path $releaseRoot "payload-stage-$version"
$payloadZip = Join-Path $releaseRoot "payload-$version.zip"
$distDir = Join-Path $releaseRoot "dist"
$zipPath = Join-Path $distDir "StarAI-PracticeClient-$version-win-x64.zip"
$setupExePath = Join-Path $distDir "StarAI-PracticeClient-$version-setup.exe"
$dataRoot = Join-Path $repo 'data'

if (-not (Test-Path -LiteralPath (Join-Path $dataRoot 'bots\bots.dat')) -or
    -not (Test-Path -LiteralPath (Join-Path $dataRoot 'maps\maps.dat'))) {
    throw "StarAI bundled assets were not found. Run .\scripts\import-schnail-assets.ps1 before building a release package."
}

if (-not (Test-Path -LiteralPath (Join-Path $repo 'scripts\setup-runtime.ps1'))) {
    throw "Runtime setup script is missing: scripts\setup-runtime.ps1"
}

Remove-Item -LiteralPath $publishDir, $setupPublishDir, $payloadStage, $payloadZip -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $publishDir, $setupPublishDir, $payloadStage, $distDir | Out-Null

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

Copy-Item -LiteralPath (Join-Path $publishDir 'StarAI.PracticeClient.App.exe') -Destination (Join-Path $payloadStage 'StarAI.PracticeClient.App.exe') -Force
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

$installCmd = @'
@echo off
setlocal

set "TARGET=C:\starai\StarAI.PracticeClient"
set "LAUNCHER=C:\starai\Start-StarAI-PracticeClient.cmd"

echo StarAI Practice Client legacy ZIP installer
echo.
echo The recommended installer is StarAI-PracticeClient-*-setup.exe.
echo This fallback script keeps the old ZIP workflow available.
echo.

if not exist "%~dp0StarAI.PracticeClient.App.exe" (
  echo [Error] StarAI.PracticeClient.App.exe was not found next to install.cmd.
  pause
  exit /b 1
)

if not exist "%~dp0data\bots\bots.dat" (
  echo [Error] StarAI bundled bot data was not found in this ZIP package.
  pause
  exit /b 1
)

set /P "SCROOT=StarCraft 1.16.1 folder containing StarCraft.exe: "
if not exist "%SCROOT%\StarCraft.exe" (
  echo [Error] StarCraft.exe was not found in "%SCROOT%".
  pause
  exit /b 1
)

mkdir "C:\starai" >nul 2>nul
mkdir "%TARGET%" >nul 2>nul

copy /Y "%~dp0StarAI.PracticeClient.App.exe" "%TARGET%\StarAI.PracticeClient.App.exe" >nul
copy /Y "%~dp0VERSION" "%TARGET%\VERSION" >nul
copy /Y "%~dp0README.md" "%TARGET%\README.md" >nul
mkdir "%TARGET%\scripts" >nul 2>nul
copy /Y "%~dp0scripts\setup-runtime.ps1" "%TARGET%\scripts\setup-runtime.ps1" >nul

robocopy "%~dp0data" "%TARGET%\data" /E /R:2 /W:1 >nul
if %ERRORLEVEL% GEQ 8 (
  echo [Error] Failed to copy StarAI bundled data.
  pause
  exit /b 1
)

> "%LAUNCHER%" echo @echo off
>> "%LAUNCHER%" echo start "StarAI Practice Client" "%TARGET%\StarAI.PracticeClient.App.exe"

> "%TARGET%\Start-StarAI-PracticeClient.cmd" echo @echo off
>> "%TARGET%\Start-StarAI-PracticeClient.cmd" echo start "StarAI Practice Client" "%TARGET%\StarAI.PracticeClient.App.exe"

powershell -NoProfile -ExecutionPolicy Bypass -Command "$desktop=[Environment]::GetFolderPath('Desktop'); $shortcut=(New-Object -ComObject WScript.Shell).CreateShortcut((Join-Path $desktop 'StarAI Practice Client.lnk')); $shortcut.TargetPath='%TARGET%\StarAI.PracticeClient.App.exe'; $shortcut.WorkingDirectory='%TARGET%'; $shortcut.Save()" >nul 2>nul
powershell -NoProfile -ExecutionPolicy Bypass -File "%TARGET%\scripts\setup-runtime.ps1" -AppRoot "%TARGET%" -StarCraftSourceRoot "%SCROOT%" -NonInteractive
if %ERRORLEVEL% NEQ 0 (
  echo [Error] StarCraft/BWAPI runtime setup failed.
  pause
  exit /b 1
)

echo.
echo Installation completed.
echo Run the desktop shortcut "StarAI Practice Client" or:
echo %LAUNCHER%
echo.

start "StarAI Practice Client" "%TARGET%\StarAI.PracticeClient.App.exe"
pause
'@

Set-Content -LiteralPath (Join-Path $payloadStage 'install.cmd') -Value $installCmd -Encoding Default

Remove-Item -LiteralPath $zipPath, $setupExePath -Force -ErrorAction SilentlyContinue
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
    "-p:PayloadZipPath=$payloadZip" `
    -o $setupPublishDir

Copy-Item -LiteralPath (Join-Path $setupPublishDir 'StarAI.PracticeClient.Setup.exe') -Destination $setupExePath -Force
Get-Item -LiteralPath $setupExePath, $zipPath | Select-Object FullName, Length, LastWriteTime
