@echo off
setlocal

set "APP=%~dp0src\StarAI.PracticeClient.App\bin\Release\net8.0-windows\StarAI.PracticeClient.App.exe"

if not exist "%APP%" (
  dotnet build "%~dp0src\StarAI.PracticeClient.App\StarAI.PracticeClient.App.csproj" -c Release
)

start "AIStarClient" "%APP%"
