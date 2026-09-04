[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$')]
    [string]$Version,

    [string]$ProjectRoot,

    [string]$DataCenterRoot =
        'C:\Program Files (x86)\Steam\steamapps\common\Data Center',

    [string]$OutputDirectory,

    [switch]$SkipBuild,

    [switch]$AllowDirty
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

function Invoke-ExternalCommand {
    param(
        [Parameter(Mandatory)]
        [string]$FilePath,

        [Parameter(Mandatory)]
        [string[]]$Arguments,

        [Parameter(Mandatory)]
        [string]$Description
    )

    Write-Host "`n===== $Description =====" -ForegroundColor Cyan

    $commandOutput =
        @(
            & $FilePath @Arguments 2>&1
        )

    $exitCode =
        $LASTEXITCODE

    foreach ($line in $commandOutput) {
        Write-Host $line
    }

    if ($exitCode -ne 0) {
        throw "$Description failed with exit code $exitCode."
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

if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($gitRoot)) {
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

$sourceCommit =
    (& git -C $ProjectRoot rev-parse HEAD).Trim()

if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($sourceCommit)) {
    throw 'Could not determine the source commit.'
}

$gitStatus =
    @(
        & git -C $ProjectRoot status --porcelain
    )

$workingTreeClean =
    $gitStatus.Count -eq 0

if (-not $workingTreeClean -and -not $AllowDirty) {
    throw @"
The working tree is not clean. Commit or stash changes before creating a release artifact.
Use -AllowDirty only for local packaging validation; dirty artifacts should not be published.
"@
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory =
        Join-Path `
            $ProjectRoot `
            'Artifacts\Releases'
}
else {
    $OutputDirectory =
        Get-AbsolutePath `
            -Path $OutputDirectory `
            -BasePath $ProjectRoot
}

$releaseDirectory =
    Join-Path `
        $OutputDirectory `
        "v$Version"

$stagingRoot =
    Join-Path `
        $releaseDirectory `
        'staging'

$helperPublishRoot =
    Join-Path `
        $releaseDirectory `
        'helper-publish'

$zipName =
    "DCML-v$Version.zip"

$zipPath =
    Join-Path `
        $releaseDirectory `
        $zipName

$checksumPath =
    Join-Path `
        $releaseDirectory `
        "DCML-v$Version.sha256"

$releaseInfoPath =
    Join-Path `
        $releaseDirectory `
        'release-info.json'

if (Test-Path -LiteralPath $releaseDirectory) {
    Remove-Item `
        -LiteralPath $releaseDirectory `
        -Recurse `
        -Force
}

New-Item `
    -ItemType Directory `
    -Path $stagingRoot `
    -Force |
    Out-Null

$loaderProject =
    Join-Path `
        $ProjectRoot `
        'src\DCML.Loader.MelonLoader\DCML.Loader.MelonLoader.csproj'

$testModuleProject =
    Join-Path `
        $ProjectRoot `
        'src\DCML.TestModule\DCML.TestModule.csproj'

$helperProject =
    Join-Path `
        $ProjectRoot `
        'src\DCML.Persistence.Helper\DCML.Persistence.Helper.csproj'

$sourceManifest =
    Join-Path `
        $ProjectRoot `
        'src\DCML.TestModule\manifest.json'

Assert-File -Path $loaderProject -Description 'MelonLoader host project'
Assert-File -Path $testModuleProject -Description 'TestModule project'
Assert-File -Path $helperProject -Description 'Persistence helper project'
Assert-File -Path $sourceManifest -Description 'TestModule manifest'

if (-not $SkipBuild) {
    Assert-Directory `
        -Path $DataCenterRoot `
        -Description 'Data Center installation root'

    Assert-File `
        -Path (Join-Path $DataCenterRoot 'MelonLoader\net6\MelonLoader.dll') `
        -Description 'MelonLoader assembly'

    Assert-File `
        -Path (Join-Path $DataCenterRoot 'MelonLoader\net6\0Harmony.dll') `
        -Description 'Harmony assembly'

    Invoke-ExternalCommand `
        -FilePath 'dotnet' `
        -Arguments @(
            'build',
            $loaderProject,
            '--configuration',
            'Release',
            "-p:DataCenterRoot=$DataCenterRoot"
        ) `
        -Description 'BUILD MELONLOADER HOST'

    Invoke-ExternalCommand `
        -FilePath 'dotnet' `
        -Arguments @(
            'build',
            $testModuleProject,
            '--configuration',
            'Release'
        ) `
        -Description 'BUILD TEST MODULE'

    Invoke-ExternalCommand `
        -FilePath 'dotnet' `
        -Arguments @(
            'publish',
            $helperProject,
            '--configuration',
            'Release',
            '--output',
            $helperPublishRoot,
            '--no-self-contained'
        ) `
        -Description 'PUBLISH PERSISTENCE HELPER'
}
else {
    New-Item `
        -ItemType Directory `
        -Path $helperPublishRoot `
        -Force |
        Out-Null

    $existingHelperOutput =
        Join-Path `
            $ProjectRoot `
            'src\DCML.Persistence.Helper\bin\Release\net8.0\publish'

    Assert-Directory `
        -Path $existingHelperOutput `
        -Description 'Existing persistence helper publish output'

    Copy-Item `
        -Path (Join-Path $existingHelperOutput '*') `
        -Destination $helperPublishRoot `
        -Recurse `
        -Force
}

$loaderDll =
    Join-Path `
        $ProjectRoot `
        'src\DCML.Loader.MelonLoader\bin\Release\net6.0\DCML.Loader.MelonLoader.dll'

$coreDll =
    Join-Path `
        $ProjectRoot `
        'src\DCML.Core\bin\Release\net6.0\DCML.Core.dll'

$dataCenterDll =
    Join-Path `
        $ProjectRoot `
        'src\DCML.DataCenter\bin\Release\net6.0\DCML.DataCenter.dll'

$dataCenterPersistenceDll =
    Join-Path `
        $ProjectRoot `
        'src\DCML.DataCenter.Persistence\bin\Release\net6.0\DCML.DataCenter.Persistence.dll'

$testModuleDll =
    Join-Path `
        $ProjectRoot `
        'src\DCML.TestModule\bin\Release\net6.0\DCML.TestModule.dll'

$requiredBuildFiles = @(
    @{ Path = $loaderDll; Description = 'MelonLoader host DLL' },
    @{ Path = $coreDll; Description = 'DCML.Core DLL' },
    @{ Path = $dataCenterDll; Description = 'DCML.DataCenter DLL' },
    @{ Path = $dataCenterPersistenceDll; Description = 'DCML.DataCenter.Persistence DLL' },
    @{ Path = $testModuleDll; Description = 'DCML.TestModule DLL' }
)

foreach ($required in $requiredBuildFiles) {
    Assert-File `
        -Path $required.Path `
        -Description $required.Description
}

$modsDirectory =
    Join-Path `
        $stagingRoot `
        'Mods'

$userLibsDirectory =
    Join-Path `
        $stagingRoot `
        'UserLibs'

$moduleDirectory =
    Join-Path `
        $stagingRoot `
        'UserData\DCML\Modules\DCML.TestModule'

$moduleHelperDirectory =
    Join-Path `
        $moduleDirectory `
        'PersistenceHelper'

@(
    $modsDirectory,
    $userLibsDirectory,
    $moduleDirectory,
    $moduleHelperDirectory
) |
    ForEach-Object {
        New-Item `
            -ItemType Directory `
            -Path $_ `
            -Force |
            Out-Null
    }

Copy-Item `
    -LiteralPath $loaderDll `
    -Destination (Join-Path $modsDirectory 'DCML.Loader.MelonLoader.dll') `
    -Force

Copy-Item `
    -LiteralPath $coreDll `
    -Destination (Join-Path $userLibsDirectory 'DCML.Core.dll') `
    -Force

Copy-Item `
    -LiteralPath $dataCenterDll `
    -Destination (Join-Path $userLibsDirectory 'DCML.DataCenter.dll') `
    -Force

Copy-Item `
    -LiteralPath $testModuleDll `
    -Destination (Join-Path $moduleDirectory 'DCML.TestModule.dll') `
    -Force

Copy-Item `
    -LiteralPath $dataCenterDll `
    -Destination (Join-Path $moduleDirectory 'DCML.DataCenter.dll') `
    -Force

Copy-Item `
    -LiteralPath $dataCenterPersistenceDll `
    -Destination (Join-Path $moduleDirectory 'DCML.DataCenter.Persistence.dll') `
    -Force

$manifest =
    Get-Content `
        -LiteralPath $sourceManifest `
        -Raw |
    ConvertFrom-Json

$manifest.version =
    $Version

$stagedManifestPath =
    Join-Path `
        $moduleDirectory `
        'manifest.json'

$manifestJson =
    $manifest |
    ConvertTo-Json `
        -Depth 32

[System.IO.File]::WriteAllText(
    $stagedManifestPath,
    $manifestJson + [Environment]::NewLine,
    [System.Text.UTF8Encoding]::new($false))

$helperFiles =
    @(
        Get-ChildItem `
            -LiteralPath $helperPublishRoot `
            -File |
        Where-Object {
            $_.Extension -notin @('.pdb', '.xml')
        }
    )

if ($helperFiles.Count -eq 0) {
    throw "Persistence helper publish output was empty: $helperPublishRoot"
}

foreach ($helperFile in $helperFiles) {
    Copy-Item `
        -LiteralPath $helperFile.FullName `
        -Destination (Join-Path $moduleHelperDirectory $helperFile.Name) `
        -Force
}

Assert-File `
    -Path (Join-Path $moduleHelperDirectory 'DCML.Persistence.Helper.dll') `
    -Description 'Packaged persistence helper DLL'

Assert-File `
    -Path (Join-Path $moduleHelperDirectory 'DCML.Persistence.Helper.deps.json') `
    -Description 'Packaged persistence helper deps file'

Assert-File `
    -Path (Join-Path $moduleHelperDirectory 'DCML.Persistence.Helper.runtimeconfig.json') `
    -Description 'Packaged persistence helper runtimeconfig'

Assert-File `
    -Path (Join-Path $moduleHelperDirectory 'System.Formats.Nrbf.dll') `
    -Description 'Packaged System.Formats.Nrbf dependency'

$packagedFiles =
    @(
        Get-ChildItem `
            -LiteralPath $stagingRoot `
            -File `
            -Recurse |
        Sort-Object FullName
    )

if ($packagedFiles.Count -eq 0) {
    throw 'Release staging directory contains no files.'
}

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item `
        -LiteralPath $zipPath `
        -Force
}

Write-Host "`n===== CREATE RELEASE ZIP =====" -ForegroundColor Cyan

Compress-Archive `
    -Path (Join-Path $stagingRoot '*') `
    -DestinationPath $zipPath `
    -CompressionLevel Optimal `
    -Force

Assert-File `
    -Path $zipPath `
    -Description 'Release ZIP'

$zipHash =
    (Get-FileHash `
        -LiteralPath $zipPath `
        -Algorithm SHA256).Hash.ToLowerInvariant()

[System.IO.File]::WriteAllText(
    $checksumPath,
    "$zipHash  $zipName" + [Environment]::NewLine,
    [System.Text.UTF8Encoding]::new($false))

$releaseInfo =
    [ordered]@{
        schemaVersion = 1
        version = $Version
        sourceCommit = $sourceCommit
        workingTreeClean = $workingTreeClean
        buildPerformed = -not $SkipBuild.IsPresent
        packageFile = $zipName
        packageSha256 = $zipHash
        packagedFileCount = $packagedFiles.Count
        generatedAtUtc = [DateTime]::UtcNow.ToString('o')
    }

$releaseInfoJson =
    $releaseInfo |
    ConvertTo-Json `
        -Depth 8

[System.IO.File]::WriteAllText(
    $releaseInfoPath,
    $releaseInfoJson + [Environment]::NewLine,
    [System.Text.UTF8Encoding]::new($false))

Write-Host "`n===== RELEASE PACKAGE COMPLETE =====" -ForegroundColor Green

[pscustomobject]@{
    Success = $true
    Version = $Version
    SourceCommit = $sourceCommit
    WorkingTreeClean = $workingTreeClean
    BuildPerformed = -not $SkipBuild.IsPresent
    ReleaseDirectory = $releaseDirectory
    StagingPath = $stagingRoot
    PackagePath = $zipPath
    ChecksumPath = $checksumPath
    ReleaseInfoPath = $releaseInfoPath
    PackageSha256 = $zipHash
    PackagedFileCount = $packagedFiles.Count
    StagedManifestVersion = $manifest.version
}
