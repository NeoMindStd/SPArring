param(
    [string]$Root = "C:\starai\SC116AI",
    [int]$TimeoutSeconds = 25
)

$ErrorActionPreference = "Stop"

$localProcesses = Get-CimInstance Win32_Process | Where-Object {
    ($_.Name -eq "Chaoslauncher - MultiInstance.exe" -and $_.ExecutablePath -like "C:\starai\*") -or
    ($_.Name -eq "StarCraft.exe" -and ($_.ExecutablePath -like "C:\starai\*" -or [string]::IsNullOrWhiteSpace($_.ExecutablePath)))
}

if ($localProcesses) {
    $list = ($localProcesses | ForEach-Object { "$($_.Name)#$($_.ProcessId)" }) -join ", "
    throw "Refusing live smoke while local StarCraft/ChaosLauncher is already running: $list"
}

$installKey = "HKLM:\SOFTWARE\WOW6432Node\Blizzard Entertainment\StarCraft"
$launcherKey = "HKCU:\Software\Chaoslauncher\Launcher"
$enabledKey = "HKCU:\Software\Chaoslauncher\PluginsEnabled"
$runIncompatibleKey = "HKCU:\Software\Chaoslauncher\PluginsRunIncompatible"
$bwapiPlugin = "BWAPI 4.4.0 Injector [RELEASE]"
$wmodePlugin = "W-MODE 1.02"
$ini = Join-Path $Root "bwapi-data\bwapi.ini"
$iniBackup = "$ini.starai-live-smoke.bak"
$coachAi = "bwapi-data/AI/CoachAI/AnyRace_CoachAI.dll"
$botAi = "bwapi-data/AI/practice-bots/NiteKatT/ExampleAIModule.dll"
$map = "maps/(4)Fighting Spirit.scx"

function Set-ChaosForRoot {
    param([string]$StarCraftRoot, [bool]$RunOnStartup)

    New-Item -Path $launcherKey, $enabledKey, $runIncompatibleKey -Force | Out-Null
    Set-ItemProperty -LiteralPath $installKey -Name "InstallPath" -Value $StarCraftRoot
    Set-ItemProperty -LiteralPath $installKey -Name "Program" -Value (Join-Path $StarCraftRoot "StarCraft.exe")
    Set-ItemProperty -Path $launcherKey -Name "GameVersion" -Value "Starcraft 1.16.1" -Type String
    Set-ItemProperty -Path $launcherKey -Name "WarnNoAdmin" -Value 0 -Type DWord
    Set-ItemProperty -Path $launcherKey -Name "RunScOnStartup" -Value ($(if ($RunOnStartup) { 1 } else { 0 })) -Type DWord
    Set-ItemProperty -Path $enabledKey -Name $wmodePlugin -Value 1 -Type DWord
    Set-ItemProperty -Path $enabledKey -Name $bwapiPlugin -Value 1 -Type DWord
    Set-ItemProperty -Path $runIncompatibleKey -Name $wmodePlugin -Value 0 -Type DWord
    Set-ItemProperty -Path $runIncompatibleKey -Name $bwapiPlugin -Value 0 -Type DWord
}

function Get-CompletedStartCount {
    param([string]$LogPath)

    if (-not (Test-Path $LogPath)) {
        return 0
    }

    return @((Get-Content -LiteralPath $LogPath) | Where-Object { $_ -like "*Starting Starcraft completed*" }).Count
}

function Wait-CompletedStarts {
    param(
        [string]$LogPath,
        [int]$Count
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        if ((Get-CompletedStartCount -LogPath $LogPath) -ge $Count) {
            return
        }

        Start-Sleep -Milliseconds 250
    } while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for $Count completed StarCraft starts in $LogPath"
}

function Set-SmokeBwapiIni {
    param([bool]$SoundOn)

    $sound = if ($SoundOn) { "ON" } else { "OFF" }
    @"
[ai]
ai = $coachAi,$botAi

[auto_menu]
auto_menu = LAN
character_name = StarAIHuman
pause_dbg = OFF
lan_mode = Local PC
auto_restart = OFF
map = $map
game = StarAILiveSmoke
mapiteration = RANDOM
race = Protoss,Terran
enemy_count = 1
enemy_race = Terran
enemy_race_1 = Default
enemy_race_2 = Default
enemy_race_3 = Default
enemy_race_4 = Default
enemy_race_5 = Default
enemy_race_6 = Default
enemy_race_7 = Default
game_type = MELEE
game_type_extra =
save_replay =
wait_for_min_players = 2
wait_for_max_players = 2
wait_for_time = 5000

[window]
windowed = ON
left = 0
top = 0
width = 640
height = 480

[starcraft]
sound = $sound
screenshots = gif
drop_players = ON
speed_override = 42
"@ | Set-Content -LiteralPath $ini -Encoding ASCII
}

function Invoke-StartButton {
    param([int]$ProcessId)

    Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class StarAiSmokeWin32 {
 public delegate bool EnumProc(IntPtr h, IntPtr l);
 [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
 [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr p, EnumProc cb, IntPtr l);
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out int processId);
 [DllImport("user32.dll", CharSet=CharSet.Auto)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
 [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);
 [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
 public static string Txt(IntPtr h){ var sb=new StringBuilder(512); GetWindowText(h,sb,sb.Capacity); return sb.ToString(); }
}
'@ -ErrorAction SilentlyContinue

    $windows = New-Object System.Collections.ArrayList
    [StarAiSmokeWin32]::EnumWindows({
        param($handle, $unused)
        $candidatePid = 0
        [StarAiSmokeWin32]::GetWindowThreadProcessId($handle, [ref]$candidatePid) | Out-Null
        if ($candidatePid -eq $ProcessId -and [StarAiSmokeWin32]::IsWindowVisible($handle)) {
            [void]$windows.Add($handle)
        }

        return $true
    }, [IntPtr]::Zero) | Out-Null

    foreach ($window in $windows) {
        $script:button = [IntPtr]::Zero
        [StarAiSmokeWin32]::EnumChildWindows($window, {
            param($handle, $unused)
            $text = [StarAiSmokeWin32]::Txt($handle).Replace("&", "")
            if ($text -eq "Start") {
                $script:button = $handle
                return $false
            }

            return $true
        }, [IntPtr]::Zero) | Out-Null

        if ($script:button -ne [IntPtr]::Zero) {
            [StarAiSmokeWin32]::ShowWindow($window, 9) | Out-Null
            [StarAiSmokeWin32]::SetForegroundWindow($window) | Out-Null
            [StarAiSmokeWin32]::SendMessage($script:button, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
            return
        }
    }

    throw "Could not find ChaosLauncher Start button."
}

function Stop-LocalStarCraft {
    Get-CimInstance Win32_Process | Where-Object {
        ($_.Name -eq "Chaoslauncher - MultiInstance.exe" -and $_.ExecutablePath -like "C:\starai\*") -or
        ($_.Name -eq "StarCraft.exe" -and ($_.ExecutablePath -like "C:\starai\*" -or [string]::IsNullOrWhiteSpace($_.ExecutablePath)))
    } | ForEach-Object {
        Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
    }
}

$launcher = Join-Path $Root "Chaoslauncher - MultiInstance.exe"
$log = Join-Path $Root "Chaoslauncher - MultiInstance.log"

try {
    if (-not (Test-Path $launcher)) {
        throw "ChaosLauncher not found: $launcher"
    }

    foreach ($required in @(
        (Join-Path $Root $coachAi.Replace("/", "\")),
        (Join-Path $Root $botAi.Replace("/", "\")),
        (Join-Path $Root $map.Replace("/", "\"))
    )) {
        if (-not (Test-Path $required)) {
            throw "Required live-smoke file not found: $required"
        }
    }

    Copy-Item -LiteralPath $ini -Destination $iniBackup -Force
    if (Test-Path $log) {
        Remove-Item -LiteralPath $log -Force
    }

    Write-Host "[live-smoke] Launching first StarCraft through RunScOnStartup"
    Set-SmokeBwapiIni -SoundOn $true
    Set-ChaosForRoot -StarCraftRoot $Root -RunOnStartup $true
    $launcherProcess = Start-Process -FilePath $launcher -WorkingDirectory $Root -PassThru
    Wait-CompletedStarts -LogPath $log -Count 1

    Write-Host "[live-smoke] Clicking the existing ChaosLauncher Start button for second StarCraft"
    Set-SmokeBwapiIni -SoundOn $false
    Set-ChaosForRoot -StarCraftRoot $Root -RunOnStartup $false
    Invoke-StartButton -ProcessId $launcherProcess.Id
    Wait-CompletedStarts -LogPath $log -Count 2

    $starCraftCount = @(
        Get-CimInstance Win32_Process | Where-Object {
            $_.Name -eq "StarCraft.exe" -and ($_.ExecutablePath -like "C:\starai\*" -or [string]::IsNullOrWhiteSpace($_.ExecutablePath))
        }
    ).Count

    if ($starCraftCount -lt 2) {
        throw "Expected two local StarCraft instances, found $starCraftCount."
    }

    Write-Host "[live-smoke] OK: one ChaosLauncher started two StarCraft instances."
}
finally {
    if (Test-Path $iniBackup) {
        Copy-Item -LiteralPath $iniBackup -Destination $ini -Force
        Remove-Item -LiteralPath $iniBackup -Force
    }

    Set-ChaosForRoot -StarCraftRoot $Root -RunOnStartup $false
    Stop-LocalStarCraft
}
