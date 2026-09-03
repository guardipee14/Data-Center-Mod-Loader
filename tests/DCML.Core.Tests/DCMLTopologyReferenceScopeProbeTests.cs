using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using DCML.DataCenter;
using DCML.DataCenter.Abstractions;
using DCML.DataCenter.Models;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLTopologyReferenceScopeProbeTests
{
    [Fact]
    public void Edge_UnknownTargetIsNotObserved()
    {
        var edge =
            new DataCenterHardwareTopologyEdge(
                "sfp-link",
                Ref(1, "SFP", "Il2Cpp.SFPModule"),
                Ref(2, "Target", "Il2Cpp.CableLink"),
                false);

        Assert.False(edge.TargetObserved);
        Assert.Equal(
            DataCenterHardwareTopologyTargetLocation.Unknown,
            edge.TargetLocation);
    }

    [Fact]
    public void Edge_NonSceneTargetIsObservedButNotResolved()
    {
        var edge =
            new DataCenterHardwareTopologyEdge(
                "sfp-link",
                Ref(1, "SFP", "Il2Cpp.SFPModule"),
                Ref(2, "Target", "Il2Cpp.CableLink"),
                false,
                "Target",
                DataCenterHardwareTopologyTargetLocation.NonSceneObject);

        Assert.True(edge.TargetObserved);
        Assert.False(edge.TargetResolved);
    }

    [Fact]
    public async Task CaptureAsync_ProbesNonSceneAfterSceneMiss()
    {
        var reader =
            new ScopeReader(
                scenePages:
                    new[]
                    {
                        Array.Empty<DCMLGameComponentState>()
                    },
                resourcePages:
                    new[]
                    {
                        Page(
                            State(
                                500,
                                "SFP_Slot2.003",
                                isResource:
                                    true))
                    });

        DataCenterHardwareTopology graphBuilder =
            CreateTopology(
                reader,
                500);

        await graphBuilder.CaptureAsync(
            Query());

        Assert.Equal(
            2,
            reader.Queries.Count);

        Assert.Equal(
            DCMLGameComponentScope.Scene,
            reader.Queries[0].Scope);

        Assert.Equal(
            DCMLGameComponentScope.Resource,
            reader.Queries[1].Scope);
    }

    [Fact]
    public async Task CaptureAsync_ClassifiesNonSceneMatch()
    {
        var reader =
            new ScopeReader(
                scenePages:
                    new[]
                    {
                        Array.Empty<DCMLGameComponentState>()
                    },
                resourcePages:
                    new[]
                    {
                        Page(
                            State(
                                500,
                                "SFP_Slot2.003",
                                true))
                    });

        DataCenterHardwareTopologyGraph graph =
            await CreateTopology(
                    reader,
                    500)
                .CaptureAsync(
                    Query());

        DataCenterHardwareTopologyEdge edge =
            Assert.Single(
                graph.Edges);

        Assert.False(
            edge.TargetResolved);

        Assert.True(
            edge.TargetObserved);

        Assert.Equal(
            DataCenterHardwareTopologyTargetLocation.NonSceneObject,
            edge.TargetLocation);

        Assert.Equal(
            "SFP_Slot2.003",
            edge.ResolvedTargetName);
    }

    [Fact]
    public async Task CaptureAsync_CountsNonSceneMatches()
    {
        var reader =
            new ScopeReader(
                scenePages:
                    new[]
                    {
                        Array.Empty<DCMLGameComponentState>()
                    },
                resourcePages:
                    new[]
                    {
                        Page(
                            State(
                                500,
                                "Target",
                                true))
                    });

        DataCenterHardwareTopologyGraph graph =
            await CreateTopology(
                    reader,
                    500)
                .CaptureAsync(
                    Query());

        Assert.Equal(
            1,
            graph.NonSceneTargetMatchCount);

        Assert.Single(
            graph.ObservedNonSceneEdges);
    }

    [Fact]
    public async Task CaptureAsync_UsesIdentityOnlyForNonSceneProbe()
    {
        var reader =
            new ScopeReader(
                scenePages:
                    new[]
                    {
                        Array.Empty<DCMLGameComponentState>()
                    },
                resourcePages:
                    new[]
                    {
                        Array.Empty<DCMLGameComponentState>()
                    });

        await CreateTopology(
                reader,
                500)
            .CaptureAsync(
                Query());

        DCMLGameComponentStateQuery resourceQuery =
            reader.Queries[1];

        Assert.Empty(
            resourceQuery.MemberNames);
    }

    [Fact]
    public async Task CaptureAsync_DoesNotProbeNonSceneWhenSceneResolves()
    {
        var reader =
            new ScopeReader(
                scenePages:
                    new[]
                    {
                        Page(
                            State(
                                500,
                                "SceneCable",
                                false))
                    },
                resourcePages:
                    Array.Empty<
                        IReadOnlyList<DCMLGameComponentState>>());

        DataCenterHardwareTopologyGraph graph =
            await CreateTopology(
                    reader,
                    500)
                .CaptureAsync(
                    Query());

        Assert.Equal(
            2,
            reader.Queries.Count);

        Assert.Empty(
            reader.Queries[0].MemberNames);

        Assert.NotEmpty(
            reader.Queries[1].MemberNames);

        Assert.DoesNotContain(
            reader.Queries,
            value =>
                value.Scope ==
                    DCMLGameComponentScope.Resource);

        DataCenterHardwareTopologyEdge edge =
            Assert.Single(
                graph.Edges);

        Assert.True(
            edge.TargetResolved);

        Assert.Equal(
            DataCenterHardwareTopologyTargetLocation.SceneObject,
            edge.TargetLocation);
    }

    [Fact]
    public async Task CaptureAsync_SceneResolutionDoesNotRequireNonSceneAssignment()
    {
        var reader =
            new ScopeReader(
                scenePages:
                    new[]
                    {
                        Page(
                            State(
                                500,
                                "SceneCable",
                                false))
                    },
                resourcePages:
                    Array.Empty<
                        IReadOnlyList<DCMLGameComponentState>>());

        DataCenterHardwareTopologyGraph graph =
            await CreateTopology(
                    reader,
                    500)
                .CaptureAsync(
                    Query());

        DataCenterHardwareTopologyEdge edge =
            Assert.Single(
                graph.Edges);

        Assert.True(
            edge.TargetResolved);

        Assert.Equal(
            DataCenterHardwareTopologyTargetLocation.SceneObject,
            edge.TargetLocation);

        Assert.Equal(
            "SceneCable",
            edge.ResolvedTargetName);
    }

    [Fact]
    public async Task CaptureAsync_LeavesMissingTargetUnknown()
    {
        var reader =
            new ScopeReader(
                scenePages:
                    new[]
                    {
                        Array.Empty<DCMLGameComponentState>()
                    },
                resourcePages:
                    new[]
                    {
                        Array.Empty<DCMLGameComponentState>()
                    });

        DataCenterHardwareTopologyGraph graph =
            await CreateTopology(
                    reader,
                    500)
                .CaptureAsync(
                    Query());

        DataCenterHardwareTopologyEdge edge =
            Assert.Single(
                graph.Edges);

        Assert.False(edge.TargetObserved);
        Assert.Equal(
            DataCenterHardwareTopologyTargetLocation.Unknown,
            edge.TargetLocation);
    }

    private static DataCenterHardwareTopology CreateTopology(
        ScopeReader reader,
        int targetInstanceId)
    {
        var snapshot =
            new DataCenterHardwareSnapshotSet(
                Array.Empty<DataCenterServerSnapshot>(),
                Array.Empty<DataCenterRackSnapshot>(),
                Array.Empty<DataCenterNetworkDeviceSnapshot>(),
                new[]
                {
                    new DataCenterSfpModuleSnapshot(
                        100,
                        "SFP_RJ45",
                        "BaseScene",
                        false,
                        2,
                        0,
                        0,
                        false,
                        Ref(
                            targetInstanceId,
                            "SFP_Slot2.003",
                            "Il2Cpp.CableLink"))
                },
                Array.Empty<DataCenterCableSnapshot>());

        return
            new DataCenterHardwareTopology(
                new FakeSnapshots(
                    snapshot),
                reader);
    }

    private static DataCenterHardwareSnapshotQuery Query()
    {
        return
            new DataCenterHardwareSnapshotQuery(
                sceneName:
                    "BaseScene",
                includeSceneObjects:
                    true,
                includeResources:
                    true,
                maxPerType:
                    64);
    }

    private static DCMLGameComponentState State(
        int instanceId,
        string name,
        bool isResource)
    {
        return
            new DCMLGameComponentState(
                instanceId,
                name,
                isResource
                    ? string.Empty
                    : "BaseScene",
                "Root/" + name,
                true,
                isResource,
                "Il2Cpp.CableLink",
                Array.Empty<
                    KeyValuePair<
                        string,
                        DCMLGameValue>>());
    }

    private static IReadOnlyList<DCMLGameComponentState> Page(
        params DCMLGameComponentState[] states)
    {
        return states;
    }

    private static DataCenterHardwareReference Ref(
        int instanceId,
        string name,
        string typeName)
    {
        return
            new DataCenterHardwareReference(
                instanceId,
                name,
                typeName);
    }

    private sealed class FakeSnapshots :
        IDataCenterHardwareSnapshots
    {
        private readonly DataCenterHardwareSnapshotSet
            _snapshot;

        public FakeSnapshots(
            DataCenterHardwareSnapshotSet snapshot)
        {
            _snapshot =
                snapshot;
        }

        public Task<DataCenterHardwareSnapshotSet> CaptureAsync(
            DataCenterHardwareSnapshotQuery query)
        {
            return
                Task.FromResult(
                    _snapshot);
        }
    }

    private sealed class ScopeReader :
        IDCMLGameComponentStateReader
    {
        private readonly Queue<
            IReadOnlyList<DCMLGameComponentState>>
            _scenePages;

        private readonly Queue<
            IReadOnlyList<DCMLGameComponentState>>
            _resourcePages;

        public ScopeReader(
            IEnumerable<IReadOnlyList<DCMLGameComponentState>> scenePages,
            IEnumerable<IReadOnlyList<DCMLGameComponentState>> resourcePages)
        {
            _scenePages =
                new Queue<
                    IReadOnlyList<DCMLGameComponentState>>(
                    scenePages);

            _resourcePages =
                new Queue<
                    IReadOnlyList<DCMLGameComponentState>>(
                    resourcePages);
        }

        public List<DCMLGameComponentStateQuery> Queries { get; } =
            new List<DCMLGameComponentStateQuery>();

        public Task<IReadOnlyList<DCMLGameComponentState>> ReadAsync(
            DCMLGameComponentStateQuery query)
        {
            Queries.Add(query);

            Queue<IReadOnlyList<DCMLGameComponentState>> queue =
                query.Scope ==
                    DCMLGameComponentScope.Resource
                    ? _resourcePages
                    : _scenePages;

            if (queue.Count == 0)
            {
                return
                    Task.FromResult<
                        IReadOnlyList<DCMLGameComponentState>>(
                        Array.Empty<DCMLGameComponentState>());
            }

            return
                Task.FromResult(
                    queue.Dequeue());
        }
    }
}
