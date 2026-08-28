using System;
using System.Collections.Generic;
using System.Linq;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using DCML.DataCenter;
using DCML.DataCenter.Models;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLDataCenterComponentCatalogTests
{
    [Fact]
    public void Query_UsesSafeDefaults()
    {
        var query =
            new DataCenterComponentCatalogQuery();

        Assert.Equal(
            string.Empty,
            query.SceneName);

        Assert.Equal(
            string.Empty,
            query.TypeNamePrefix);

        Assert.True(
            query.IncludeInactive);

        Assert.Equal(
            DCMLGameObjectQuery.MaximumMaxResults,
            query.MaxObjects);

        Assert.Equal(
            8,
            query.MaxExamplesPerType);
    }

    [Fact]
    public void Query_NormalizesFilters()
    {
        var query =
            new DataCenterComponentCatalogQuery(
                sceneName:
                    " MainMenu ",
                typeNamePrefix:
                    " Il2Cpp. ");

        Assert.Equal(
            "MainMenu",
            query.SceneName);

        Assert.Equal(
            "Il2Cpp.",
            query.TypeNamePrefix);
    }

    [Fact]
    public void Query_RejectsInvalidMaxObjects()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new DataCenterComponentCatalogQuery(
                    maxObjects: 0));
    }

    [Fact]
    public void Query_RejectsInvalidMaxExamples()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new DataCenterComponentCatalogQuery(
                    maxExamplesPerType: 0));
    }

    [Fact]
    public void TypeInfo_ExposesRuntimeFamilies()
    {
        var il2Cpp =
            new DataCenterComponentTypeInfo(
                "Il2Cpp.Server",
                1,
                1,
                0,
                new[] { "World/Server" });

        var unity =
            new DataCenterComponentTypeInfo(
                "UnityEngine.Transform",
                1,
                1,
                0,
                new[] { "World" });

        Assert.True(
            il2Cpp.IsIl2Cpp);

        Assert.False(
            il2Cpp.IsUnityEngine);

        Assert.True(
            unity.IsUnityEngine);
    }

    [Fact]
    public void TypeInfo_RejectsMismatchedCounts()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new DataCenterComponentTypeInfo(
                    "Il2Cpp.Server",
                    2,
                    1,
                    0,
                    Array.Empty<string>()));
    }

    [Fact]
    public void Snapshot_CountsRuntimeFamilies()
    {
        var snapshot =
            new DataCenterComponentCatalogSnapshot(
                "Gameplay",
                3,
                new[]
                {
                    TypeInfo(
                        "Il2Cpp.Server",
                        1),
                    TypeInfo(
                        "UnityEngine.Transform",
                        3),
                    TypeInfo(
                        "Custom.Component",
                        1)
                });

        Assert.Equal(
            3,
            snapshot.UniqueComponentTypeCount);

        Assert.Equal(
            1,
            snapshot.Il2CppTypeCount);

        Assert.Equal(
            1,
            snapshot.UnityEngineTypeCount);
    }

    [Fact]
    public void Catalog_GroupsComponentTypesAcrossObjects()
    {
        var source =
            new FakeDiscovery(
                GameObject(
                    1,
                    "ServerA",
                    true,
                    "World/Rack/ServerA",
                    "Il2Cpp.Server",
                    "UnityEngine.Transform"),
                GameObject(
                    2,
                    "ServerB",
                    true,
                    "World/Rack/ServerB",
                    "Il2Cpp.Server",
                    "UnityEngine.Transform"));

        var catalog =
            new DataCenterComponentCatalog(
                source);

        DataCenterComponentCatalogSnapshot snapshot =
            catalog.Scan(
                new DataCenterComponentCatalogQuery(
                    sceneName:
                        "Gameplay"));

        DataCenterComponentTypeInfo server =
            Assert.Single(
                snapshot.ComponentTypes.Where(
                    value =>
                        value.TypeName ==
                        "Il2Cpp.Server"));

        Assert.Equal(
            2,
            server.ObjectCount);
    }

    [Fact]
    public void Catalog_TracksActiveAndInactiveObjects()
    {
        var source =
            new FakeDiscovery(
                GameObject(
                    1,
                    "A",
                    true,
                    "World/A",
                    "Il2Cpp.Device"),
                GameObject(
                    2,
                    "B",
                    false,
                    "World/B",
                    "Il2Cpp.Device"));

        var catalog =
            new DataCenterComponentCatalog(
                source);

        DataCenterComponentTypeInfo info =
            Assert.Single(
                catalog
                    .Scan(
                        new DataCenterComponentCatalogQuery())
                    .ComponentTypes);

        Assert.Equal(
            1,
            info.ActiveObjectCount);

        Assert.Equal(
            1,
            info.InactiveObjectCount);
    }

    [Fact]
    public void Catalog_BoundsExamplesPerType()
    {
        var source =
            new FakeDiscovery(
                GameObject(
                    1,
                    "A",
                    true,
                    "World/A",
                    "Il2Cpp.Device"),
                GameObject(
                    2,
                    "B",
                    true,
                    "World/B",
                    "Il2Cpp.Device"),
                GameObject(
                    3,
                    "C",
                    true,
                    "World/C",
                    "Il2Cpp.Device"));

        var catalog =
            new DataCenterComponentCatalog(
                source);

        DataCenterComponentTypeInfo info =
            Assert.Single(
                catalog
                    .Scan(
                        new DataCenterComponentCatalogQuery(
                            maxExamplesPerType: 2))
                    .ComponentTypes);

        Assert.Equal(
            2,
            info.ExampleHierarchyPaths.Count);
    }

    [Fact]
    public void Catalog_FiltersByTypePrefix()
    {
        var source =
            new FakeDiscovery(
                GameObject(
                    1,
                    "A",
                    true,
                    "World/A",
                    "Il2Cpp.Device",
                    "UnityEngine.Transform"));

        var catalog =
            new DataCenterComponentCatalog(
                source);

        DataCenterComponentCatalogSnapshot snapshot =
            catalog.Scan(
                new DataCenterComponentCatalogQuery(
                    typeNamePrefix:
                        "Il2Cpp."));

        DataCenterComponentTypeInfo info =
            Assert.Single(
                snapshot.ComponentTypes);

        Assert.Equal(
            "Il2Cpp.Device",
            info.TypeName);
    }

    [Fact]
    public void Catalog_ForwardsSceneAndObjectLimit()
    {
        var source =
            new FakeDiscovery();

        var catalog =
            new DataCenterComponentCatalog(
                source);

        catalog.Scan(
            new DataCenterComponentCatalogQuery(
                sceneName:
                    "Gameplay",
                includeInactive:
                    false,
                maxObjects:
                    123));

        Assert.NotNull(
            source.LastQuery);

        Assert.Equal(
            "Gameplay",
            source.LastQuery!.SceneName);

        Assert.False(
            source.LastQuery.IncludeInactive);

        Assert.Equal(
            123,
            source.LastQuery.MaxResults);
    }

    [Fact]
    public void Api_ExposesComponentCatalog()
    {
        var context =
            new FakeContext(
                new FakeDiscovery(
                    GameObject(
                        1,
                        "A",
                        true,
                        "World/A",
                        "Il2Cpp.Device")));

        DataCenterApi api =
            DataCenterApi.Create(
                context);

        DataCenterComponentCatalogSnapshot snapshot =
            api.Components.Scan(
                new DataCenterComponentCatalogQuery());

        Assert.Single(
            snapshot.ComponentTypes);
    }

    private static DataCenterComponentTypeInfo TypeInfo(
        string typeName,
        int count)
    {
        return
            new DataCenterComponentTypeInfo(
                typeName,
                count,
                count,
                0,
                Array.Empty<string>());
    }

    private static DCMLGameObjectInfo GameObject(
        int id,
        string name,
        bool active,
        string hierarchy,
        params string[] componentTypes)
    {
        return
            new DCMLGameObjectInfo(
                id,
                name,
                "Gameplay",
                hierarchy,
                active,
                componentTypes);
    }

    private sealed class FakeDiscovery :
        IDCMLGameObjectDiscovery
    {
        private readonly IReadOnlyList<DCMLGameObjectInfo>
            _objects;

        public FakeDiscovery(
            params DCMLGameObjectInfo[] objects)
        {
            _objects =
                objects;
        }

        public DCMLGameObjectQuery? LastQuery { get; private set; }

        public IReadOnlyList<DCMLGameObjectInfo> Find(
            DCMLGameObjectQuery query)
        {
            LastQuery =
                query;

            return
                _objects;
        }
    }

    private sealed class FakeContext :
        IDCMLModuleContext
    {
        public FakeContext(
            IDCMLGameObjectDiscovery discovery)
        {
            Services =
                new FakeServices(
                    discovery);
        }

        public string ModuleDirectory =>
            "module";

        public string DataDirectory =>
            "data";

        public IServiceProvider Services { get; }
    }

    private sealed class FakeServices :
        IServiceProvider
    {
        private readonly IDCMLGameObjectDiscovery
            _discovery;

        public FakeServices(
            IDCMLGameObjectDiscovery discovery)
        {
            _discovery =
                discovery;
        }

        public object? GetService(
            Type serviceType)
        {
            if (
                serviceType ==
                typeof(IDCMLGameObjectDiscovery)
            )
            {
                return
                    _discovery;
            }

            return null;
        }
    }
}
