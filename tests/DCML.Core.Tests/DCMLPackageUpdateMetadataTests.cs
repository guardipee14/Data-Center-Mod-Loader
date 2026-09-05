using System;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLPackageUpdateMetadataTests
{
    [Fact]
    public void Metadata_NormalizesAndPreservesDescriptiveState()
    {
        var metadata =
            new DCMLPackageUpdateMetadata(
                " source.test ",
                " package-a ",
                " module.a ",
                "1.2.3",
                "0.0.6",
                true,
                new[]
                {
                    new DCMLPackageUpdateDependency(
                        "dependency.a",
                        "2.0.0"),
                    new DCMLPackageUpdateDependency(
                        "dependency.optional",
                        null,
                        true)
                });

        Assert.Equal(
            "source.test",
            metadata.SourceId);

        Assert.Equal(
            "package-a",
            metadata.PackageKey);

        Assert.Equal(
            "module.a",
            metadata.ModuleId);

        Assert.Equal(
            "1.2.3",
            metadata.Version);

        Assert.Equal(
            "0.0.6",
            metadata.MinimumDCMLVersion);

        Assert.True(
            metadata.RequiresRestart);

        Assert.Equal(
            2,
            metadata.Dependencies.Count);

        Assert.True(
            metadata.Dependencies[1].Optional);
    }

    [Fact]
    public void Metadata_RejectsInvalidPackageVersion()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new DCMLPackageUpdateMetadata(
                    "source.test",
                    "package-a",
                    "module.a",
                    "not-a-version"));
    }

    [Fact]
    public void Metadata_RejectsInvalidMinimumDCMLVersion()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new DCMLPackageUpdateMetadata(
                    "source.test",
                    "package-a",
                    "module.a",
                    "1.0.0",
                    "latest"));
    }

    [Fact]
    public void Dependency_RejectsInvalidMinimumVersion()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new DCMLPackageUpdateDependency(
                    "dependency.a",
                    "2"));
    }

    [Fact]
    public void Metadata_RejectsDuplicateDependencyIdsCaseInsensitively()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new DCMLPackageUpdateMetadata(
                    "source.test",
                    "package-a",
                    "module.a",
                    "1.0.0",
                    dependencies:
                        new[]
                        {
                            new DCMLPackageUpdateDependency(
                                "dependency.a"),
                            new DCMLPackageUpdateDependency(
                                "DEPENDENCY.A")
                        }));
    }

    [Fact]
    public void Result_SucceededCarriesMetadataIdentity()
    {
        var metadata =
            new DCMLPackageUpdateMetadata(
                "source.test",
                "package-a",
                "module.a",
                "1.0.0");

        DCMLPackageUpdateMetadataResult result =
            DCMLPackageUpdateMetadataResult.Succeeded(
                metadata);

        Assert.True(
            result.Success);

        Assert.Same(
            metadata,
            result.Metadata);

        Assert.Equal(
            "source.test",
            result.SourceId);

        Assert.Equal(
            "package-a",
            result.PackageKey);

        Assert.Null(
            result.ErrorCode);
    }

    [Fact]
    public void Result_FailedCarriesStableErrorWithoutMetadata()
    {
        DCMLPackageUpdateMetadataResult result =
            DCMLPackageUpdateMetadataResult.Failed(
                "source.test",
                "package-a",
                "DCML_UPDATE_METADATA_UNAVAILABLE",
                "Update metadata is unavailable.");

        Assert.False(
            result.Success);

        Assert.Null(
            result.Metadata);

        Assert.Equal(
            "DCML_UPDATE_METADATA_UNAVAILABLE",
            result.ErrorCode);

        Assert.Equal(
            "Update metadata is unavailable.",
            result.ErrorMessage);
    }

    [Fact]
    public void UpdateMetadataSourceContract_IsDescriptiveOnly()
    {
        IDCMLPackageUpdateMetadataSource source =
            new ProbeUpdateMetadataSource();

        var entry =
            new DCMLPackageSourceEntry(
                source.Descriptor.Id,
                "package-a");

        DCMLPackageUpdateMetadataResult result =
            source.GetUpdateMetadata(
                entry);

        Assert.True(
            source.Descriptor.HasCapability(
                DCMLPackageSourceCapabilities.UpdateMetadata));

        Assert.True(
            result.Success);

        Assert.NotNull(
            result.Metadata);

        Assert.Equal(
            "2.0.0",
            result.Metadata!.Version);
    }

    private sealed class ProbeUpdateMetadataSource :
        IDCMLPackageUpdateMetadataSource
    {
        public DCMLPackageSourceDescriptor Descriptor { get; } =
            new DCMLPackageSourceDescriptor(
                "source.probe",
                "Probe Source",
                "test",
                DCMLPackageSourceCapabilities.Discovery |
                DCMLPackageSourceCapabilities.UpdateMetadata);

        public DCMLPackageSourceDiscoveryResult DiscoverPackages()
        {
            return
                new DCMLPackageSourceDiscoveryResult(
                    Descriptor.Id,
                    new[]
                    {
                        new DCMLPackageSourceEntry(
                            Descriptor.Id,
                            "package-a")
                    });
        }

        public DCMLPackageUpdateMetadataResult GetUpdateMetadata(
            DCMLPackageSourceEntry entry)
        {
            return
                DCMLPackageUpdateMetadataResult.Succeeded(
                    new DCMLPackageUpdateMetadata(
                        Descriptor.Id,
                        entry.PackageKey,
                        "module.a",
                        "2.0.0"));
        }
    }
}
