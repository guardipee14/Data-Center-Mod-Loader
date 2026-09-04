[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ReleaseDirectory,

    [string]$ProjectRoot,

    [string]$BaseCommit,

    [string]$ReleaseCommit,

    [string]$LiveProofPath,

    [string[]]$ChangedPath,

    [switch]$AllowChangedPathOverride
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

function Resolve-GitCommit {
    param(
        [Parameter(Mandatory)]
        [string]$ProjectRoot,

        [Parameter(Mandatory)]
        [string]$Commitish,

        [Parameter(Mandatory)]
        [string]$Description
    )

    $resolved =
        (& git -C $ProjectRoot rev-parse --verify "$Commitish^{commit}" 2>$null)

    if (
        $LASTEXITCODE -ne 0 -or
        [string]::IsNullOrWhiteSpace($resolved)
    ) {
        throw "Could not resolve $Description as a Git commit: $Commitish"
    }

    return $resolved.Trim()
}

function Normalize-RepositoryPath {
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    $normalized =
        $Path.Trim().Replace(
            '\',
            '/')

    while ($normalized.StartsWith('./', [System.StringComparison]::Ordinal)) {
        $normalized =
            $normalized.Substring(2)
    }

    return $normalized
}

function Get-ReleaseChangeClassification {
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    $normalized =
        Normalize-RepositoryPath `
            -Path $Path

    if ([string]::IsNullOrWhiteSpace($normalized)) {
        throw 'Changed path cannot be empty.'
    }

    if (
        $normalized.StartsWith(
            'src/',
            [System.StringComparison]::OrdinalIgnoreCase)
    ) {
        return [pscustomobject]@{
            Path = $normalized
            RuntimeFacing = $true
            Reason = 'runtime-source'
        }
    }

    if (
        [string]::Equals(
            $normalized,
            'tools/Build-DCMLRelease.ps1',
            [System.StringComparison]::OrdinalIgnoreCase)
    ) {
        return [pscustomobject]@{
            Path = $normalized
            RuntimeFacing = $true
            Reason = 'release-package-layout'
        }
    }

    $nonRuntimeExact =
        [System.Collections.Generic.HashSet[string]]::new(
            [System.StringComparer]::OrdinalIgnoreCase)

    foreach (
        $safePath in
        @(
            'README.md'
            'LICENSE'
            'LICENSE.md'
            '.gitignore'
            'DCML.sln'
            'tools/Test-DCMLReleaseArtifact.ps1'
            'tools/Test-DCMLReleaseReadiness.ps1'
        )
    ) {
        $null =
            $nonRuntimeExact.Add(
                $safePath)
    }

    if ($nonRuntimeExact.Contains($normalized)) {
        return [pscustomobject]@{
            Path = $normalized
            RuntimeFacing = $false
            Reason = 'repository-support'
        }
    }

    foreach (
        $safePrefix in
        @(
            'docs/'
            'tests/'
            'examples/'
            '.github/'
        )
    ) {
        if (
            $normalized.StartsWith(
                $safePrefix,
                [System.StringComparison]::OrdinalIgnoreCase)
        ) {
            return [pscustomobject]@{
                Path = $normalized
                RuntimeFacing = $false
                Reason = 'non-runtime-content'
            }
        }
    }

    return [pscustomobject]@{
        Path = $normalized
        RuntimeFacing = $true
        Reason = 'conservative-unclassified'
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

$gitRoot =
    (& git -C $ProjectRoot rev-parse --show-toplevel 2>$null)

if (
    $LASTEXITCODE -ne 0 -or
    [string]::IsNullOrWhiteSpace($gitRoot)
) {
    throw "Project root is not inside a Git repository: $ProjectRoot"
}

$gitRoot =
    [System.IO.Path]::GetFullPath(
        $gitRoot.Trim())

if (
    -not [string]::Equals(
        $gitRoot,
        [System.IO.Path]::GetFullPath($ProjectRoot),
        [System.StringComparison]::OrdinalIgnoreCase)
) {
    throw "ProjectRoot must be the Git repository root. Git root: $gitRoot"
}

if ([string]::IsNullOrWhiteSpace($ReleaseCommit)) {
    $ReleaseCommit =
        'HEAD'
}

$resolvedReleaseCommit =
    Resolve-GitCommit `
        -ProjectRoot $ProjectRoot `
        -Commitish $ReleaseCommit `
        -Description 'release commit'

$ReleaseDirectory =
    Get-AbsolutePath `
        -Path $ReleaseDirectory `
        -BasePath $ProjectRoot

$artifactValidator =
    Join-Path `
        $ProjectRoot `
        'tools\Test-DCMLReleaseArtifact.ps1'

Assert-File `
    -Path $artifactValidator `
    -Description 'Release artifact validator'

$artifactValidation =
    & $artifactValidator `
        -ReleaseDirectory $ReleaseDirectory `
        -ProjectRoot $ProjectRoot `
        -ExpectedSourceCommit $resolvedReleaseCommit

if (
    $null -eq $artifactValidation -or
    -not $artifactValidation.Success
) {
    throw 'Release artifact validation did not report success.'
}

$resolvedBaseCommit =
    $null

$changedPaths =
    @()

if ($null -ne $ChangedPath -and @($ChangedPath).Count -gt 0) {
    if (-not $AllowChangedPathOverride) {
        throw 'ChangedPath override requires -AllowChangedPathOverride and is intended only for repository validation/tests.'
    }

    if (-not [string]::IsNullOrWhiteSpace($BaseCommit)) {
        $resolvedBaseCommit =
            Resolve-GitCommit `
                -ProjectRoot $ProjectRoot `
                -Commitish $BaseCommit `
                -Description 'base commit'
    }

    $changedPaths =
        @(
            $ChangedPath |
            ForEach-Object {
                Normalize-RepositoryPath `
                    -Path $_
            } |
            Where-Object {
                -not [string]::IsNullOrWhiteSpace($_)
            } |
            Sort-Object -Unique
        )
}
else {
    if ([string]::IsNullOrWhiteSpace($BaseCommit)) {
        throw 'BaseCommit is required unless the explicit ChangedPath test override is used.'
    }

    $resolvedBaseCommit =
        Resolve-GitCommit `
            -ProjectRoot $ProjectRoot `
            -Commitish $BaseCommit `
            -Description 'base commit'

    $diffRange =
        "$resolvedBaseCommit..$resolvedReleaseCommit"

    $gitChangedPaths =
        @(
            & git -C $ProjectRoot diff --name-only $diffRange --
        )

    if ($LASTEXITCODE -ne 0) {
        throw "Could not determine changed paths for release range: $diffRange"
    }

    $changedPaths =
        @(
            $gitChangedPaths |
            ForEach-Object {
                Normalize-RepositoryPath `
                    -Path $_
            } |
            Where-Object {
                -not [string]::IsNullOrWhiteSpace($_)
            } |
            Sort-Object -Unique
        )
}

$classifications =
    @(
        foreach ($path in $changedPaths) {
            Get-ReleaseChangeClassification `
                -Path $path
        }
    )

$runtimeFacingChanges =
    @(
        $classifications |
        Where-Object {
            $_.RuntimeFacing
        }
    )

$liveProofRequired =
    $runtimeFacingChanges.Count -gt 0

$liveProofProvided =
    -not [string]::IsNullOrWhiteSpace($LiveProofPath)

$liveProofValidated =
    $false

$resolvedLiveProofPath =
    $null

if ($liveProofRequired) {
    if ([string]::IsNullOrWhiteSpace($LiveProofPath)) {
        $LiveProofPath =
            Join-Path `
                $ReleaseDirectory `
                'live-proof.json'
    }

    $resolvedLiveProofPath =
        Get-AbsolutePath `
            -Path $LiveProofPath `
            -BasePath $ProjectRoot

    if (-not (Test-Path -LiteralPath $resolvedLiveProofPath -PathType Leaf)) {
        $runtimePathText =
            (
                $runtimeFacingChanges |
                ForEach-Object {
                    $_.Path
                }
            ) -join ', '

        throw @"
Live Data Center proof is required for runtime-facing release changes.
Runtime-facing paths: $runtimePathText
Expected proof file: $resolvedLiveProofPath
"@
    }

    $liveProofProvided =
        $true

    $proof =
        Get-Content `
            -LiteralPath $resolvedLiveProofPath `
            -Raw |
        ConvertFrom-Json

    if ($proof.schemaVersion -ne 1) {
        throw "Unsupported live-proof schema version: $($proof.schemaVersion)"
    }

    $proofSourceCommit =
        [string]$proof.sourceCommit

    if (
        [string]::IsNullOrWhiteSpace($proofSourceCommit) -or
        -not [string]::Equals(
            $proofSourceCommit,
            $resolvedReleaseCommit,
            [System.StringComparison]::OrdinalIgnoreCase)
    ) {
        throw @"
Live proof source commit does not match the release commit.
Release: $resolvedReleaseCommit
Proof:   $proofSourceCommit
"@
    }

    $proofPackageSha256 =
        [string]$proof.packageSha256

    if (
        [string]::IsNullOrWhiteSpace($proofPackageSha256) -or
        $proofPackageSha256 -notmatch '^[0-9A-Fa-f]{64}$'
    ) {
        throw 'Live proof contains an invalid package SHA-256.'
    }

    if (
        -not [string]::Equals(
            $proofPackageSha256,
            [string]$artifactValidation.ActualSha256,
            [System.StringComparison]::OrdinalIgnoreCase)
    ) {
        throw @"
Live proof package SHA-256 does not match the validated release artifact.
Artifact: $($artifactValidation.ActualSha256)
Proof:    $proofPackageSha256
"@
    }

    if (
        -not [string]::Equals(
            [string]$proof.result,
            'passed',
            [System.StringComparison]::OrdinalIgnoreCase)
    ) {
        throw "Live proof result must be 'passed'."
    }

    if (
        -not [string]::Equals(
            [string]$proof.game,
            'Data Center',
            [System.StringComparison]::Ordinal)
    ) {
        throw "Live proof game must be 'Data Center'."
    }

    if ([string]::IsNullOrWhiteSpace([string]$proof.host)) {
        throw 'Live proof host cannot be empty.'
    }

    $observedAt =
        [DateTimeOffset]::MinValue

    $observedAtValid =
        [DateTimeOffset]::TryParse(
            [string]$proof.observedAtUtc,
            [System.Globalization.CultureInfo]::InvariantCulture,
            [System.Globalization.DateTimeStyles]::RoundtripKind,
            [ref]$observedAt)

    if (-not $observedAtValid) {
        throw 'Live proof observedAtUtc must be a valid ISO-8601 timestamp.'
    }

    if ([string]::IsNullOrWhiteSpace([string]$proof.summary)) {
        throw 'Live proof summary cannot be empty.'
    }

    $liveProofValidated =
        $true
}

[pscustomobject]@{
    Success = $true
    ProjectRoot = $ProjectRoot
    ReleaseDirectory = $ReleaseDirectory
    BaseCommit = $resolvedBaseCommit
    ReleaseCommit = $resolvedReleaseCommit
    ArtifactValidated = [bool]$artifactValidation.Success
    ArtifactSha256 = [string]$artifactValidation.ActualSha256
    ChangeCount = $changedPaths.Count
    ChangedPaths = $changedPaths
    RuntimeFacingChangeCount = $runtimeFacingChanges.Count
    RuntimeFacingPaths =
        @(
            $runtimeFacingChanges |
            ForEach-Object {
                $_.Path
            }
        )
    ChangeClassifications = $classifications
    LiveProofRequired = $liveProofRequired
    LiveProofProvided = $liveProofProvided
    LiveProofValidated = $liveProofValidated
    LiveProofPath = $resolvedLiveProofPath
}
