using System;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLGameTypeCatalogTests
{
    [Fact]
    public void Query_UsesSafeDefaults()
    {
        var query =
            new DCMLGameTypeQuery();

        Assert.Equal(
            string.Empty,
            query.FullNameStartsWith);

        Assert.Equal(
            string.Empty,
            query.NameContains);

        Assert.Equal(
            string.Empty,
            query.AssemblyName);

        Assert.Equal(
            DCMLGameTypeQuery.DefaultMaxResults,
            query.MaxResults);
    }

    [Fact]
    public void Query_NormalizesFilters()
    {
        var query =
            new DCMLGameTypeQuery(
                fullNameStartsWith:
                    "  Il2Cpp. ",
                nameContains:
                    "  Server ",
                assemblyName:
                    "  Assembly-CSharp ");

        Assert.Equal(
            "Il2Cpp.",
            query.FullNameStartsWith);

        Assert.Equal(
            "Server",
            query.NameContains);

        Assert.Equal(
            "Assembly-CSharp",
            query.AssemblyName);
    }

    [Fact]
    public void Query_RejectsZeroMaxResults()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new DCMLGameTypeQuery(
                    maxResults:
                        0));
    }

    [Fact]
    public void Query_RejectsTooLargeMaxResults()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new DCMLGameTypeQuery(
                    maxResults:
                        DCMLGameTypeQuery.MaximumMaxResults +
                        1));
    }

    [Fact]
    public void Info_PreservesIdentity()
    {
        var info =
            CreateInfo();

        Assert.Equal(
            "Il2Cpp.Server",
            info.FullName);

        Assert.Equal(
            "Il2Cpp",
            info.NamespaceName);

        Assert.Equal(
            "Server",
            info.Name);

        Assert.Equal(
            "Assembly-CSharp",
            info.AssemblyName);

        Assert.Equal(
            "Il2Cpp.Device",
            info.BaseTypeFullName);
    }

    [Fact]
    public void Info_CopiesSortsAndDeduplicatesInterfaces()
    {
        string[] interfaces =
        {
            "Il2Cpp.Z",
            "Il2Cpp.A",
            "Il2Cpp.Z",
            " "
        };

        var info =
            new DCMLGameTypeInfo(
                "Il2Cpp.Server",
                "Il2Cpp",
                "Server",
                "Assembly-CSharp",
                null,
                true,
                false,
                false,
                false,
                false,
                interfaces);

        Assert.Equal(
            new[]
            {
                "Il2Cpp.A",
                "Il2Cpp.Z"
            },
            info.InterfaceFullNames);

        interfaces[0] =
            "Changed";

        Assert.DoesNotContain(
            "Changed",
            info.InterfaceFullNames);
    }

    [Fact]
    public void Info_ReportsClassKind()
    {
        Assert.Equal(
            "class",
            CreateInfo().Kind);
    }

    [Fact]
    public void Info_ReportsInterfaceKind()
    {
        var info =
            new DCMLGameTypeInfo(
                "Il2Cpp.IModPlugin",
                "Il2Cpp",
                "IModPlugin",
                "Assembly-CSharp",
                null,
                false,
                true,
                false,
                false,
                true,
                null);

        Assert.Equal(
            "interface",
            info.Kind);
    }

    [Fact]
    public void Info_ReportsEnumKind()
    {
        var info =
            new DCMLGameTypeInfo(
                "Il2Cpp.DeviceKind",
                "Il2Cpp",
                "DeviceKind",
                "Assembly-CSharp",
                "System.Enum",
                false,
                false,
                true,
                true,
                false,
                null);

        Assert.Equal(
            "enum",
            info.Kind);
    }

    [Fact]
    public void Info_RejectsMissingFullName()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new DCMLGameTypeInfo(
                    " ",
                    "Il2Cpp",
                    "Server",
                    "Assembly-CSharp",
                    null,
                    true,
                    false,
                    false,
                    false,
                    false,
                    null));
    }

    [Fact]
    public void Capability_HasStableIdentifier()
    {
        Assert.Equal(
            "dcml.game.type-catalog",
            DCMLRuntimeCapabilities.GameTypeCatalog);
    }

    [Fact]
    public void Query_MaximumSupportsWholeRuntimeProbe()
    {
        var query =
            new DCMLGameTypeQuery(
                fullNameStartsWith:
                    "Il2Cpp.",
                maxResults:
                    DCMLGameTypeQuery.MaximumMaxResults);

        Assert.Equal(
            16384,
            query.MaxResults);
    }

    private static DCMLGameTypeInfo CreateInfo()
    {
        return
            new DCMLGameTypeInfo(
                "Il2Cpp.Server",
                "Il2Cpp",
                "Server",
                "Assembly-CSharp",
                "Il2Cpp.Device",
                true,
                false,
                false,
                false,
                false,
                new[]
                {
                    "Il2Cpp.IAddressable"
                });
    }
}
