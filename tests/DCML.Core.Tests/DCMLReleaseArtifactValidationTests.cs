using System;
using System.IO;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLReleaseArtifactValidationTests
{
    [Fact]
    public void Validator_RequiresReleaseMetadataAndChecksumFiles()
    {
        string source =
            ReadTool(
                "Test-DCMLReleaseArtifact.ps1");

        Assert.Contains(
            "'release-info.json'",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "\"DCML-v$version.sha256\"",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "Assert-File",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Validator_RequiresExpectedSourceCommitMatch()
    {
        string source =
            ReadTool(
                "Test-DCMLReleaseArtifact.ps1");

        Assert.Contains(
            "$metadataSourceCommit",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "$ExpectedSourceCommit",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "Release source commit does not match the expected source commit.",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Validator_RecomputesActualZipSha256()
    {
        string source =
            ReadTool(
                "Test-DCMLReleaseArtifact.ps1");

        Assert.Contains(
            "Get-FileHash",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "-Algorithm SHA256",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "$actualPackageSha256",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Validator_RequiresMetadataAndChecksumHashesToMatchActualZip()
    {
        string source =
            ReadTool(
                "Test-DCMLReleaseArtifact.ps1");

        Assert.Contains(
            "Release ZIP SHA-256 does not match release-info.json.",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "Release SHA-256 file does not match the actual ZIP.",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "$checksumSha256",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ReleaseBuilder_InvokesArtifactValidatorAutomatically()
    {
        string source =
            ReadTool(
                "Build-DCMLRelease.ps1");

        Assert.Contains(
            "'tools\\Test-DCMLReleaseArtifact.ps1'",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "===== VALIDATE RELEASE ARTIFACT =====",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "& $validatorScript",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ReleaseBuilder_PassesCapturedSourceCommitIntoValidator()
    {
        string source =
            ReadTool(
                "Build-DCMLRelease.ps1");

        Assert.Contains(
            "-ExpectedSourceCommit $sourceCommit",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "ArtifactValidated =",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Validator_DeclaresTheDataCenterSharedAssemblyPair()
    {
        string source =
            ReadTool(
                "Test-DCMLReleaseArtifact.ps1");

        Assert.Contains(
            "UserLibs/DCML.DataCenter.dll",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "UserData/DCML/Modules/DCML.TestModule/DCML.DataCenter.dll",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Validator_HashesSharedAssembliesFromTheReleaseZip()
    {
        string source =
            ReadTool(
                "Test-DCMLReleaseArtifact.ps1");

        Assert.Contains(
            "Get-ZipEntrySha256",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "System.IO.Compression.ZipFile",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "foreach ($candidate in $Archive.Entries)",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "Release ZIP entry lookup returned no entry after an exact path match:",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "return $foundEntry",
            source,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "return\n        $foundEntry",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "SHA256",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Validator_FailsClosedWhenSharedAssemblyHashesDiffer()
    {
        string source =
            ReadTool(
                "Test-DCMLReleaseArtifact.ps1");

        Assert.Contains(
            "Shared assembly hash mismatch.",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "$primarySha256",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "$secondarySha256",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Validator_ReportsSharedAssemblyGateResults()
    {
        string source =
            ReadTool(
                "Test-DCMLReleaseArtifact.ps1");

        Assert.Contains(
            "SharedAssemblyChecksPassed = $true",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "SharedAssemblyCheckCount = $sharedAssemblyChecks.Count",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "SharedAssemblyChecks = $sharedAssemblyChecks",
            source,
            StringComparison.Ordinal);
    }

    private static string ReadTool(
        string fileName)
    {
        string root =
            GetSolutionRoot();

        string path =
            Path.Combine(
                root,
                "tools",
                fileName);

        Assert.True(
            File.Exists(path),
            $"Expected tool was not found: {path}");

        return
            File.ReadAllText(
                path);
    }

    private static string GetSolutionRoot()
    {
        DirectoryInfo? directory =
            new DirectoryInfo(
                AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (
                File.Exists(
                    Path.Combine(
                        directory.FullName,
                        "DCML.sln"))
            )
            {
                return
                    directory.FullName;
            }

            directory =
                directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the DCML solution root.");
    }
}
