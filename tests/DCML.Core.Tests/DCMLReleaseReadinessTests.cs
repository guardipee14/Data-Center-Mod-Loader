using System;
using System.IO;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLReleaseReadinessTests
{
    [Fact]
    public void ReadinessTool_ValidatesReleaseArtifactBeforePolicyChecks()
    {
        string source =
            ReadTool();

        Assert.Contains(
            "tools\\Test-DCMLReleaseArtifact.ps1",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "-ExpectedSourceCommit $resolvedReleaseCommit",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "ArtifactValidated = [bool]$artifactValidation.Success",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ReadinessTool_ClassifiesRuntimeSourcesAndReleaseBuilderAsRuntimeFacing()
    {
        string source =
            ReadTool();

        Assert.Contains(
            "'src/'",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "'tools/Build-DCMLRelease.ps1'",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "Reason = 'runtime-source'",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "Reason = 'release-package-layout'",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ReadinessTool_ClassifiesRepositorySupportContentAsNonRuntime()
    {
        string source =
            ReadTool();

        Assert.Contains(
            "'docs/'",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "'tests/'",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "'examples/'",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "'.github/'",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "'tools/Test-DCMLReleaseArtifact.ps1'",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "'tools/Test-DCMLReleaseReadiness.ps1'",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ReadinessTool_FailsSafeForUnclassifiedPaths()
    {
        string source =
            ReadTool();

        Assert.Contains(
            "Reason = 'conservative-unclassified'",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "RuntimeFacing = $true",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ReadinessTool_UsesGitRangeUnlessExplicitTestOverrideIsAllowed()
    {
        string source =
            ReadTool();

        Assert.Contains(
            "BaseCommit is required unless the explicit ChangedPath test override is used.",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "git -C $ProjectRoot diff --name-only $diffRange --",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "ChangedPath override requires -AllowChangedPathOverride",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ReadinessTool_RequiresLiveProofForRuntimeFacingChanges()
    {
        string source =
            ReadTool();

        Assert.Contains(
            "$liveProofRequired =",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "Live Data Center proof is required for runtime-facing release changes.",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "'live-proof.json'",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ReadinessTool_BindsLiveProofToReleaseCommitAndPackage()
    {
        string source =
            ReadTool();

        Assert.Contains(
            "Live proof source commit does not match the release commit.",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "Live proof package SHA-256 does not match the validated release artifact.",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "$artifactValidation.ActualSha256",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ReadinessTool_RequiresPassedDataCenterEvidenceMetadata()
    {
        string source =
            ReadTool();

        Assert.Contains(
            "Live proof result must be 'passed'.",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "Live proof game must be 'Data Center'.",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "Live proof observedAtUtc must be a valid ISO-8601 timestamp.",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "Live proof summary cannot be empty.",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ReleaseChecklist_DocumentsConditionalLiveProofPolicy()
    {
        string root =
            GetSolutionRoot();

        string checklistPath =
            Path.Combine(
                root,
                "docs",
                "RELEASE-CHECKLIST.md");

        Assert.True(
            File.Exists(checklistPath),
            $"Expected release checklist was not found: {checklistPath}");

        string checklist =
            File.ReadAllText(
                checklistPath);

        Assert.Contains(
            "Live proof is required only when runtime-facing changes are present.",
            checklist,
            StringComparison.Ordinal);

        Assert.Contains(
            "packageSha256",
            checklist,
            StringComparison.Ordinal);

        Assert.Contains(
            "Test-DCMLReleaseReadiness.ps1",
            checklist,
            StringComparison.Ordinal);
    }

    private static string ReadTool()
    {
        string root =
            GetSolutionRoot();

        string path =
            Path.Combine(
                root,
                "tools",
                "Test-DCMLReleaseReadiness.ps1");

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
