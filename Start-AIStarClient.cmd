@echo off
setlocal

set /p VERSION=<"%~dp0VERSION"
set "APPDIR=%~dp0artifacts\run\AIStarClient-%VERSION%"
set "BUILDDIR=%~dp0artifacts\run-build\AIStarClient-%VERSION%"
set "APP=%APPDIR%\StarAI.PracticeClient.App.exe"

taskkill /IM StarAI.PracticeClient.App.exe /F >nul 2>nul

dotnet publish "%~dp0src\StarAI.PracticeClient.App\StarAI.PracticeClient.App.csproj" -c Release -o "%APPDIR%" -p:OutputPath="%BUILDDIR%\"
if errorlevel 1 exit /b %errorlevel%

start "AIStarClient" "%APP%"
