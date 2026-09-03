using System;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLGameResourceDiscoveryTests
{
    [Fact]
    public void Query_UsesSafeDefaults()
    {
        var query =
            new DCMLGameResourceQuery();

        Assert.Equal(
            string.Empty,
            query.NameContains);

        Assert.Equal(
            string.Empty,
            query.ComponentTypeName);

        Assert.Equal(
            string.Empty,
            query.ComponentTypeNamePrefix);

        Assert.Equal(
            DCMLGameResourceQuery.DefaultMaxResults,
            query.MaxResults);

        Assert.Equal(
            0,
            query.SkipResults);
    }

    [Fact]
    public void Query_NormalizesFilters()
    {
        var query =
            new DCMLGameResourceQuery(
                nameContains:
                    "  Server ",
                componentTypeName:
                    "  Il2Cpp.Server ",
                componentTypeNamePrefix:
                    "  Il2Cpp. ");

        Assert.Equal(
            "Server",
            query.NameContains);

        Assert.Equal(
            "Il2Cpp.Server",
            query.ComponentTypeName);

        Assert.Equal(
            "Il2Cpp.",
            query.ComponentTypeNamePrefix);
    }

    [Fact]
    public void Query_RejectsZeroMaxResults()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new DCMLGameResourceQuery(
                    maxResults:
                        0));
    }

    [Fact]
    public void Query_RejectsTooLargeMaxResults()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new DCMLGameResourceQuery(
                    maxResults:
                        DCMLGameResourceQuery.MaximumMaxResults +
                        1));
    }

    [Fact]
    public void Query_RejectsNegativeSkip()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new DCMLGameResourceQuery(
                    skipResults:
                        -1));
    }

    [Fact]
    public void Info_PreservesIdentityAndNormalizesComponents()
    {
        string[] components =
        {
            " Il2Cpp.Server ",
            "UnityEngine.Transform",
            "Il2Cpp.Server",
            " "
        };

        var info =
            new DCMLGameResourceInfo(
                42,
                "ServerPrefab",
                components);

        Assert.Equal(
            42,
            info.InstanceId);

        Assert.Equal(
            "ServerPrefab",
            info.Name);

        Assert.Equal(
            new[]
            {
                "Il2Cpp.Server",
                "UnityEngine.Transform"
            },
            info.ComponentTypeNames);

        components[0] =
            "Changed";

        Assert.DoesNotContain(
            "Changed",
            info.ComponentTypeNames);
    }

    [Fact]
    public void Info_AllowsMissingOptionalData()
    {
        var info =
            new DCMLGameResourceInfo(
                7,
                null,
                null);

        Assert.Equal(
            string.Empty,
            info.Name);

        Assert.Empty(
            info.ComponentTypeNames);
    }

    [Fact]
    public void Capability_HasStableIdentifier()
    {
        Assert.Equal(
            "dcml.game.resource-discovery",
            DCMLRuntimeCapabilities.GameResourceDiscovery);
    }
}
