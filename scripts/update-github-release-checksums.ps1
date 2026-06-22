param(
    [string] $Repo = 'NeoMindStd/SPArring',
    [string] $Tag = (Get-Content -LiteralPath (Join-Path $PSScriptRoot '..\VERSION') -Raw).Trim(),
    [string] $ChecksumPath = (Join-Path $PSScriptRoot "..\artifacts\release\dist\StarAI-PracticeClient-$((Get-Content -LiteralPath (Join-Path $PSScriptRoot '..\VERSION') -Raw).Trim())-checksums.txt"),
    [switch] $UploadChecksumAsset
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw 'GitHub CLI (gh) is required.'
}

if (-not (Test-Path -LiteralPath $ChecksumPath -PathType Leaf)) {
    throw "Checksum file was not found: $ChecksumPath"
}

$bodyLines = @(gh release view $Tag --repo $Repo --json body --jq .body)
if ($LASTEXITCODE -ne 0) {
    throw "Failed to read GitHub release: $Tag"
}
$body = $bodyLines -join "`n"

$checksums = Get-Content -LiteralPath $ChecksumPath -Raw -Encoding ASCII
$sectionTitle = '## ' + (-join ([char[]](0xD30C, 0xC77C, 0x20, 0xCCB4, 0xD06C, 0xC12C))) + ' (SHA256)'
$codeFence = '```'
$newSection = $sectionTitle + "`n`n" + $codeFence + "text`n" + $checksums.Trim() + "`n" + $codeFence
$sectionPattern = '(?s)\s*##\s+.*?\(SHA256\).*?\z'

if ([regex]::IsMatch($body, $sectionPattern)) {
    $body = [regex]::Replace($body, $sectionPattern, $newSection.TrimEnd())
}
else {
    $body = $body.TrimEnd() + "`n`n" + $newSection
}

$notesPath = Join-Path ([IO.Path]::GetTempPath()) ("starai-release-notes-" + [Guid]::NewGuid().ToString('N') + ".md")
try {
    [IO.File]::WriteAllText($notesPath, $body, [Text.UTF8Encoding]::new($false))
    gh release edit $Tag --repo $Repo --notes-file $notesPath
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to update GitHub release notes: $Tag"
    }
}
finally {
    Remove-Item -LiteralPath $notesPath -Force -ErrorAction SilentlyContinue
}

if ($UploadChecksumAsset) {
    gh release upload $Tag $ChecksumPath --repo $Repo --clobber
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to upload checksum asset: $ChecksumPath"
    }
}
