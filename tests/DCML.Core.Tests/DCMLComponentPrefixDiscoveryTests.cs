using System;
using System.Collections.Generic;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using DCML.DataCenter;
using DCML.DataCenter.Models;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLComponentPrefixDiscoveryTests
{
    [Fact]
    public void GameObjectQuery_DefaultPrefixIsEmpty()
    {
        var query =
            new DCMLGameObjectQuery();

        Assert.Equal(
            string.Empty,
            query.ComponentTypeNamePrefix);
    }

    [Fact]
    public void GameObjectQuery_PreservesComponentTypePrefix()
    {
        var query =
            new DCMLGameObjectQuery(
                componentTypeNamePrefix:
                    "Il2Cpp.");

        Assert.Equal(
            "Il2Cpp.",
            query.ComponentTypeNamePrefix);
    }

    [Fact]
    public void GameObjectQuery_NormalizesComponentTypePrefix()
    {
        var query =
            new DCMLGameObjectQuery(
                componentTypeNamePrefix:
                    "  Il2Cpp.  ");

        Assert.Equal(
            "Il2Cpp.",
            query.ComponentTypeNamePrefix);
    }

    [Fact]
    public void GameObjectQuery_CanCombineExactAndPrefixFilters()
    {
        var query =
            new DCMLGameObjectQuery(
                componentTypeName:
                    "CableLink",
                componentTypeNamePrefix:
                    "Il2Cpp.");

        Assert.Equal(
            "CableLink",
            query.ComponentTypeName);

        Assert.Equal(
            "Il2Cpp.",
            query.ComponentTypeNamePrefix);
    }

    [Fact]
    public void ComponentCatalog_ForwardsPrefixToLowLevelDiscovery()
    {
        var discovery =
            new CapturingDiscovery();

        var catalog =
            new DataCenterComponentCatalog(
                discovery);

        catalog.Scan(
            new DataCenterComponentCatalogQuery(
                sceneName:
                    "BaseScene",
                typeNamePrefix:
                    "Il2Cpp.",
                maxObjects:
                    512));

        Assert.NotNull(
            discovery.LastQuery);

        Assert.Equal(
            "BaseScene",
            discovery.LastQuery!.SceneName);

        Assert.Equal(
            "Il2Cpp.",
            discovery.LastQuery.ComponentTypeNamePrefix);

        Assert.Equal(
            512,
            discovery.LastQuery.MaxResults);
    }

    private sealed class CapturingDiscovery :
        IDCMLGameObjectDiscovery
    {
        public DCMLGameObjectQuery? LastQuery { get; private set; }

        public IReadOnlyList<DCMLGameObjectInfo> Find(
            DCMLGameObjectQuery query)
        {
            LastQuery =
                query;

            return
                Array.Empty<DCMLGameObjectInfo>();
        }
    }
}
