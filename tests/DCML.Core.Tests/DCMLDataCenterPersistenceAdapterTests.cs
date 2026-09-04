using System;
using System.IO;
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
            "DataCenterProcessCablePersistenceSourceFactory.Create(",
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

    [Fact]
    public void PersistenceSettings_DefaultToDisabledAndPathFree()
    {
        var settings =
            new DataCenterProcessCablePersistenceSettings();

        Assert.False(
            settings.Enabled);

        Assert.False(
            settings.HasRequiredPaths);

        Assert.Equal(
            string.Empty,
            settings.SavePath);

        Assert.Equal(
            string.Empty,
            settings.HelperHostPath);

        Assert.Equal(
            string.Empty,
            settings.HelperDllPath);
    }

    [Fact]
    public void PersistenceFactory_DisabledSettingsDoNotCreateSource()
    {
        var settings =
            new DataCenterProcessCablePersistenceSettings
            {
                Enabled =
                    false,

                SavePath =
                    "save.data",

                HelperHostPath =
                    "dotnet",

                HelperDllPath =
                    "DCML.Persistence.Helper.dll"
            };

        Assert.Null(
            DataCenterProcessCablePersistenceSourceFactory.Create(
                settings));
    }

    [Fact]
    public void PersistenceFactory_IncompleteEnabledSettingsDoNotCreateSource()
    {
        var settings =
            new DataCenterProcessCablePersistenceSettings
            {
                Enabled =
                    true,

                SavePath =
                    "save.data"
            };

        Assert.False(
            settings.HasRequiredPaths);

        Assert.Null(
            DataCenterProcessCablePersistenceSourceFactory.Create(
                settings));
    }

    [Fact]
    public void PersistenceFactory_CompleteEnabledSettingsCreateExplicitSource()
    {
        string savePath =
            Path.Combine(
                "production-config",
                "selected-save.data");

        var settings =
            new DataCenterProcessCablePersistenceSettings
            {
                Enabled =
                    true,

                SavePath =
                    savePath,

                HelperHostPath =
                    "dotnet",

                HelperDllPath =
                    "DCML.Persistence.Helper.dll"
            };

        IDataCenterCablePersistenceSource? source =
            DataCenterProcessCablePersistenceSourceFactory.Create(
                settings);

        Assert.NotNull(
            source);

        Assert.Equal(
            Path.GetFullPath(
                savePath),
            source!.SourcePath);
    }

    [Fact]
    public void ProcessSource_RejectsBlankExplicitPaths()
    {
        Action[] invalidConstructors =
        {
            () =>
                new DataCenterProcessCablePersistenceSource(
                    string.Empty,
                    "helper.dll",
                    "save.data"),

            () =>
                new DataCenterProcessCablePersistenceSource(
                    "dotnet",
                    string.Empty,
                    "save.data"),

            () =>
                new DataCenterProcessCablePersistenceSource(
                    "dotnet",
                    "helper.dll",
                    string.Empty)
        };

        foreach (Action invalidConstructor in invalidConstructors)
        {
            Assert.Throws<ArgumentException>(
                invalidConstructor);
        }
    }

    [Fact]
    public void PersistenceConfiguration_DocumentUsesModuleOwnedConfigAndPathFreeDefaults()
    {
        string root =
            GetSolutionRoot();

        string documentation =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "docs",
                    "DATACENTER-PERSISTENCE.md"));

        Assert.Contains(
            @"UserData\DCML\Data\<module-id>\config.json",
            documentation,
            StringComparison.Ordinal);

        Assert.Contains(
            "Enabled = false",
            documentation,
            StringComparison.Ordinal);

        Assert.Contains(
            "SavePath = string.Empty",
            documentation,
            StringComparison.Ordinal);

        Assert.Contains(
            "release packages must not contain machine-specific",
            documentation,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TestModule_PreservesExistingPersistenceConfigSchemaWhileUsingFactory()
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
            "public bool EnablePhysicalCablePersistenceSource { get; set; }",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "public string PhysicalCableSavePath { get; set; }",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "public string PhysicalCableHelperHostPath { get; set; }",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "public string PhysicalCableHelperDllPath { get; set; }",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "new DataCenterProcessCablePersistenceSettings",
            source,
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
