using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using DCML.DataCenter;
using DCML.DataCenter.Classification;
using DCML.DataCenter.Models;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLInheritanceAwareSemanticDiscoveryTests
{
    [Fact]
    public void Rule_PreservesAssignableToMatcher()
    {
        var rule =
            new DataCenterEntityRule(
                "network",
                DataCenterEntityKinds.NetworkDevice,
                componentTypeAssignableTo:
                    "Il2Cpp.NetworkSwitch");

        Assert.Equal(
            "Il2Cpp.NetworkSwitch",
            rule.ComponentTypeAssignableTo);
    }

    [Fact]
    public void Rule_AssignableMatcherMatchesExactTypeWithoutResolver()
    {
        var rule =
            new DataCenterEntityRule(
                "network",
                DataCenterEntityKinds.NetworkDevice,
                componentTypeAssignableTo:
                    "Il2Cpp.NetworkSwitch");

        Assert.True(
            rule.IsMatch(
                CreateObject(
                    "Il2Cpp.NetworkSwitch")));
    }

    [Fact]
    public void Rule_AssignableMatcherUsesResolverForDerivedType()
    {
        var rule =
            new DataCenterEntityRule(
                "network",
                DataCenterEntityKinds.NetworkDevice,
                componentTypeAssignableTo:
                    "Il2Cpp.NetworkSwitch");

        Assert.True(
            rule.IsMatch(
                CreateObject(
                    "Il2Cpp.Router"),
                (candidate, target) =>
                    candidate == "Il2Cpp.Router" &&
                    target == "Il2Cpp.NetworkSwitch"));
    }

    [Fact]
    public void Rule_AssignableMatcherDoesNotGuessWithoutResolver()
    {
        var rule =
            new DataCenterEntityRule(
                "network",
                DataCenterEntityKinds.NetworkDevice,
                componentTypeAssignableTo:
                    "Il2Cpp.NetworkSwitch");

        Assert.False(
            rule.IsMatch(
                CreateObject(
                    "Il2Cpp.CustomRouter")));
    }

    [Fact]
    public void Discovery_ClassifiesCustomNetworkSwitchSubclass()
    {
        var discovery =
            new DataCenterEntityDiscovery(
                new SinglePageDiscovery(
                    CreateObject(
                        "Il2Cpp.CustomRouter")),
                new FakeTypeCatalog(
                    TypeInfo(
                        "Il2Cpp.NetworkSwitch",
                        "Il2Cpp.UsableObject"),
                    TypeInfo(
                        "Il2Cpp.Router",
                        "Il2Cpp.NetworkSwitch"),
                    TypeInfo(
                        "Il2Cpp.CustomRouter",
                        "Il2Cpp.Router")));

        DataCenterEntityInfo result =
            Assert.Single(
                discovery.Find(
                    new DataCenterEntityQuery(
                        kind:
                            DataCenterEntityKinds.NetworkDevice,
                        includeUnknown:
                            false)));

        Assert.Equal(
            DataCenterEntityKinds.NetworkDevice,
            result.Kind);

        Assert.Equal(
            "dcml.datacenter.network-device.inherited",
            result.ClassificationRuleId);
    }

    [Fact]
    public void Discovery_TraversesMultiLevelInheritance()
    {
        var customRule =
            new DataCenterEntityRule(
                "usable",
                DataCenterEntityKinds.NetworkDevice,
                componentTypeAssignableTo:
                    "Il2Cpp.UsableObject");

        var discovery =
            new DataCenterEntityDiscovery(
                new SinglePageDiscovery(
                    CreateObject(
                        "Il2Cpp.Firewall")),
                new FakeTypeCatalog(
                    TypeInfo(
                        "Il2Cpp.UsableObject",
                        "Il2Cpp.Interact"),
                    TypeInfo(
                        "Il2Cpp.NetworkSwitch",
                        "Il2Cpp.UsableObject"),
                    TypeInfo(
                        "Il2Cpp.Firewall",
                        "Il2Cpp.NetworkSwitch")),
                new[]
                {
                    customRule
                });

        DataCenterEntityInfo result =
            Assert.Single(
                discovery.Find(
                    new DataCenterEntityQuery(
                        includeUnknown:
                            false)));

        Assert.Equal(
            "usable",
            result.ClassificationRuleId);
    }

    [Fact]
    public void Discovery_ExactDefaultsStillWorkWithoutTypeCatalog()
    {
        var discovery =
            new DataCenterEntityDiscovery(
                new SinglePageDiscovery(
                    CreateObject(
                        "Il2Cpp.Router")));

        DataCenterEntityInfo result =
            Assert.Single(
                discovery.Find(
                    new DataCenterEntityQuery(
                        kind:
                            DataCenterEntityKinds.NetworkDevice,
                        includeUnknown:
                            false)));

        Assert.Equal(
            "dcml.datacenter.router.component",
            result.ClassificationRuleId);
    }

    [Fact]
    public void Discovery_PagesUntilFilteredServerIsFound()
    {
        var lowLevel =
            new TwoPageDiscovery(
                secondPageObject:
                    CreateObject(
                        "Il2Cpp.Server"));

        var discovery =
            new DataCenterEntityDiscovery(
                lowLevel);

        DataCenterEntityInfo result =
            Assert.Single(
                discovery.Find(
                    new DataCenterEntityQuery(
                        kind:
                            DataCenterEntityKinds.Server,
                        includeUnknown:
                            false,
                        maxResults:
                            1)));

        Assert.Equal(
            DataCenterEntityKinds.Server,
            result.Kind);

        Assert.Equal(
            new[]
            {
                0,
                DCMLGameObjectQuery.MaximumMaxResults
            },
            lowLevel.SkipValues);
    }

    [Fact]
    public void Discovery_StopsAfterPartialPage()
    {
        var lowLevel =
            new SinglePageDiscovery(
                CreateObject(
                    "Il2Cpp.CableLink"));

        var discovery =
            new DataCenterEntityDiscovery(
                lowLevel);

        Assert.Empty(
            discovery.Find(
                new DataCenterEntityQuery(
                    kind:
                        DataCenterEntityKinds.Server,
                    includeUnknown:
                        false)));

        Assert.Equal(
            1,
            lowLevel.CallCount);
    }

    [Fact]
    public void Defaults_KeepRackMountUnknownWithHierarchyAvailable()
    {
        var discovery =
            new DataCenterEntityDiscovery(
                new SinglePageDiscovery(
                    CreateObject(
                        "Il2Cpp.RackMount")),
                new FakeTypeCatalog(
                    TypeInfo(
                        "Il2Cpp.Rack",
                        "UnityEngine.MonoBehaviour"),
                    TypeInfo(
                        "Il2Cpp.RackMount",
                        "Il2Cpp.Interact")));

        DataCenterEntityInfo result =
            Assert.Single(
                discovery.Find(
                    new DataCenterEntityQuery()));

        Assert.Equal(
            DataCenterEntityKinds.Unknown,
            result.Kind);
    }

    [Fact]
    public void Defaults_KeepSfpModuleUnknownWithHierarchyAvailable()
    {
        var discovery =
            new DataCenterEntityDiscovery(
                new SinglePageDiscovery(
                    CreateObject(
                        "Il2Cpp.SFPModule")),
                new FakeTypeCatalog(
                    TypeInfo(
                        "Il2Cpp.SFPModule",
                        "Il2Cpp.UsableObject"),
                    TypeInfo(
                        "Il2Cpp.NetworkSwitch",
                        "Il2Cpp.UsableObject")));

        DataCenterEntityInfo result =
            Assert.Single(
                discovery.Find(
                    new DataCenterEntityQuery()));

        Assert.Equal(
            DataCenterEntityKinds.Unknown,
            result.Kind);
    }

    [Fact]
    public void Api_CanUseOptionalTypeCatalogWithoutRequiringIt()
    {
        var gameObjects =
            new SinglePageDiscovery(
                CreateObject(
                    "Il2Cpp.CustomRouter"));

        var context =
            new FakeContext(
                gameObjects,
                new FakeTypeCatalog(
                    TypeInfo(
                        "Il2Cpp.NetworkSwitch",
                        "Il2Cpp.UsableObject"),
                    TypeInfo(
                        "Il2Cpp.CustomRouter",
                        "Il2Cpp.NetworkSwitch")));

        DataCenterApi api =
            DataCenterApi.Create(
                context);

        DataCenterEntityInfo result =
            Assert.Single(
                api.Entities.Find(
                    new DataCenterEntityQuery(
                        kind:
                            DataCenterEntityKinds.NetworkDevice,
                        includeUnknown:
                            false)));

        Assert.Equal(
            "dcml.datacenter.network-device.inherited",
            result.ClassificationRuleId);
    }

    private static DCMLGameObjectInfo CreateObject(
        string componentType)
    {
        return
            new DCMLGameObjectInfo(
                componentType.GetHashCode(),
                componentType.Split('.').Last(),
                "BaseScene",
                "Objects/Test/" +
                componentType.Split('.').Last(),
                true,
                new[]
                {
                    componentType
                });
    }

    private static DCMLGameTypeInfo TypeInfo(
        string fullName,
        string baseType)
    {
        string name =
            fullName.Split('.').Last();

        string namespaceName =
            fullName.Contains('.')
                ? fullName.Substring(
                    0,
                    fullName.LastIndexOf('.'))
                : string.Empty;

        return
            new DCMLGameTypeInfo(
                fullName,
                namespaceName,
                name,
                "Assembly-CSharp",
                baseType,
                true,
                false,
                false,
                false,
                false,
                null);
    }

    private sealed class SinglePageDiscovery :
        IDCMLGameObjectDiscovery
    {
        private readonly IReadOnlyList<DCMLGameObjectInfo>
            _objects;

        public SinglePageDiscovery(
            params DCMLGameObjectInfo[] objects)
        {
            _objects =
                objects;
        }

        public int CallCount { get; private set; }

        public IReadOnlyList<DCMLGameObjectInfo> Find(
            DCMLGameObjectQuery query)
        {
            CallCount++;

            return
                query.SkipResults == 0
                    ? _objects
                    : Array.Empty<DCMLGameObjectInfo>();
        }
    }

    private sealed class TwoPageDiscovery :
        IDCMLGameObjectDiscovery
    {
        private readonly DCMLGameObjectInfo
            _secondPageObject;

        public TwoPageDiscovery(
            DCMLGameObjectInfo secondPageObject)
        {
            _secondPageObject =
                secondPageObject;
        }

        public List<int> SkipValues { get; } =
            new List<int>();

        public IReadOnlyList<DCMLGameObjectInfo> Find(
            DCMLGameObjectQuery query)
        {
            SkipValues.Add(
                query.SkipResults);

            if (query.SkipResults == 0)
            {
                return
                    new FullUnknownPage(
                        query.MaxResults);
            }

            return
                new[]
                {
                    _secondPageObject
                };
        }
    }

    private sealed class FullUnknownPage :
        IReadOnlyList<DCMLGameObjectInfo>
    {
        private readonly int _count;

        public FullUnknownPage(
            int count)
        {
            _count =
                count;
        }

        public int Count =>
            _count;

        public DCMLGameObjectInfo this[int index] =>
            CreateUnknown(
                index);

        public IEnumerator<DCMLGameObjectInfo> GetEnumerator()
        {
            for (
                int index = 0;
                index < _count;
                index++
            )
            {
                yield return
                    CreateUnknown(
                        index);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return
                GetEnumerator();
        }

        private static DCMLGameObjectInfo CreateUnknown(
            int index)
        {
            return
                new DCMLGameObjectInfo(
                    index,
                    "Unknown" + index,
                    "BaseScene",
                    "Objects/Unknown/" + index,
                    true,
                    new[]
                    {
                        "UnityEngine.Transform"
                    });
        }
    }

    private sealed class FakeTypeCatalog :
        IDCMLGameTypeCatalog
    {
        private readonly IReadOnlyList<DCMLGameTypeInfo>
            _types;

        public FakeTypeCatalog(
            params DCMLGameTypeInfo[] types)
        {
            _types =
                types;
        }

        public IReadOnlyList<DCMLGameTypeInfo> Find(
            DCMLGameTypeQuery query)
        {
            return
                _types
                    .Where(
                        value =>
                            query.FullNameStartsWith.Length == 0 ||
                            value.FullName.StartsWith(
                                query.FullNameStartsWith,
                                StringComparison.OrdinalIgnoreCase))
                    .Take(
                        query.MaxResults)
                    .ToArray();
        }
    }

    private sealed class FakeContext :
        IDCMLModuleContext
    {
        public FakeContext(
            IDCMLGameObjectDiscovery gameObjectDiscovery,
            IDCMLGameTypeCatalog? gameTypeCatalog)
        {
            Services =
                new FakeServices(
                    gameObjectDiscovery,
                    gameTypeCatalog);
        }

        public string ModuleDirectory =>
            "C:\\DCML\\Module";

        public string DataDirectory =>
            "C:\\DCML\\Data";

        public IServiceProvider Services { get; }

        private sealed class FakeServices :
            IServiceProvider
        {
            private readonly IDCMLGameObjectDiscovery
                _gameObjectDiscovery;

            private readonly IDCMLGameTypeCatalog?
                _gameTypeCatalog;

            public FakeServices(
                IDCMLGameObjectDiscovery gameObjectDiscovery,
                IDCMLGameTypeCatalog? gameTypeCatalog)
            {
                _gameObjectDiscovery =
                    gameObjectDiscovery;

                _gameTypeCatalog =
                    gameTypeCatalog;
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
                        _gameObjectDiscovery;
                }

                if (
                    serviceType ==
                    typeof(IDCMLGameTypeCatalog)
                )
                {
                    return
                        _gameTypeCatalog;
                }

                return
                    null;
            }
        }
    }
}
