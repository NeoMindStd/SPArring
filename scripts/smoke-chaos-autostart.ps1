param(
    [string]$PlayerRoot = "C:\starai\SC116AI",
    [string]$AiRoot = "C:\starai\SC116AI_ai",
    [int]$TimeoutSeconds = 20
)

$ErrorActionPreference = "Stop"

$existing = Get-Process "Chaoslauncher - MultiInstance", "StarCraft" -ErrorAction SilentlyContinue
if ($existing) {
    $list = ($existing | ForEach-Object { "$($_.ProcessName)#$($_.Id)" }) -join ", "
    throw "Refusing live smoke while StarCraft/ChaosLauncher is already running: $list"
}

$installKey = "HKLM:\SOFTWARE\WOW6432Node\Blizzard Entertainment\StarCraft"
$launcherKey = "HKCU:\Software\Chaoslauncher\Launcher"

function Set-ChaosRoot {
    param([string]$Root, [bool]$RunOnStartup)

    New-Item -Path $launcherKey -Force | Out-Null
    Set-ItemProperty -LiteralPath $installKey -Name "InstallPath" -Value $Root
    Set-ItemProperty -LiteralPath $installKey -Name "Program" -Value (Join-Path $Root "StarCraft.exe")
    Set-ItemProperty -Path $launcherKey -Name "GameVersion" -Value "Starcraft 1.16.1" -Type String
    Set-ItemProperty -Path $launcherKey -Name "WarnNoAdmin" -Value 0 -Type DWord
    Set-ItemProperty -Path $launcherKey -Name "RunScOnStartup" -Value ($(if ($RunOnStartup) { 1 } else { 0 })) -Type DWord
}

function Wait-LogLine {
    param(
        [string]$LogPath,
        [string]$Needle,
        [int]$TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        if (Test-Path $LogPath) {
            $content = Get-Content -LiteralPath $LogPath -Raw
            if ($content -like "*$Needle*") {
                return
            }
        }

        Start-Sleep -Milliseconds 250
    } while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for '$Needle' in $LogPath"
}

function Invoke-ChaosRootSmoke {
    param([string]$Root)

    $launcher = Join-Path $Root "Chaoslauncher - MultiInstance.exe"
    $log = Join-Path $Root "Chaoslauncher - MultiInstance.log"
    if (-not (Test-Path $launcher)) {
        throw "ChaosLauncher not found: $launcher"
    }

    if (Test-Path $log) {
        Remove-Item -LiteralPath $log -Force
    }

    Write-Host "[live-smoke] Launching $launcher"
    Set-ChaosRoot -Root $Root -RunOnStartup $true
    $process = Start-Process -FilePath $launcher -WorkingDirectory $Root -PassThru

    try {
        Wait-LogLine -LogPath $log -Needle "GamePath: $Root\" -TimeoutSeconds $TimeoutSeconds
        Wait-LogLine -LogPath $log -Needle "Starting Starcraft completed" -TimeoutSeconds $TimeoutSeconds
        Write-Host "[live-smoke] OK: $Root"
    }
    finally {
        Get-Process "Chaoslauncher - MultiInstance", "StarCraft" -ErrorAction SilentlyContinue | Stop-Process -Force
        Set-ChaosRoot -Root $PlayerRoot -RunOnStartup $false
        Start-Sleep -Milliseconds 500
    }
}

try {
    Invoke-ChaosRootSmoke -Root $PlayerRoot
    Invoke-ChaosRootSmoke -Root $AiRoot
}
finally {
    Get-Process "Chaoslauncher - MultiInstance", "StarCraft" -ErrorAction SilentlyContinue | Stop-Process -Force
    Set-ChaosRoot -Root $PlayerRoot -RunOnStartup $false
}
