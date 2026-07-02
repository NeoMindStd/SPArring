param(
    [ValidateSet('DryRun', 'PrepareOnly', 'Runtime')]
    [string]$ValidationMode = 'DryRun',

    [string[]]$BotName = @(),
    [string[]]$MapName = @(),
    [int]$Limit = 0,
    [int]$ObserveSeconds = 0,
    [int]$InterRunDelaySeconds = 3,
    [switch]$IncludeNeo,
    [switch]$NoBuild,
    [switch]$RetryFailures,
    [switch]$SkipAiActivityCheck,

    [ValidateSet('None', 'Failures', 'All')]
    [string]$KeepScreenshots = 'Failures',

    [string]$ResultPath
)

$ErrorActionPreference = 'Stop'

$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$appDll = Join-Path $repo 'src\Sparring.Client\bin\Release\net8.0-windows\Sparring.Client.dll'
$solution = Join-Path $repo 'Sparring.sln'
$auditRoot = Join-Path $repo 'artifacts\compatibility-audit'
$matrixRoot = Join-Path $repo 'artifacts\compatibility-matrix'
$screenshotRoot = Join-Path $repo 'artifacts\screenshots'

if ([string]::IsNullOrWhiteSpace($ResultPath)) {
    $ResultPath = Join-Path $matrixRoot ("{0:yyyyMMdd-HHmmss}-{1}.csv" -f (Get-Date), $ValidationMode.ToLowerInvariant())
}

function Resolve-Dotnet {
    $local = Join-Path $repo '.dotnet\dotnet.exe'
    if (Test-Path -LiteralPath $local -PathType Leaf) {
        return $local
    }

    $command = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    throw 'dotnet was not found. Install .NET SDK or place it under .dotnet\dotnet.exe.'
}

function Invoke-NativeChecked {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $true)]
        [scriptblock]$Command
    )

    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE"
    }
}

function Test-NameFilter {
    param(
        [string]$Value,
        [string[]]$Filters
    )

    if ($Filters.Count -eq 0) {
        return $true
    }

    foreach ($filter in $Filters) {
        if ($Value -like "*$filter*") {
            return $true
        }
    }

    return $false
}

function ConvertTo-SafeName {
    param([string]$Value)

    $safe = $Value -replace '[^A-Za-z0-9._-]+', '_'
    return $safe.Trim('_')
}

function Join-ProcessArguments {
    param([string[]]$Values)

    return ($Values | ForEach-Object {
        if ($_ -match '[\s"]') {
            '"' + ($_.Replace('"', '\"')) + '"'
        }
        else {
            $_
        }
    }) -join ' '
}

function Invoke-CapturedNative {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $FilePath
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.Arguments = Join-ProcessArguments $Arguments

    $process = [Diagnostics.Process]::Start($startInfo)
    $stdout = $process.StandardOutput.ReadToEnd()
    $stderr = $process.StandardError.ReadToEnd()
    $process.WaitForExit()
    $output = ($stdout + "`n" + $stderr).Trim()
    return [pscustomobject]@{
        ExitCode = $process.ExitCode
        Output = $output
    }
}

function Copy-SmokeScreenshots {
    param(
        [int]$Index,
        [string]$Bot,
        [string]$Map
    )

    if (-not (Test-Path -LiteralPath $screenshotRoot)) {
        return
    }

    $target = Join-Path $matrixRoot 'screenshots'
    New-Item -ItemType Directory -Force -Path $target | Out-Null
    $prefix = '{0:D4}-{1}-{2}' -f $Index, (ConvertTo-SafeName $Bot), (ConvertTo-SafeName $Map)
    Get-ChildItem -LiteralPath $screenshotRoot -Filter 'smoke-start-*.png' -File -ErrorAction SilentlyContinue |
        ForEach-Object {
            Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $target "$prefix-$($_.Name)") -Force
        }
}

$dotnet = Resolve-Dotnet
New-Item -ItemType Directory -Force -Path $matrixRoot | Out-Null

if (-not $NoBuild) {
    Invoke-NativeChecked -Name 'dotnet build' -Command {
        & $dotnet build $solution -c Release --nologo --no-restore
    }
}

if (-not (Test-Path -LiteralPath $appDll -PathType Leaf)) {
    throw "Sparring.Client release DLL was not found: $appDll"
}

Invoke-NativeChecked -Name 'compatibility audit' -Command {
    & $dotnet $appDll --audit-compatibility --bundled-catalog-only
}

$compatiblePairsPath = Join-Path $auditRoot 'compatible-pairs.csv'
if (-not (Test-Path -LiteralPath $compatiblePairsPath -PathType Leaf)) {
    throw "compatible pair CSV was not found: $compatiblePairsPath"
}

$completed = @{}
if ((Test-Path -LiteralPath $ResultPath -PathType Leaf) -and -not $RetryFailures) {
    Import-Csv -LiteralPath $ResultPath | ForEach-Object {
        $completed["$($_.botName)|$($_.mapName)"] = $true
    }
}

$pairs = Import-Csv -LiteralPath $compatiblePairsPath |
    Where-Object { Test-NameFilter $_.botName $BotName } |
    Where-Object { Test-NameFilter $_.mapName $MapName } |
    Where-Object { $IncludeNeo -or ($_.botName -notlike 'Neo*') } |
    Where-Object { -not $completed.ContainsKey("$($_.botName)|$($_.mapName)") } |
    Sort-Object botName, mapName

if ($Limit -gt 0) {
    $pairs = @($pairs | Select-Object -First $Limit)
}
else {
    $pairs = @($pairs)
}

if ($pairs.Count -eq 0) {
    Write-Host "compatibility-matrix: no pairs to test. result=$ResultPath"
    exit 0
}

$runtimeObserveSeconds = $ObserveSeconds
if ($ValidationMode -eq 'Runtime' -and $runtimeObserveSeconds -le 0) {
    $runtimeObserveSeconds = 90
}

$failures = 0
$index = 0
foreach ($pair in $pairs) {
    $index++
    $args = @(
        $appDll,
        '--smoke-start',
        '--bundled-catalog-only',
        '--mode', 'Sparring',
        '--bot', $pair.botName,
        '--map', $pair.mapName
    )

    if ($ValidationMode -eq 'DryRun') {
        $args += '--dry-run'
    }
    elseif ($ValidationMode -eq 'PrepareOnly') {
        $args += '--prepare-only'
    }
    elseif ($runtimeObserveSeconds -gt 0) {
        $args += @('--observe-seconds', $runtimeObserveSeconds.ToString())
        if (-not $SkipAiActivityCheck) {
            $args += '--require-ai-activity'
        }
    }

    Write-Host ("compatibility-matrix [{0}/{1}] {2} + {3}" -f $index, $pairs.Count, $pair.botName, $pair.mapName)
    $started = Get-Date
    $captured = Invoke-CapturedNative -FilePath $dotnet -Arguments $args
    $output = $captured.Output
    $exitCode = $captured.ExitCode
    $elapsedMs = [int]((Get-Date) - $started).TotalMilliseconds
    if ($exitCode -ne 0) {
        $failures++
    }

    if ($ValidationMode -eq 'Runtime' -and
        ($KeepScreenshots -eq 'All' -or ($KeepScreenshots -eq 'Failures' -and $exitCode -ne 0))) {
        Copy-SmokeScreenshots -Index $index -Bot $pair.botName -Map $pair.mapName
    }

    $row = [pscustomobject]@{
        timestampUtc = (Get-Date).ToUniversalTime().ToString('o')
        validationMode = $ValidationMode
        botName = $pair.botName
        botRace = $pair.botRace
        botExecutable = $pair.botExecutable
        mapName = $pair.mapName
        mapFileName = $pair.mapFileName
        observeSeconds = if ($ValidationMode -eq 'Runtime') { $runtimeObserveSeconds } else { 0 }
        exitCode = $exitCode
        elapsedMs = $elapsedMs
        output = ($output -replace "\r?\n", ' ')
    }

    if (Test-Path -LiteralPath $ResultPath -PathType Leaf) {
        $row | Export-Csv -LiteralPath $ResultPath -NoTypeInformation -Append -Encoding UTF8
    }
    else {
        $row | Export-Csv -LiteralPath $ResultPath -NoTypeInformation -Encoding UTF8
    }

    if ($ValidationMode -eq 'Runtime' -and $InterRunDelaySeconds -gt 0 -and $index -lt $pairs.Count) {
        Start-Sleep -Seconds $InterRunDelaySeconds
    }
}

Write-Host "compatibility-matrix: tested=$($pairs.Count), failures=$failures, result=$ResultPath"
if ($failures -gt 0) {
    exit 1
}
