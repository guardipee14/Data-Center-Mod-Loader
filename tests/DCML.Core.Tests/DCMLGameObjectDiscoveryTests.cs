using System;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLGameObjectDiscoveryTests
{
    [Fact]
    public void Query_UsesSafeDefaults()
    {
        var query =
            new DCMLGameObjectQuery();

        Assert.Equal(
            string.Empty,
            query.NameContains);

        Assert.Equal(
            string.Empty,
            query.SceneName);

        Assert.Equal(
            string.Empty,
            query.ComponentTypeName);

        Assert.True(
            query.IncludeInactive);

        Assert.Equal(
            DCMLGameObjectQuery.DefaultMaxResults,
            query.MaxResults);
    }

    [Fact]
    public void Query_PreservesFilters()
    {
        var query =
            new DCMLGameObjectQuery(
                "rack",
                "MainMenu",
                "Transform",
                false,
                32);

        Assert.Equal(
            "rack",
            query.NameContains);

        Assert.Equal(
            "MainMenu",
            query.SceneName);

        Assert.Equal(
            "Transform",
            query.ComponentTypeName);

        Assert.False(
            query.IncludeInactive);

        Assert.Equal(
            32,
            query.MaxResults);
    }

    [Fact]
    public void Query_NormalizesNullAndWhitespaceFilters()
    {
        var query =
            new DCMLGameObjectQuery(
                null,
                "  MainMenu  ",
                "  ",
                true,
                5);

        Assert.Equal(
            string.Empty,
            query.NameContains);

        Assert.Equal(
            "MainMenu",
            query.SceneName);

        Assert.Equal(
            string.Empty,
            query.ComponentTypeName);
    }

    [Fact]
    public void Query_RejectsZeroMaxResults()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new DCMLGameObjectQuery(
                    maxResults: 0));
    }

    [Fact]
    public void Query_RejectsTooLargeMaxResults()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new DCMLGameObjectQuery(
                    maxResults:
                        DCMLGameObjectQuery.MaximumMaxResults +
                        1));
    }

    [Fact]
    public void Info_PreservesScalarData()
    {
        var info =
            new DCMLGameObjectInfo(
                42,
                "Rack",
                "Gameplay",
                "World/Room/Rack",
                true,
                new[]
                {
                    "UnityEngine.Transform"
                });

        Assert.Equal(
            42,
            info.InstanceId);

        Assert.Equal(
            "Rack",
            info.Name);

        Assert.Equal(
            "Gameplay",
            info.SceneName);

        Assert.Equal(
            "World/Room/Rack",
            info.HierarchyPath);

        Assert.True(
            info.ActiveInHierarchy);
    }

    [Fact]
    public void Info_NormalizesNullStringsAndComponents()
    {
        var info =
            new DCMLGameObjectInfo(
                -1,
                null,
                null,
                null,
                false,
                null);

        Assert.Equal(
            string.Empty,
            info.Name);

        Assert.Equal(
            string.Empty,
            info.SceneName);

        Assert.Equal(
            string.Empty,
            info.HierarchyPath);

        Assert.Empty(
            info.ComponentTypeNames);
    }

    [Fact]
    public void Info_CopiesSortsAndDeduplicatesComponentNames()
    {
        var source =
            new[]
            {
                "Z.Component",
                "A.Component",
                "Z.Component",
                " ",
                null!
            };

        var info =
            new DCMLGameObjectInfo(
                1,
                "Object",
                "Scene",
                "Object",
                true,
                source);

        Assert.Equal(
            new[]
            {
                "A.Component",
                "Z.Component"
            },
            info.ComponentTypeNames);

        source[0] =
            "Changed.Component";

        Assert.DoesNotContain(
            "Changed.Component",
            info.ComponentTypeNames);

        Assert.Equal(
            "dcml.game.object-discovery",
            DCMLRuntimeCapabilities.GameObjectDiscovery);
    }
}
