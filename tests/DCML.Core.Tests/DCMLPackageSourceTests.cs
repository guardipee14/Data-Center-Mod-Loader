using System;
using System.Collections.Generic;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLPackageSourceTests
{
    [Fact]
    public void Descriptor_NormalizesIdentityAndReportsCapabilities()
    {
        var descriptor =
            new DCMLPackageSourceDescriptor(
                " source.test ",
                " Test Source ",
                " custom ",
                DCMLPackageSourceCapabilities.Discovery |
                DCMLPackageSourceCapabilities.Staging);

        Assert.Equal("source.test", descriptor.Id);
        Assert.Equal("Test Source", descriptor.DisplayName);
        Assert.Equal("custom", descriptor.SourceType);
        Assert.True(descriptor.HasCapability(DCMLPackageSourceCapabilities.Discovery));
        Assert.True(descriptor.HasCapability(DCMLPackageSourceCapabilities.Staging));
        Assert.False(descriptor.HasCapability(DCMLPackageSourceCapabilities.UpdateMetadata));
    }

    [Fact]
    public void Descriptor_RequiresDiscoveryCapability()
    {
        Assert.Throws<ArgumentException>(
            () => new DCMLPackageSourceDescriptor(
                "source.test",
                "Test Source",
                "custom",
                DCMLPackageSourceCapabilities.Staging));
    }

    [Fact]
    public void DiscoveryResult_PreservesEntriesAndIssues()
    {
        var entries = new[]
        {
            new DCMLPackageSourceEntry("source.test", "package-a")
        };

        var issues = new[]
        {
            new DCMLPackageSourceIssue(
                "source.test",
                "package-b",
                "DCML_SOURCE_PACKAGE_UNAVAILABLE",
                "The package entry is unavailable.")
        };

        var result = new DCMLPackageSourceDiscoveryResult(
            "source.test",
            entries,
            issues);

        Assert.False(result.Success);
        Assert.Single(result.Entries);
        Assert.Single(result.Issues);
        Assert.Equal("package-a", result.Entries[0].PackageKey);
        Assert.Equal("DCML_SOURCE_PACKAGE_UNAVAILABLE", result.Issues[0].Code);
    }

    [Fact]
    public void PackageSourceContract_SupportsReadOnlyDiscoveryOnly()
    {
        IDCMLPackageSource source = new ProbePackageSource();
        DCMLPackageSourceDiscoveryResult result = source.DiscoverPackages();

        Assert.True(result.Success);
        Assert.Single(result.Entries);
        Assert.Equal("probe-package", result.Entries[0].PackageKey);
        Assert.Equal(
            DCMLPackageSourceCapabilities.Discovery,
            source.Descriptor.Capabilities);
    }

    private sealed class ProbePackageSource : IDCMLPackageSource
    {
        public DCMLPackageSourceDescriptor Descriptor { get; } =
            new DCMLPackageSourceDescriptor(
                "source.probe",
                "Probe Source",
                "test",
                DCMLPackageSourceCapabilities.Discovery);

        public DCMLPackageSourceDiscoveryResult DiscoverPackages()
        {
            return new DCMLPackageSourceDiscoveryResult(
                Descriptor.Id,
                new List<DCMLPackageSourceEntry>
                {
                    new DCMLPackageSourceEntry(Descriptor.Id, "probe-package")
                });
        }
    }
}
