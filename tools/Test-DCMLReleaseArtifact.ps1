[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ReleaseDirectory,

    [string]$ExpectedSourceCommit,

    [string]$ProjectRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-AbsolutePath {
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter(Mandatory)]
        [string]$BasePath
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath(
        (Join-Path $BasePath $Path))
}

function Assert-File {
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter(Mandatory)]
        [string]$Description
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Description was not found: $Path"
    }
}

function Assert-Directory {
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter(Mandatory)]
        [string]$Description
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        throw "$Description was not found: $Path"
    }
}

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot =
        [System.IO.Path]::GetFullPath(
            (Join-Path $PSScriptRoot '..'))
}
else {
    $ProjectRoot =
        Get-AbsolutePath `
            -Path $ProjectRoot `
            -BasePath (Get-Location).Path
}

Assert-File `
    -Path (Join-Path $ProjectRoot 'DCML.sln') `
    -Description 'DCML solution'

$ReleaseDirectory =
    Get-AbsolutePath `
        -Path $ReleaseDirectory `
        -BasePath $ProjectRoot

Assert-Directory `
    -Path $ReleaseDirectory `
    -Description 'Release directory'

$releaseInfoPath =
    Join-Path `
        $ReleaseDirectory `
        'release-info.json'

Assert-File `
    -Path $releaseInfoPath `
    -Description 'Release metadata'

$releaseInfo =
    Get-Content `
        -LiteralPath $releaseInfoPath `
        -Raw |
    ConvertFrom-Json

if ($releaseInfo.schemaVersion -ne 1) {
    throw "Unsupported release-info schema version: $($releaseInfo.schemaVersion)"
}

$version =
    [string]$releaseInfo.version

$metadataSourceCommit =
    [string]$releaseInfo.sourceCommit

$metadataPackageFile =
    [string]$releaseInfo.packageFile

$metadataPackageSha256 =
    [string]$releaseInfo.packageSha256

if (
    [string]::IsNullOrWhiteSpace($version) -or
    $version -notmatch '^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$'
) {
    throw "Release metadata contains an invalid version: '$version'"
}

if (
    [string]::IsNullOrWhiteSpace($metadataSourceCommit) -or
    $metadataSourceCommit -notmatch '^[0-9A-Fa-f]{40,64}$'
) {
    throw "Release metadata contains an invalid source commit: '$metadataSourceCommit'"
}

if (
    [string]::IsNullOrWhiteSpace($metadataPackageSha256) -or
    $metadataPackageSha256 -notmatch '^[0-9A-Fa-f]{64}$'
) {
    throw 'Release metadata contains an invalid package SHA-256.'
}

$expectedPackageFile =
    "DCML-v$version.zip"

if (
    -not [string]::Equals(
        $metadataPackageFile,
        $expectedPackageFile,
        [System.StringComparison]::Ordinal)
) {
    throw @"
Release package filename does not match the metadata version.
Expected: $expectedPackageFile
Metadata: $metadataPackageFile
"@
}

$packagePath =
    Join-Path `
        $ReleaseDirectory `
        $metadataPackageFile

$checksumPath =
    Join-Path `
        $ReleaseDirectory `
        "DCML-v$version.sha256"

Assert-File `
    -Path $packagePath `
    -Description 'Release ZIP'

Assert-File `
    -Path $checksumPath `
    -Description 'Release SHA-256 file'

if ([string]::IsNullOrWhiteSpace($ExpectedSourceCommit)) {
    $ExpectedSourceCommit =
        (& git -C $ProjectRoot rev-parse HEAD 2>$null)

    if (
        $LASTEXITCODE -ne 0 -or
        [string]::IsNullOrWhiteSpace($ExpectedSourceCommit)
    ) {
        throw 'Could not determine the expected source commit from Git HEAD.'
    }

    $ExpectedSourceCommit =
        $ExpectedSourceCommit.Trim()
}

if ($ExpectedSourceCommit -notmatch '^[0-9A-Fa-f]{40,64}$') {
    throw "ExpectedSourceCommit is not a valid Git object ID: '$ExpectedSourceCommit'"
}

if (
    -not [string]::Equals(
        $metadataSourceCommit,
        $ExpectedSourceCommit,
        [System.StringComparison]::OrdinalIgnoreCase)
) {
    throw @"
Release source commit does not match the expected source commit.
Expected: $ExpectedSourceCommit
Metadata: $metadataSourceCommit
"@
}

$actualPackageSha256 =
    (Get-FileHash `
        -LiteralPath $packagePath `
        -Algorithm SHA256).Hash.ToLowerInvariant()

$normalizedMetadataSha256 =
    $metadataPackageSha256.ToLowerInvariant()

if (
    -not [string]::Equals(
        $actualPackageSha256,
        $normalizedMetadataSha256,
        [System.StringComparison]::Ordinal)
) {
    throw @"
Release ZIP SHA-256 does not match release-info.json.
Actual:   $actualPackageSha256
Metadata: $normalizedMetadataSha256
"@
}

$checksumLines =
    @(
        Get-Content `
            -LiteralPath $checksumPath |
        Where-Object {
            -not [string]::IsNullOrWhiteSpace($_)
        }
    )

if ($checksumLines.Count -ne 1) {
    throw "Release SHA-256 file must contain exactly one non-empty checksum line: $checksumPath"
}

$checksumMatch =
    [regex]::Match(
        $checksumLines[0],
        '^(?<hash>[0-9A-Fa-f]{64})\s+\*?(?<file>.+?)\s*$')

if (-not $checksumMatch.Success) {
    throw "Release SHA-256 file has an invalid format: $checksumPath"
}

$checksumSha256 =
    $checksumMatch.Groups['hash'].Value.ToLowerInvariant()

$checksumPackageFile =
    $checksumMatch.Groups['file'].Value

if (
    -not [string]::Equals(
        $checksumPackageFile,
        $metadataPackageFile,
        [System.StringComparison]::Ordinal)
) {
    throw @"
Release SHA-256 filename does not match release-info.json.
Checksum file: $checksumPackageFile
Metadata:      $metadataPackageFile
"@
}

if (
    -not [string]::Equals(
        $checksumSha256,
        $actualPackageSha256,
        [System.StringComparison]::Ordinal)
) {
    throw @"
Release SHA-256 file does not match the actual ZIP.
Actual:   $actualPackageSha256
Checksum: $checksumSha256
"@
}

[pscustomobject]@{
    Success = $true
    Version = $version
    ReleaseDirectory = $ReleaseDirectory
    SourceCommit = $metadataSourceCommit
    ExpectedSourceCommit = $ExpectedSourceCommit
    SourceCommitMatches = $true
    PackageFile = $metadataPackageFile
    PackagePath = $packagePath
    MetadataSha256 = $normalizedMetadataSha256
    ChecksumSha256 = $checksumSha256
    ActualSha256 = $actualPackageSha256
    HashMatches = $true
    ChecksumFileMatches = $true
    PackageNameMatches = $true
}
