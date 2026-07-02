$ErrorActionPreference = 'Stop'

$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$appProject = Join-Path $repo 'src\Sparring.Client\Sparring.Client.csproj'

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

$dotnet = Resolve-Dotnet

& $dotnet run --project $appProject -c Release -- --audit-compatibility
if ($LASTEXITCODE -ne 0) {
    throw "compatibility audit failed with exit code $LASTEXITCODE"
}
