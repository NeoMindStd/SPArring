$ErrorActionPreference = 'Stop'

$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$appProject = Join-Path $repo 'src\Sparring.Client\Sparring.Client.csproj'

dotnet run --project $appProject -c Release -- --audit-compatibility
if ($LASTEXITCODE -ne 0) {
    throw "compatibility audit failed with exit code $LASTEXITCODE"
}
