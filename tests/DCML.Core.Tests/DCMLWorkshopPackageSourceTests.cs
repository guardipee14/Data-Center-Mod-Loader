using System;
using System.IO;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using DCML.DataCenter.PackageSources;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLWorkshopPackageSourceTests
{
    [Fact]
    public void Descriptor_UsesDataCenterWorkshopIdentity()
    {
        using var environment =
            new WorkshopEnvironment();

        var source =
            new DCMLWorkshopPackageSource(
                environment.WorkshopRoot);

        Assert.Equal(
            "4170200",
            DCMLWorkshopPackageSource.DataCenterSteamAppId);

        Assert.Equal(
            "datacenter.steam-workshop",
            source.Descriptor.Id);

        Assert.Equal(
            "steam-workshop",
            source.Descriptor.SourceType);

        Assert.True(
            source.Descriptor.HasCapability(
                DCMLPackageSourceCapabilities.Discovery));

        Assert.True(
            source.Descriptor.HasCapability(
                DCMLPackageSourceCapabilities.Staging));

        Assert.False(
            source.Descriptor.HasCapability(
                DCMLPackageSourceCapabilities.UpdateMetadata));
    }

    [Fact]
    public void DiscoverPackages_ReturnsMaterializedNumericItemsInOrder()
    {
        using var environment =
            new WorkshopEnvironment();

        Directory.CreateDirectory(
            Path.Combine(
                environment.WorkshopRoot,
                "200"));

        Directory.CreateDirectory(
            Path.Combine(
                environment.WorkshopRoot,
                "100"));

        var source =
            new DCMLWorkshopPackageSource(
                environment.WorkshopRoot);

        DCMLPackageSourceDiscoveryResult result =
            source.DiscoverPackages();

        Assert.True(
            result.Success);

        Assert.Equal(
            2,
            result.Entries.Count);

        Assert.Equal(
            "100",
            result.Entries[0].PackageKey);

        Assert.Equal(
            "200",
            result.Entries[1].PackageKey);
    }

    [Fact]
    public void DiscoverPackages_ReportsMissingRootWithoutCreatingIt()
    {
        using var environment =
            new WorkshopEnvironment();

        string missingRoot =
            Path.Combine(
                environment.Root,
                "missing-workshop");

        var source =
            new DCMLWorkshopPackageSource(
                missingRoot);

        DCMLPackageSourceDiscoveryResult result =
            source.DiscoverPackages();

        Assert.False(
            result.Success);

        Assert.False(
            Directory.Exists(
                missingRoot));

        Assert.Equal(
            "DCML_WORKSHOP_ROOT_NOT_FOUND",
            Assert.Single(
                result.Issues).Code);
    }

    [Fact]
    public void DiscoverPackages_ReportsNonNumericItemDirectory()
    {
        using var environment =
            new WorkshopEnvironment();

        Directory.CreateDirectory(
            Path.Combine(
                environment.WorkshopRoot,
                "not-an-item"));

        var source =
            new DCMLWorkshopPackageSource(
                environment.WorkshopRoot);

        DCMLPackageSourceDiscoveryResult result =
            source.DiscoverPackages();

        Assert.False(
            result.Success);

        Assert.Empty(
            result.Entries);

        Assert.Equal(
            "DCML_WORKSHOP_ITEM_ID_INVALID",
            Assert.Single(
                result.Issues).Code);
    }

    [Fact]
    public void StagePackage_CopiesAvailableItemRecursively()
    {
        using var environment =
            new WorkshopEnvironment();

        string itemDirectory =
            environment.CreateItem(
                "12345");

        File.WriteAllText(
            Path.Combine(
                itemDirectory,
                "manifest.json"),
            "{}");

        string nested =
            Path.Combine(
                itemDirectory,
                "content");

        Directory.CreateDirectory(
            nested);

        File.WriteAllText(
            Path.Combine(
                nested,
                "module.bin"),
            "probe");

        var source =
            new DCMLWorkshopPackageSource(
                environment.WorkshopRoot);

        var entry =
            new DCMLPackageSourceEntry(
                source.Descriptor.Id,
                "12345");

        DCMLPackageStageResult result =
            source.StagePackage(
                entry,
                environment.StagingRoot);

        Assert.True(
            result.Success);

        Assert.NotNull(
            result.StagedPackageDirectory);

        Assert.True(
            File.Exists(
                Path.Combine(
                    result.StagedPackageDirectory!,
                    "manifest.json")));

        Assert.Equal(
            "probe",
            File.ReadAllText(
                Path.Combine(
                    result.StagedPackageDirectory!,
                    "content",
                    "module.bin")));
    }

    [Fact]
    public void StagePackage_RejectsEntryFromDifferentSource()
    {
        using var environment =
            new WorkshopEnvironment();

        environment.CreateItem(
            "12345");

        var source =
            new DCMLWorkshopPackageSource(
                environment.WorkshopRoot);

        var entry =
            new DCMLPackageSourceEntry(
                "another.source",
                "12345");

        DCMLPackageStageResult result =
            source.StagePackage(
                entry,
                environment.StagingRoot);

        Assert.False(
            result.Success);

        Assert.Equal(
            "DCML_WORKSHOP_SOURCE_MISMATCH",
            result.ErrorCode);
    }

    [Fact]
    public void StagePackage_RefusesToOverwriteExistingTarget()
    {
        using var environment =
            new WorkshopEnvironment();

        environment.CreateItem(
            "12345");

        string existingTarget =
            Path.Combine(
                environment.StagingRoot,
                "12345");

        Directory.CreateDirectory(
            existingTarget);

        string markerPath =
            Path.Combine(
                existingTarget,
                "keep.txt");

        File.WriteAllText(
            markerPath,
            "keep");

        var source =
            new DCMLWorkshopPackageSource(
                environment.WorkshopRoot);

        var entry =
            new DCMLPackageSourceEntry(
                source.Descriptor.Id,
                "12345");

        DCMLPackageStageResult result =
            source.StagePackage(
                entry,
                environment.StagingRoot);

        Assert.False(
            result.Success);

        Assert.Equal(
            "DCML_WORKSHOP_STAGE_TARGET_EXISTS",
            result.ErrorCode);

        Assert.Equal(
            "keep",
            File.ReadAllText(
                markerPath));
    }

    private sealed class WorkshopEnvironment :
        IDisposable
    {
        public WorkshopEnvironment()
        {
            Root =
                Path.Combine(
                    Path.GetTempPath(),
                    "DCML-Workshop-" +
                    Guid.NewGuid().ToString("N"));

            WorkshopRoot =
                Path.Combine(
                    Root,
                    "workshop");

            StagingRoot =
                Path.Combine(
                    Root,
                    "staging");

            Directory.CreateDirectory(
                WorkshopRoot);
        }

        public string Root { get; }

        public string WorkshopRoot { get; }

        public string StagingRoot { get; }

        public string CreateItem(
            string itemId)
        {
            string itemDirectory =
                Path.Combine(
                    WorkshopRoot,
                    itemId);

            Directory.CreateDirectory(
                itemDirectory);

            return
                itemDirectory;
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(
                    Root,
                    true);
            }
        }
    }
}
