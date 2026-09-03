using System;
using System.IO;
using DCML.Core.Abstractions;
using DCML.Core.Services;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLVersionedCapabilityTests
{
    [Fact]
    public void Descriptor_NormalizesIdAndVersion()
    {
        var descriptor =
            new DCMLCapabilityDescriptor(
                "  dcml.events  ",
                "1.0.0");

        Assert.Equal(
            "dcml.events",
            descriptor.Id);

        Assert.Equal(
            "1.0.0",
            descriptor.Version);
    }

    [Fact]
    public void Descriptor_RejectsInvalidSemanticVersion()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new DCMLCapabilityDescriptor(
                    "dcml.events",
                    "1.0"));
    }

    [Fact]
    public void LegacyConstructor_AssignsV1ToCapabilities()
    {
        var info =
            CreateLegacyRuntimeInfo();

        Assert.True(
            info.TryGetCapabilityVersion(
                DCMLRuntimeCapabilities.Events,
                out string? version));

        Assert.Equal(
            DCMLCapabilityVersions.V1,
            version);
    }

    [Fact]
    public void Catalog_TryGetCapabilityVersion_IsCaseInsensitive()
    {
        var info =
            CreateLegacyRuntimeInfo();

        Assert.True(
            info.TryGetCapabilityVersion(
                "DCML.EVENTS",
                out string? version));

        Assert.Equal(
            "1.0.0",
            version);
    }

    [Fact]
    public void Catalog_SupportsCapability_EqualMinimum()
    {
        var info =
            CreateLegacyRuntimeInfo();

        Assert.True(
            info.SupportsCapability(
                DCMLRuntimeCapabilities.Events,
                "1.0.0"));
    }

    [Fact]
    public void Catalog_SupportsCapability_LowerMinimum()
    {
        var info =
            CreateLegacyRuntimeInfo();

        Assert.True(
            info.SupportsCapability(
                DCMLRuntimeCapabilities.Events,
                "0.9.9"));
    }

    [Fact]
    public void Catalog_SupportsCapability_ReturnsFalseForHigherMinimum()
    {
        var info =
            CreateLegacyRuntimeInfo();

        Assert.False(
            info.SupportsCapability(
                DCMLRuntimeCapabilities.Events,
                "1.0.1"));
    }

    [Fact]
    public void Catalog_SupportsCapability_ReturnsFalseForInvalidMinimum()
    {
        var info =
            CreateLegacyRuntimeInfo();

        Assert.False(
            info.SupportsCapability(
                DCMLRuntimeCapabilities.Events,
                "version-one"));
    }

    [Fact]
    public void VersionedConstructor_DeduplicatesEquivalentCapabilities()
    {
        var info =
            new DCMLRuntimeInfo(
                "dcml.test",
                "0.0.3",
                "TestHost",
                "1.2.3",
                "Data Center",
                @"C:\Game",
                new[]
                {
                    new DCMLCapabilityDescriptor(
                        "dcml.events",
                        "1.0.0"),
                    new DCMLCapabilityDescriptor(
                        "DCML.EVENTS",
                        "1.0.0")
                });

        Assert.Single(
            info.CapabilityDescriptors);

        Assert.Single(
            info.Capabilities);
    }

    [Fact]
    public void VersionedConstructor_RejectsConflictingCapabilityVersions()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new DCMLRuntimeInfo(
                    "dcml.test",
                    "0.0.3",
                    "TestHost",
                    "1.2.3",
                    "Data Center",
                    @"C:\Game",
                    new[]
                    {
                        new DCMLCapabilityDescriptor(
                            "dcml.events",
                            "1.0.0"),
                        new DCMLCapabilityDescriptor(
                            "DCML.EVENTS",
                            "2.0.0")
                    }));
    }

    [Fact]
    public void RuntimeCapabilities_HasStableCatalogIdentifier()
    {
        Assert.Equal(
            "dcml.runtime-capabilities",
            DCMLRuntimeCapabilities.RuntimeCapabilities);
    }

    [Fact]
    public void MelonModuleContext_RegistersCapabilityCatalog()
    {
        string root =
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    ".."));

        string path =
            Path.Combine(
                root,
                "src",
                "DCML.Loader.MelonLoader",
                "MelonModuleContext.cs");

        string source =
            File.ReadAllText(
                path);

        Assert.Contains(
            "typeof(IDCMLCapabilityCatalog)",
            source);

        Assert.Contains(
            "DCMLRuntimeCapabilities.RuntimeCapabilities",
            source);
    }

    private static DCMLRuntimeInfo CreateLegacyRuntimeInfo()
    {
        return
            new DCMLRuntimeInfo(
                "dcml.test",
                "0.0.3",
                "TestHost",
                "1.2.3",
                "Data Center",
                @"C:\Game",
                new[]
                {
                    DCMLRuntimeCapabilities.Logging,
                    DCMLRuntimeCapabilities.Events
                });
    }
}
