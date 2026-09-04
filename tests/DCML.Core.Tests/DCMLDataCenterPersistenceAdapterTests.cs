using System;
using System.IO;
using System.Linq;
using DCML.DataCenter.Abstractions;
using DCML.DataCenter.Persistence;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLDataCenterPersistenceAdapterTests
{
    [Fact]
    public void ProcessSource_ImplementsPersistenceSourceContract()
    {
        Assert.True(
            typeof(IDataCenterCablePersistenceSource)
                .IsAssignableFrom(
                    typeof(DataCenterProcessCablePersistenceSource)));
    }

    [Fact]
    public void ProcessSource_PreservesExplicitSaveSelection()
    {
        string relativeSavePath =
            Path.Combine(
                "explicit-save-selection",
                "chosen-save.data");

        var source =
            new DataCenterProcessCablePersistenceSource(
                "dotnet",
                "DCML.Persistence.Helper.dll",
                relativeSavePath);

        Assert.Equal(
            Path.GetFullPath(
                relativeSavePath),
            source.SourcePath);
    }

    [Fact]
    public void PersistenceAdapter_ProjectDoesNotReferenceHelperProject()
    {
        string root =
            GetSolutionRoot();

        string project =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "DCML.DataCenter.Persistence",
                    "DCML.DataCenter.Persistence.csproj"));

        Assert.Contains(
            @"..\DCML.DataCenter\DCML.DataCenter.csproj",
            project,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "DCML.Persistence.Helper",
            project,
            StringComparison.Ordinal);
    }

    [Fact]
    public void PersistenceAdapter_DoesNotDiscoverOrChooseSaveFiles()
    {
        string root =
            GetSolutionRoot();

        string source =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "DCML.DataCenter.Persistence",
                    "DataCenterProcessCablePersistenceSource.cs"));

        string[] forbiddenSelectionApis =
        {
            "Directory.GetFiles",
            "Directory.EnumerateFiles",
            "Directory.GetFileSystemEntries",
            "Directory.EnumerateFileSystemEntries",
            "LastWriteTimeUtc)",
            "OrderByDescending",
            "MaxBy("
        };

        foreach (string forbidden in forbiddenSelectionApis)
        {
            Assert.DoesNotContain(
                forbidden,
                source,
                StringComparison.Ordinal);
        }

        Assert.Matches(
            @"startInfo\.ArgumentList\.Add\(\s*SourcePath\s*\);",
            source);
    }

    [Fact]
    public void TestModule_UsesReusablePersistenceAdapter()
    {
        string root =
            GetSolutionRoot();

        string source =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "DCML.TestModule",
                    "TestModule.cs"));

        Assert.Contains(
            "new DataCenterProcessCablePersistenceSource(",
            source,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "private sealed class ProcessCablePersistenceSource",
            source,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "using System.Diagnostics;",
            source,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "using System.Text.Json;",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void PersistenceAdapter_IsNet6HostBoundary()
    {
        string root =
            GetSolutionRoot();

        string dataCenterProject =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "DCML.DataCenter",
                    "DCML.DataCenter.csproj"));

        string adapterProject =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "DCML.DataCenter.Persistence",
                    "DCML.DataCenter.Persistence.csproj"));

        Assert.Contains(
            "<TargetFrameworks>netstandard2.1;net6.0</TargetFrameworks>",
            dataCenterProject,
            StringComparison.Ordinal);

        Assert.Contains(
            "<TargetFramework>net6.0</TargetFramework>",
            adapterProject,
            StringComparison.Ordinal);
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
                        "DCML.sln")))
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
