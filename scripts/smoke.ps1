param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$Solution = Join-Path $RepoRoot "StarAI.PracticeClient.sln"
$Artifacts = Join-Path $RepoRoot "artifacts"
$PublishDir = Join-Path $RepoRoot "artifacts\publish\AIStarClient"
$BuildDir = Join-Path $Artifacts "smoke-build"

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

Write-Host "[smoke] Repo: $RepoRoot"
Write-Host "[smoke] Running unit tests"
Invoke-Native -FilePath "dotnet" -Arguments @("test", $Solution, "-v:quiet")

Write-Host "[smoke] Publishing app without launching StarCraft"
if (Test-Path $PublishDir) {
    Remove-Item -LiteralPath $PublishDir -Recurse -Force
}

if (Test-Path $BuildDir) {
    Remove-Item -LiteralPath $BuildDir -Recurse -Force
}

$publishArgs = @(
    "publish",
    (Join-Path $RepoRoot "src\StarAI.PracticeClient.App\StarAI.PracticeClient.App.csproj"),
    "-c",
    $Configuration,
    "-o",
    $PublishDir,
    "-v:quiet",
    "-p:OutputPath=$BuildDir\app-output\"
)
Invoke-Native -FilePath "dotnet" -Arguments $publishArgs

$forbiddenPatterns = @(
    "StarCraft.exe",
    "StarEdit.exe",
    "*.mpq",
    "Chaoslauncher*.exe",
    "*ChaosLauncher*.exe",
    "BWAPI.dll",
    "*.rep",
    "*.ERR"
)

$forbidden = foreach ($pattern in $forbiddenPatterns) {
    Get-ChildItem -LiteralPath $PublishDir -Recurse -File -Filter $pattern -ErrorAction SilentlyContinue
}

if ($forbidden) {
    $list = ($forbidden | Select-Object -ExpandProperty FullName) -join [Environment]::NewLine
    throw "Forbidden redistributable/runtime files found in publish output:$([Environment]::NewLine)$list"
}

Write-Host "[smoke] OK: tests passed and publish output contains no known game/runtime binaries."
