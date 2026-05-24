param(
    [string]$Version = (Get-Content (Join-Path $PSScriptRoot "..\VERSION") -Raw).Trim()
)

$ErrorActionPreference = "Stop"
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$Artifacts = Join-Path $RepoRoot "artifacts"
$PublishDir = Join-Path $Artifacts "publish\AIStarClient"
$ZipPath = Join-Path $Artifacts "AIStarClient-$Version-win-x64.zip"

& (Join-Path $PSScriptRoot "smoke.ps1") -Configuration Release

if (Test-Path $ZipPath) {
    Remove-Item -LiteralPath $ZipPath -Force
}

Compress-Archive -Path (Join-Path $PublishDir "*") -DestinationPath $ZipPath
Write-Host "[release] Zip: $ZipPath"

$iscc = Get-Command ISCC.exe -ErrorAction SilentlyContinue
if ($iscc) {
    Write-Host "[release] Building installer with Inno Setup"
    & $iscc.Source (Join-Path $RepoRoot "installer\AIStarClient.iss") "/DMyAppVersion=$Version"
} else {
    Write-Host "[release] Inno Setup not found; skipped installer build."
}
