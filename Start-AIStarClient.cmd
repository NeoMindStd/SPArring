@echo off
setlocal

set /p VERSION=<"%~dp0VERSION"
set "APPDIR=%~dp0artifacts\run\AIStarClient-%VERSION%"
set "BUILDDIR=%~dp0artifacts\run-build\AIStarClient-%VERSION%"
set "APP=%APPDIR%\StarAI.PracticeClient.App.exe"

if not exist "%APP%" (
  dotnet publish "%~dp0src\StarAI.PracticeClient.App\StarAI.PracticeClient.App.csproj" -c Release -o "%APPDIR%" -p:OutputPath="%BUILDDIR%\"
  if errorlevel 1 exit /b %errorlevel%
)

start "AIStarClient" "%APP%"
