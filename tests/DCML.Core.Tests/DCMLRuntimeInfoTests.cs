using System;
using DCML.Core.Abstractions;
using DCML.Core.Services;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLRuntimeInfoTests
{
    [Fact]
    public void Constructor_ExposesRuntimeValues()
    {
        var info =
            CreateRuntimeInfo();

        Assert.Equal(
            "dcml.test.lifecycle",
            info.ModuleId);

        Assert.Equal(
            "0.0.1",
            info.DCMLVersion);

        Assert.Equal(
            "TestHost",
            info.HostName);

        Assert.Equal(
            "1.2.3",
            info.HostVersion);

        Assert.Equal(
            "Data Center",
            info.GameName);

        Assert.Equal(
            @"C:\Game",
            info.GameRoot);
    }

    [Fact]
    public void HasCapability_IsCaseInsensitive()
    {
        var info =
            CreateRuntimeInfo();

        Assert.True(
            info.HasCapability(
                "DCML.LOGGING"));
    }

    [Fact]
    public void HasCapability_ReturnsFalseForUnknownCapability()
    {
        var info =
            CreateRuntimeInfo();

        Assert.False(
            info.HasCapability(
                "dcml.unknown"));
    }

    [Fact]
    public void Constructor_DeduplicatesCapabilitiesCaseInsensitively()
    {
        var info =
            new DCMLRuntimeInfo(
                "dcml.test.lifecycle",
                "0.0.1",
                "TestHost",
                "1.2.3",
                "Data Center",
                @"C:\Game",
                new[]
                {
                    DCMLRuntimeCapabilities.Logging,
                    "DCML.LOGGING",
                    DCMLRuntimeCapabilities.RuntimeInformation
                });

        Assert.Equal(
            2,
            info.Capabilities.Count);
    }

    [Fact]
    public void Constructor_RejectsEmptyModuleId()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new DCMLRuntimeInfo(
                    " ",
                    "0.0.1",
                    "TestHost",
                    "1.2.3",
                    "Data Center",
                    @"C:\Game",
                    Array.Empty<string>()));
    }

    private static DCMLRuntimeInfo CreateRuntimeInfo()
    {
        return
            new DCMLRuntimeInfo(
                "dcml.test.lifecycle",
                "0.0.1",
                "TestHost",
                "1.2.3",
                "Data Center",
                @"C:\Game",
                new[]
                {
                    DCMLRuntimeCapabilities.Logging,
                    DCMLRuntimeCapabilities.RuntimeInformation
                });
    }
}
