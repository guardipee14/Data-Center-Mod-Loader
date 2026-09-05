[CmdletBinding()]
param(
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

$solutionPath =
    Join-Path `
        $ProjectRoot `
        'DCML.sln'

if (-not (Test-Path -LiteralPath $solutionPath -PathType Leaf)) {
    throw "DCML solution was not found: $solutionPath"
}

$scopeRoots =
    @(
        [pscustomobject]@{
            Name = 'host-neutral-core'
            Path =
                Join-Path `
                    $ProjectRoot `
                    'src\DCML.Core'
        }

        [pscustomobject]@{
            Name = 'datacenter-package-sources'
            Path =
                Join-Path `
                    $ProjectRoot `
                    'src\DCML.DataCenter\PackageSources'
        }
    )

$rules =
    @(
        [pscustomobject]@{
            Id = 'DCML_PROVIDER_PROCESS_TYPE'
            Pattern = '\bSystem\.Diagnostics\.Process\b'
            Description = 'Direct process execution is not allowed in package/provider policy scope.'
        }

        [pscustomobject]@{
            Id = 'DCML_PROVIDER_PROCESS_START'
            Pattern = '\bProcess\.Start\s*\('
            Description = 'Process.Start is not allowed in package/provider policy scope.'
        }

        [pscustomobject]@{
            Id = 'DCML_PROVIDER_PROCESS_START_INFO'
            Pattern = '\bProcessStartInfo\b'
            Description = 'ProcessStartInfo is not allowed in package/provider policy scope.'
        }

        [pscustomobject]@{
            Id = 'DCML_PROVIDER_HTTP_NAMESPACE'
            Pattern = '\bSystem\.Net\.Http\b'
            Description = 'Direct HTTP dependencies are not allowed in package/provider policy scope.'
        }

        [pscustomobject]@{
            Id = 'DCML_PROVIDER_HTTP_CLIENT'
            Pattern = '\bHttpClient\b'
            Description = 'HttpClient is not allowed in package/provider policy scope.'
        }

        [pscustomobject]@{
            Id = 'DCML_PROVIDER_WEB_CLIENT'
            Pattern = '\bWebClient\b'
            Description = 'WebClient is not allowed in package/provider policy scope.'
        }

        [pscustomobject]@{
            Id = 'DCML_PROVIDER_WEB_REQUEST'
            Pattern = '\b(?:HttpWebRequest|WebRequest\.Create)\b'
            Description = 'Direct web requests are not allowed in package/provider policy scope.'
        }

        [pscustomobject]@{
            Id = 'DCML_PROVIDER_REMOTE_URI'
            Pattern = 'https?://'
            Description = 'Remote HTTP/HTTPS endpoints are not allowed in package/provider policy scope.'
        }

        [pscustomobject]@{
            Id = 'DCML_PROVIDER_STEAMWORKS'
            Pattern = '\bSteamworks\b'
            Description = 'Steamworks API dependencies are not allowed in the sanctioned-only package source.'
        }

        [pscustomobject]@{
            Id = 'DCML_PROVIDER_STEAM_API'
            Pattern = '\bSteamAPI\b'
            Description = 'Steam API calls are not allowed in the sanctioned-only package source.'
        }

        [pscustomobject]@{
            Id = 'DCML_PROVIDER_STEAM_UGC'
            Pattern = '\b(?:SteamUGC|ISteamUGC)\b'
            Description = 'Steam UGC API access is not allowed in the sanctioned-only package source.'
        }

        [pscustomobject]@{
            Id = 'DCML_PROVIDER_WORKSHOP_ACTION'
            Pattern = '\b(?:SubscribeItem|UnsubscribeItem|DownloadItem)\s*\('
            Description = 'Workshop subscribe/download operations are not allowed.'
        }

        [pscustomobject]@{
            Id = 'DCML_PROVIDER_STEAM_NATIVE'
            Pattern = '\bsteam_api(?:64)?\.dll\b'
            Description = 'Direct native Steam API loading is not allowed.'
        }
    )

$files =
    New-Object `
        'System.Collections.Generic.List[System.IO.FileInfo]'

foreach ($scope in $scopeRoots) {
    if (-not (Test-Path -LiteralPath $scope.Path -PathType Container)) {
        throw "Provider-restriction scope was not found: $($scope.Path)"
    }

    foreach (
        $file
        in Get-ChildItem `
            -LiteralPath $scope.Path `
            -Filter '*.cs' `
            -File `
            -Recurse
    ) {
        if (
            $file.FullName -match
            '[\\/](?:bin|obj)[\\/]'
        ) {
            continue
        }

        $files.Add($file)
    }
}

$orderedFiles =
    @(
        $files |
        Sort-Object FullName -Unique
    )

if ($orderedFiles.Count -eq 0) {
    throw 'Provider-restriction validation found no source files to inspect.'
}

$violations =
    New-Object `
        'System.Collections.Generic.List[object]'

foreach ($file in $orderedFiles) {
    $text =
        [System.IO.File]::ReadAllText(
            $file.FullName)

    $relativePath =
        [System.IO.Path]::GetRelativePath(
            $ProjectRoot,
            $file.FullName).Replace(
                '\',
                '/')

    foreach ($rule in $rules) {
        if (
            [System.Text.RegularExpressions.Regex]::IsMatch(
                $text,
                $rule.Pattern,
                [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        ) {
            $violations.Add(
                [pscustomobject]@{
                    Path = $relativePath
                    RuleId = $rule.Id
                    Description = $rule.Description
                })
        }
    }
}

if ($violations.Count -gt 0) {
    $details =
        (
            $violations |
            Select-Object -First 25 |
            ForEach-Object {
                "$($_.RuleId): $($_.Path) - $($_.Description)"
            }
        ) -join [Environment]::NewLine

    throw @"
Provider/platform restriction validation failed.
DCML package/provider code must not add direct process execution, network
retrieval, Steam/Steamworks API access, Workshop subscription/download calls,
or remote provider endpoints.

Violations:
$details
"@
}

[pscustomobject]@{
    Success = $true
    ProjectRoot = $ProjectRoot
    PolicyVersion = 1
    ScopeCount = $scopeRoots.Count
    CheckedFileCount = $orderedFiles.Count
    ForbiddenRuleCount = $rules.Count
    ViolationCount = 0
}
