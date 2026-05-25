param(
    [string]$Root = "C:\starai\SC116AI",
    [int]$TimeoutSeconds = 60,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$Version = (Get-Content -LiteralPath (Join-Path $RepoRoot "VERSION") -Raw).Trim()
$AppDir = Join-Path $RepoRoot "artifacts\run\AIStarClient-$Version"
$BuildDir = Join-Path $RepoRoot "artifacts\run-build\AIStarClient-$Version"
$App = Join-Path $AppDir "StarAI.PracticeClient.App.exe"
$Log = Join-Path $Root "Chaoslauncher - MultiInstance.log"
$Ini = Join-Path $Root "bwapi-data\bwapi.ini"
$Preferences = Join-Path $env:APPDATA "AIStarClient\preferences.json"
$PreferencesBackup = "$Preferences.starai-smoke.bak"
$SafeBot = "bwapi-data\AI\practice-bots\NiteKatT\ExampleAIModule.dll"
$SafeMap = "maps\(4)Fighting Spirit.scx"
$StartButtonText = -join ([char[]](0xC2A4, 0xD30C, 0xB9C1, 0x20, 0xC2DC, 0xC791))

function Invoke-Native {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $FilePath $($Arguments -join ' ')"
    }
}

function Stop-LocalRuntime {
    Get-CimInstance Win32_Process | Where-Object {
        ($_.Name -eq "StarAI.PracticeClient.App.exe" -and $_.ExecutablePath -like "C:\starai\*") -or
        ($_.Name -eq "Chaoslauncher - MultiInstance.exe" -and $_.ExecutablePath -like "C:\starai\*") -or
        ($_.Name -eq "StarCraft.exe" -and ($_.ExecutablePath -like "C:\starai\*" -or [string]::IsNullOrWhiteSpace($_.ExecutablePath)))
    } | ForEach-Object {
        Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
    }
}

function Get-CompletedStartCount {
    if (-not (Test-Path -LiteralPath $Log)) {
        return 0
    }

    return @((Get-Content -LiteralPath $Log) | Where-Object { $_ -like "*Starting Starcraft completed*" }).Count
}

function Wait-CompletedStarts {
    param([int]$Count)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        if ((Get-CompletedStartCount) -ge $Count) {
            return
        }

        Start-Sleep -Milliseconds 500
    } while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for $Count completed StarCraft starts. Actual: $(Get-CompletedStartCount)."
}

function Add-UiClickTypes {
    Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class StarAiAppSmokeWin32 {
 public delegate bool EnumProc(IntPtr h, IntPtr l);
 [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
 [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr p, EnumProc cb, IntPtr l);
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out int processId);
 [DllImport("user32.dll", CharSet=CharSet.Auto)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
 [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);
 public static string Txt(IntPtr h){ var sb=new StringBuilder(512); GetWindowText(h,sb,sb.Capacity); return sb.ToString(); }
}
'@ -ErrorAction SilentlyContinue
}

function Invoke-SparringStartButton {
    param([int]$ProcessId)

    $deadline = (Get-Date).AddSeconds(20)
    do {
        $script:ClickedStarAiButton = $false
        [StarAiAppSmokeWin32]::EnumWindows({
            param($window, $unused)

            $candidateProcessId = 0
            [StarAiAppSmokeWin32]::GetWindowThreadProcessId($window, [ref]$candidateProcessId) | Out-Null
            if ($candidateProcessId -ne $ProcessId -or -not [StarAiAppSmokeWin32]::IsWindowVisible($window)) {
                return $true
            }

            [StarAiAppSmokeWin32]::EnumChildWindows($window, {
                param($child, $unused2)

                if ([StarAiAppSmokeWin32]::Txt($child) -eq $StartButtonText) {
                    [StarAiAppSmokeWin32]::SendMessage($child, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
                    $script:ClickedStarAiButton = $true
                    return $false
                }

                return $true
            }, [IntPtr]::Zero) | Out-Null

            return -not $script:ClickedStarAiButton
        }, [IntPtr]::Zero) | Out-Null

        if ($script:ClickedStarAiButton) {
            return
        }

        Start-Sleep -Milliseconds 250
    } while ((Get-Date) -lt $deadline)

    throw "Could not click the app's Sparring Start button."
}

function Set-SmokePreferences {
    if (Test-Path -LiteralPath $Preferences) {
        Copy-Item -LiteralPath $Preferences -Destination $PreferencesBackup -Force
    }

    $directory = Split-Path $Preferences -Parent
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    @"
{
  "StarCraftRoot": "$($Root.Replace('\', '\\'))",
  "PlayerRace": 2,
  "EnemyRace": 1,
  "Tier": null,
  "BuildFilter": null,
  "Sort": "elo-asc",
  "Search": "",
  "BotId": "nitekatt",
  "MapRelativePath": "maps/(4)Fighting Spirit.scx",
  "BotBuildId": "default",
  "GameName": "StarAIAppSmoke",
  "SpeedOverrideMs": 42,
  "WindowedMode": true,
  "ConfineMouse": false
}
"@ | Set-Content -LiteralPath $Preferences -Encoding UTF8
}

function Restore-Preferences {
    if (Test-Path -LiteralPath $PreferencesBackup) {
        Copy-Item -LiteralPath $PreferencesBackup -Destination $Preferences -Force
        Remove-Item -LiteralPath $PreferencesBackup -Force
    }
}

function Assert-FinalBotJoinIni {
    $lines = Get-Content -LiteralPath $Ini | Select-String -Pattern "ai =|map =|game =|race =|enemy_race =|character_name|sound ="
    $text = ($lines | ForEach-Object { $_.Line }) -join "`n"

    if ($text -notmatch "ai = bwapi-data/AI/practice-bots/NiteKatT/ExampleAIModule.dll") {
        throw "Final bwapi.ini is not the smoke bot AI join config:`n$text"
    }

    if ($text -notmatch "character_name = StarAIBot" -or
        $text -notmatch "map =\s*(\n|$)" -or
        $text -notmatch "game = StarAIAppSmoke" -or
        $text -notmatch "race = Terran" -or
        $text -notmatch "enemy_race = Protoss" -or
        $text -notmatch "sound = OFF") {
        throw "Final bwapi.ini is not a bot-join role config:`n$text"
    }

    Write-Host "[app-smoke] Final bwapi.ini is bot-join:"
    Write-Host $text
}

try {
    if (-not (Test-Path -LiteralPath (Join-Path $Root $SafeBot))) {
        throw "Required smoke bot is missing: $(Join-Path $Root $SafeBot)"
    }

    if (-not (Test-Path -LiteralPath (Join-Path $Root $SafeMap))) {
        throw "Required smoke map is missing: $(Join-Path $Root $SafeMap)"
    }

    Stop-LocalRuntime
    Set-SmokePreferences

    if (-not $SkipBuild) {
        Invoke-Native -FilePath "dotnet" -Arguments @(
            "publish",
            (Join-Path $RepoRoot "src\StarAI.PracticeClient.App\StarAI.PracticeClient.App.csproj"),
            "-c",
            "Release",
            "-o",
            $AppDir,
            "-v:quiet",
            "-p:OutputPath=$BuildDir\"
        )
    }

    if (Test-Path -LiteralPath $Log) {
        Remove-Item -LiteralPath $Log -Force
    }

    Add-UiClickTypes
    Write-Host "[app-smoke] Launching app and clicking Sparring Start"
    $appProcess = Start-Process -FilePath $App -WorkingDirectory (Split-Path $App -Parent) -PassThru
    Start-Sleep -Seconds 2
    Invoke-SparringStartButton -ProcessId $appProcess.Id

    Wait-CompletedStarts -Count 2
    Assert-FinalBotJoinIni

    Write-Host "[app-smoke] OK: app button launched player-host then bot-join StarCraft instances."
}
finally {
    Stop-LocalRuntime
    Restore-Preferences
}
