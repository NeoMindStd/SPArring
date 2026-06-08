param(
    [string]$BotName,
    [string]$MapName,
    [string]$Mode,
    [string]$PlayerRace,
    [string]$EnemyRace,
    [switch]$DryRun,
    [switch]$PrepareOnly
)

$ErrorActionPreference = 'Stop'

$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$appProject = Join-Path $repo 'src\StarAI.PracticeClient.App\StarAI.PracticeClient.App.csproj'
$starCraftBefore = @((Get-Process -Name 'StarCraft' -ErrorAction SilentlyContinue).Id)

function Get-ScreenSignature {
    Add-Type -AssemblyName System.Windows.Forms
    return ([System.Windows.Forms.Screen]::AllScreens |
        ForEach-Object { "$($_.DeviceName):$($_.Bounds.X),$($_.Bounds.Y),$($_.Bounds.Width),$($_.Bounds.Height)" }) -join ';'
}

$screenBefore = Get-ScreenSignature

try {
    $smokeArgs = @('--smoke-start')
    if (-not [string]::IsNullOrWhiteSpace($BotName)) {
        $smokeArgs += @('--bot', $BotName)
    }
    if (-not [string]::IsNullOrWhiteSpace($MapName)) {
        $smokeArgs += @('--map', $MapName)
    }
    if (-not [string]::IsNullOrWhiteSpace($Mode)) {
        $smokeArgs += @('--mode', $Mode)
    }
    if (-not [string]::IsNullOrWhiteSpace($PlayerRace)) {
        $smokeArgs += @('--player-race', $PlayerRace)
    }
    if (-not [string]::IsNullOrWhiteSpace($EnemyRace)) {
        $smokeArgs += @('--enemy-race', $EnemyRace)
    }
    if ($DryRun) {
        $smokeArgs += '--dry-run'
    }
    if ($PrepareOnly) {
        $smokeArgs += '--prepare-only'
    }

    dotnet run --project $appProject -c Release -- $smokeArgs
    $exitCode = $LASTEXITCODE
}
finally {
    $deadline = (Get-Date).AddSeconds(8)
    do {
        $newStarCraft = Get-Process -Name 'StarCraft' -ErrorAction SilentlyContinue |
            Where-Object { $starCraftBefore -notcontains $_.Id }
        foreach ($process in $newStarCraft) {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        }

        Get-CimInstance Win32_Process | Where-Object {
            $_.Name -eq 'Chaoslauncher - MultiInstance.exe' -and $_.ExecutablePath -like 'C:\starai\*'
        } | ForEach-Object {
            Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
        }

        Start-Sleep -Milliseconds 300
    } while ((Get-Date) -lt $deadline -and (Get-Process -Name 'StarCraft' -ErrorAction SilentlyContinue | Where-Object { $starCraftBefore -notcontains $_.Id }))
}

if ($screenBefore -ne (Get-ScreenSignature)) {
    throw "Screen bounds changed during smoke-app-start. Before: $screenBefore"
}

exit $exitCode
