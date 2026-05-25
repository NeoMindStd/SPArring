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
$AiRoot = "${Root}_ai"
$ini = Join-Path $Root "bwapi-data\bwapi.ini"
$aiIni = Join-Path $AiRoot "bwapi-data\bwapi.ini"
$iniBackup = "$ini.starai-live-smoke.bak"
$aiIniBackup = "$aiIni.starai-live-smoke.bak"
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

function Sync-AiRuntime {
    New-Item -ItemType Directory -Path $AiRoot -Force | Out-Null
    foreach ($source in Get-ChildItem -LiteralPath $Root -Recurse -File) {
        $relative = [System.IO.Path]::GetRelativePath($Root, $source.FullName)
        $normalized = $relative.Replace('\', '/')
        if ($normalized.EndsWith(".rep", [StringComparison]::OrdinalIgnoreCase) -or
            $normalized.Contains("/write/", [StringComparison]::OrdinalIgnoreCase) -or
            $normalized.Contains("/logs/", [StringComparison]::OrdinalIgnoreCase) -or
            $normalized.Contains("/errors/", [StringComparison]::OrdinalIgnoreCase) -or
            $normalized.StartsWith("errors/", [StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        $target = Join-Path $AiRoot $relative
        New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
        if ((Test-Path -LiteralPath $target) -and
            ((Get-Item -LiteralPath $target).Length -eq $source.Length) -and
            ((Get-Item -LiteralPath $target).LastWriteTimeUtc -ge $source.LastWriteTimeUtc)) {
            continue
        }

        Copy-Item -LiteralPath $source.FullName -Destination $target -Force
        (Get-Item -LiteralPath $target).LastWriteTimeUtc = $source.LastWriteTimeUtc
    }
}

function Set-SmokeBwapiIni {
    param(
        [ValidateSet("PlayerHost", "BotJoin")]
        [string]$Role
    )

    if ($Role -eq "PlayerHost") {
        $ai = ""
        $characterName = "StarAIHuman"
        $mapValue = $map
        $race = "Protoss"
        $enemyRace = "Terran"
        $sound = "ON"
    }
    else {
        $ai = $botAi
        $characterName = "StarAIBot"
        $mapValue = ""
        $race = "Terran"
        $enemyRace = "Protoss"
        $sound = "OFF"
    }

    $targetIni = if ($Role -eq "PlayerHost") { $ini } else { $aiIni }
    New-Item -ItemType Directory -Path (Split-Path $targetIni -Parent) -Force | Out-Null
    @"
[ai]
ai = $ai

[auto_menu]
auto_menu = LAN
character_name = $characterName
pause_dbg = OFF
lan_mode = Local PC
auto_restart = OFF
map = $mapValue
game = StarAILiveSmoke
mapiteration = RANDOM
race = $race
enemy_count = 1
enemy_race = $enemyRace
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
"@ | Set-Content -LiteralPath $targetIni -Encoding ASCII
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
$aiLauncher = Join-Path $AiRoot "Chaoslauncher - MultiInstance.exe"
$log = Join-Path $Root "Chaoslauncher - MultiInstance.log"
$aiLog = Join-Path $AiRoot "Chaoslauncher - MultiInstance.log"

try {
    if (-not (Test-Path $launcher)) {
        throw "ChaosLauncher not found: $launcher"
    }

    Sync-AiRuntime
    if (-not (Test-Path $aiLauncher)) {
        throw "AI ChaosLauncher not found after sync: $aiLauncher"
    }

    foreach ($required in @(
        (Join-Path $Root $botAi.Replace("/", "\")),
        (Join-Path $Root $map.Replace("/", "\")),
        (Join-Path $AiRoot $botAi.Replace("/", "\")),
        (Join-Path $AiRoot $map.Replace("/", "\"))
    )) {
        if (-not (Test-Path $required)) {
            throw "Required live-smoke file not found: $required"
        }
    }

    Copy-Item -LiteralPath $ini -Destination $iniBackup -Force
    Copy-Item -LiteralPath $aiIni -Destination $aiIniBackup -Force
    if (Test-Path $log) {
        Remove-Item -LiteralPath $log -Force
    }
    if (Test-Path $aiLog) {
        Remove-Item -LiteralPath $aiLog -Force
    }

    Write-Host "[live-smoke] Launching player-host StarCraft through RunScOnStartup"
    Set-SmokeBwapiIni -Role PlayerHost
    Set-ChaosForRoot -StarCraftRoot $Root -RunOnStartup $true
    $launcherProcess = Start-Process -FilePath $launcher -WorkingDirectory $Root -PassThru
    Wait-CompletedStarts -LogPath $log -Count 1

    Write-Host "[live-smoke] Waiting for the player-host room, then switching bwapi.ini to bot-join"
    Start-Sleep -Seconds 5
    if (-not $launcherProcess.HasExited) {
        $launcherProcess.CloseMainWindow() | Out-Null
        if (-not $launcherProcess.WaitForExit(3000)) {
            Stop-Process -Id $launcherProcess.Id -Force -ErrorAction SilentlyContinue
        }
    }

    Set-SmokeBwapiIni -Role BotJoin
    Set-ChaosForRoot -StarCraftRoot $AiRoot -RunOnStartup $true
    Write-Host "[live-smoke] Reopening AI ChaosLauncher with RunScOnStartup for bot-join StarCraft"
    $botLauncherProcess = Start-Process -FilePath $aiLauncher -WorkingDirectory $AiRoot -PassThru
    Wait-CompletedStarts -LogPath $aiLog -Count 1

    if (-not $botLauncherProcess.HasExited) {
        $botLauncherProcess.CloseMainWindow() | Out-Null
        if (-not $botLauncherProcess.WaitForExit(3000)) {
            Stop-Process -Id $botLauncherProcess.Id -Force -ErrorAction SilentlyContinue
        }
    }

    $starCraftCount = @(
        Get-CimInstance Win32_Process | Where-Object {
            $_.Name -eq "StarCraft.exe" -and ($_.ExecutablePath -like "C:\starai\*" -or [string]::IsNullOrWhiteSpace($_.ExecutablePath))
        }
    ).Count

    if ($starCraftCount -lt 2) {
        throw "Expected two local StarCraft instances, found $starCraftCount."
    }

    Write-Host "[live-smoke] OK: player root hosted and AI root started bot-join StarCraft."
}
finally {
    if (Test-Path $iniBackup) {
        Copy-Item -LiteralPath $iniBackup -Destination $ini -Force
        Remove-Item -LiteralPath $iniBackup -Force
    }
    if (Test-Path $aiIniBackup) {
        Copy-Item -LiteralPath $aiIniBackup -Destination $aiIni -Force
        Remove-Item -LiteralPath $aiIniBackup -Force
    }

    Set-ChaosForRoot -StarCraftRoot $Root -RunOnStartup $false
    Set-ChaosForRoot -StarCraftRoot $AiRoot -RunOnStartup $false
    Stop-LocalStarCraft
}
