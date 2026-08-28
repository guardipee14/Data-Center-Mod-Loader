using System;
using System.Collections.Generic;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using DCML.DataCenter;
using DCML.DataCenter.Classification;
using DCML.DataCenter.Models;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLDataCenterRecommendedApiTests
{
    [Fact]
    public void Kinds_ExposeStableSemanticIdentifiers()
    {
        Assert.Equal(
            "unknown",
            DataCenterEntityKinds.Unknown);

        Assert.Equal(
            "user-interface",
            DataCenterEntityKinds.UserInterface);

        Assert.Equal(
            "rack",
            DataCenterEntityKinds.Rack);
    }

    [Fact]
    public void Rule_RejectsMissingMatcher()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new DataCenterEntityRule(
                    "rule",
                    DataCenterEntityKinds.Server));
    }

    [Fact]
    public void Rule_MatchesHierarchyPrefix()
    {
        var rule =
            new DataCenterEntityRule(
                "ui",
                DataCenterEntityKinds.UserInterface,
                hierarchyStartsWith:
                    "Canvas");

        Assert.True(
            rule.IsMatch(
                CreateObject(
                    "Button",
                    "MainMenu",
                    "Canvas - MainMenu/Button")));
    }

    [Fact]
    public void Rule_MatchesComponentPrefix()
    {
        var rule =
            new DataCenterEntityRule(
                "ui",
                DataCenterEntityKinds.UserInterface,
                componentTypePrefix:
                    "UnityEngine.UI.");

        Assert.True(
            rule.IsMatch(
                CreateObject(
                    "Button",
                    "MainMenu",
                    "Root/Button",
                    "UnityEngine.UI.Button")));
    }

    [Fact]
    public void Rule_RequiresAllConfiguredCriteria()
    {
        var rule =
            new DataCenterEntityRule(
                "strict",
                DataCenterEntityKinds.Server,
                nameContains:
                    "Server",
                hierarchyContains:
                    "Rack");

        Assert.False(
            rule.IsMatch(
                CreateObject(
                    "Server",
                    "Gameplay",
                    "World/Floor/Server")));
    }

    [Fact]
    public void EntityInfo_ProjectsImmutableSourceData()
    {
        var source =
            CreateObject(
                "Server01",
                "Gameplay",
                "World/Rack/Server01",
                "Il2Cpp.Server");

        var entity =
            new DataCenterEntityInfo(
                source,
                DataCenterEntityKinds.Server,
                "server-rule");

        Assert.Equal(
            source.InstanceId,
            entity.InstanceId);

        Assert.Equal(
            source.HierarchyPath,
            entity.HierarchyPath);

        Assert.Equal(
            DataCenterEntityKinds.Server,
            entity.Kind);

        Assert.Equal(
            "server-rule",
            entity.ClassificationRuleId);
    }

    [Fact]
    public void Query_UsesSafeDefaults()
    {
        var query =
            new DataCenterEntityQuery();

        Assert.Equal(
            string.Empty,
            query.Kind);

        Assert.True(
            query.IncludeInactive);

        Assert.True(
            query.IncludeUnknown);

        Assert.Equal(
            DCMLGameObjectQuery.DefaultMaxResults,
            query.MaxResults);
    }

    [Fact]
    public void Query_RejectsInvalidMaxResults()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new DataCenterEntityQuery(
                    maxResults: 0));
    }

    [Fact]
    public void Discovery_ClassifiesCanvasAsUserInterface()
    {
        var discovery =
            new DataCenterEntityDiscovery(
                new FakeDiscovery(
                    CreateObject(
                        "MainMenu",
                        "MainMenu",
                        "Canvas - MainMenu/MainMenu")));

        var result =
            Assert.Single(
                discovery.Find(
                    new DataCenterEntityQuery()));

        Assert.Equal(
            DataCenterEntityKinds.UserInterface,
            result.Kind);

        Assert.Equal(
            "dcml.datacenter.ui.canvas",
            result.ClassificationRuleId);
    }

    [Fact]
    public void Discovery_FiltersByKind()
    {
        var discovery =
            new DataCenterEntityDiscovery(
                new FakeDiscovery(
                    CreateObject(
                        "Button",
                        "MainMenu",
                        "Canvas/Button"),
                    CreateObject(
                        "World",
                        "MainMenu",
                        "World")));

        IReadOnlyList<DataCenterEntityInfo> results =
            discovery.Find(
                new DataCenterEntityQuery(
                    kind:
                        DataCenterEntityKinds.UserInterface));

        Assert.Single(
            results);

        Assert.Equal(
            "Button",
            results[0].Name);
    }

    [Fact]
    public void Discovery_CanExcludeUnknownEntities()
    {
        var discovery =
            new DataCenterEntityDiscovery(
                new FakeDiscovery(
                    CreateObject(
                        "World",
                        "Gameplay",
                        "World")));

        IReadOnlyList<DataCenterEntityInfo> results =
            discovery.Find(
                new DataCenterEntityQuery(
                    includeUnknown:
                        false));

        Assert.Empty(
            results);
    }

    [Fact]
    public void Api_Create_ResolvesLowLevelDiscovery()
    {
        var lowLevel =
            new FakeDiscovery(
                CreateObject(
                    "Button",
                    "MainMenu",
                    "Canvas/Button"));

        var context =
            new FakeContext(
                lowLevel);

        DataCenterApi api =
            DataCenterApi.Create(
                context);

        Assert.Single(
            api.Entities.Find(
                new DataCenterEntityQuery(
                    kind:
                        DataCenterEntityKinds.UserInterface)));
    }

    [Fact]
    public void Api_Create_RejectsMissingLowLevelDiscovery()
    {
        var context =
            new FakeContext(
                null);

        Assert.Throws<InvalidOperationException>(
            () =>
                DataCenterApi.Create(
                    context));
    }

    private static DCMLGameObjectInfo CreateObject(
        string name,
        string scene,
        string hierarchy,
        params string[] components)
    {
        return
            new DCMLGameObjectInfo(
                name.GetHashCode(),
                name,
                scene,
                hierarchy,
                true,
                components);
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

        public IReadOnlyList<DCMLGameObjectInfo> Find(
            DCMLGameObjectQuery query)
        {
            return
                _objects;
        }
    }

    private sealed class FakeContext :
        IDCMLModuleContext
    {
        public FakeContext(
            IDCMLGameObjectDiscovery? discovery)
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
        private readonly IDCMLGameObjectDiscovery?
            _discovery;

        public FakeServices(
            IDCMLGameObjectDiscovery? discovery)
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
