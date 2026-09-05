using System;
using System.IO;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLProviderRestrictionPolicyTests
{
    [Fact]
    public void ProviderRestrictionTool_DefinesFailClosedProviderRules()
    {
        string source =
            ReadRepositoryFile(
                "tools",
                "Test-DCMLProviderRestrictions.ps1");

        Assert.Contains(
            "src\\DCML.Core",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "src\\DCML.DataCenter\\PackageSources",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "HttpClient",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "Process\\.Start",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "SubscribeItem|UnsubscribeItem|DownloadItem",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "steam_api",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "Provider/platform restriction validation failed.",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ReleaseReadiness_RequiresProviderRestrictionValidation()
    {
        string source =
            ReadRepositoryFile(
                "tools",
                "Test-DCMLReleaseReadiness.ps1");

        Assert.Contains(
            "Test-DCMLProviderRestrictions.ps1",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "$providerRestrictionValidation",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "ProviderRestrictionsValidated = [bool]$providerRestrictionValidation.Success",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void CiWorkflow_RunsProviderRestrictionGate()
    {
        string workflow =
            ReadRepositoryFile(
                ".github",
                "workflows",
                "ci.yml");

        Assert.Contains(
            "Enforce provider restrictions",
            workflow,
            StringComparison.Ordinal);

        Assert.Contains(
            "./tools/Test-DCMLProviderRestrictions.ps1",
            workflow,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ReleaseChecklist_DocumentsProviderRestrictionGate()
    {
        string checklist =
            ReadRepositoryFile(
                "docs",
                "RELEASE-CHECKLIST.md");

        Assert.Contains(
            "ProviderRestrictionsValidated",
            checklist,
            StringComparison.Ordinal);

        Assert.Contains(
            "Test-DCMLProviderRestrictions.ps1",
            checklist,
            StringComparison.Ordinal);

        Assert.Contains(
            "platform/provider restriction",
            checklist,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadRepositoryFile(
        params string[] pathParts)
    {
        string root =
            GetSolutionRoot();

        string path =
            root;

        foreach (string part in pathParts)
        {
            path =
                Path.Combine(
                    path,
                    part);
        }

        Assert.True(
            File.Exists(path),
            $"Expected repository file was not found: {path}");

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
