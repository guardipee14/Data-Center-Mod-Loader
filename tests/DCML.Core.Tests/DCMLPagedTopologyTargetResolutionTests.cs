using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using DCML.DataCenter;
using DCML.DataCenter.Abstractions;
using DCML.DataCenter.Models;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLPagedTopologyTargetResolutionTests
{
    [Fact]
    public async Task CaptureAsync_ResolvesTargetFromPagedReader()
    {
        var reader =
            new FakeReader(
                Page(
                    State(
                        500,
                        "CableTarget")));

        var topology =
            CreateTopology(
                reader,
                targetInstanceId:
                    500);

        DataCenterHardwareTopologyGraph graph =
            await topology.CaptureAsync(
                Query());

        DataCenterHardwareTopologyEdge edge =
            Assert.Single(
                graph.Edges);

        Assert.True(
            edge.TargetResolved);

        Assert.Equal(
            "CableTarget",
            edge.ResolvedTargetName);
    }

    [Fact]
    public async Task CaptureAsync_UsesInstanceIdInsteadOfName()
    {
        var reader =
            new FakeReader(
                Page(
                    State(
                        500,
                        "CompletelyDifferentName")));

        var topology =
            CreateTopology(
                reader,
                targetInstanceId:
                    500,
                targetName:
                    "SFP_Slot2.003");

        DataCenterHardwareTopologyGraph graph =
            await topology.CaptureAsync(
                Query());

        DataCenterHardwareTopologyEdge edge =
            Assert.Single(
                graph.Edges);

        Assert.True(
            edge.TargetResolved);

        Assert.Equal(
            "CompletelyDifferentName",
            edge.ResolvedTargetName);
    }

    [Fact]
    public async Task CaptureAsync_RequestsIdentityOnlyCableState()
    {
        var reader =
            new FakeReader(
                Page(
                    State(
                        500,
                        "Cable")));

        DataCenterHardwareTopology topology =
            CreateTopology(
                reader,
                500);

        await topology.CaptureAsync(
            Query());

        DCMLGameComponentStateQuery lowLevelQuery =
            reader.Queries.Single(
                value =>
                    value.MemberNames.Count == 0);

        Assert.Empty(
            lowLevelQuery.MemberNames);

        Assert.Equal(
            "Il2Cpp.CableLink",
            lowLevelQuery.ComponentTypeName);
    }

    [Fact]
    public async Task CaptureAsync_UsesSceneScopeAndSceneName()
    {
        var reader =
            new FakeReader(
                Page(
                    State(
                        500,
                        "Cable")));

        DataCenterHardwareTopology topology =
            CreateTopology(
                reader,
                500);

        await topology.CaptureAsync(
            Query(
                "BaseScene"));

        DCMLGameComponentStateQuery lowLevelQuery =
            reader.Queries.Single(
                value =>
                    value.MemberNames.Count == 0);

        Assert.Equal(
            DCMLGameComponentScope.Scene,
            lowLevelQuery.Scope);

        Assert.Equal(
            "BaseScene",
            lowLevelQuery.SceneName);
    }

    [Fact]
    public async Task CaptureAsync_StopsWhenAllTargetsAreFound()
    {
        var reader =
            new FakeReader(
                Page(
                    State(
                        500,
                        "Cable")),
                Page(
                    State(
                        600,
                        "Unused")));

        DataCenterHardwareTopology topology =
            CreateTopology(
                reader,
                500);

        DataCenterHardwareTopologyGraph graph =
            await topology.CaptureAsync(
                Query());

        Assert.Single(
            reader.Queries.Where(
                value =>
                    value.MemberNames.Count == 0));

        Assert.Equal(
            1,
            graph.CableSearchPages);
    }

    [Fact]
    public async Task CaptureAsync_MarksMissingTargetUnresolvedAfterExhaustion()
    {
        var reader =
            new FakeReader(
                Array.Empty<DCMLGameComponentState>());

        DataCenterHardwareTopology topology =
            CreateTopology(
                reader,
                500);

        DataCenterHardwareTopologyGraph graph =
            await topology.CaptureAsync(
                Query());

        Assert.Single(
            graph.UnresolvedEdges);

        Assert.True(
            graph.CableSearchExhausted);
    }

    [Fact]
    public async Task CaptureAsync_RecordsSearchDiagnostics()
    {
        var reader =
            new FakeReader(
                Page(
                    State(
                        1,
                        "Other"),
                    State(
                        500,
                        "Cable")));

        DataCenterHardwareTopology topology =
            CreateTopology(
                reader,
                500);

        DataCenterHardwareTopologyGraph graph =
            await topology.CaptureAsync(
                Query());

        Assert.Equal(
            1,
            graph.CableSearchPages);

        Assert.Equal(
            2,
            graph.CableCandidatesScanned);
    }

    [Fact]
    public async Task CaptureAsync_DoesNotPageWhenSceneObjectsAreDisabled()
    {
        var reader =
            new FakeReader(
                Page(
                    State(
                        500,
                        "Cable")));

        DataCenterHardwareTopology topology =
            CreateTopology(
                reader,
                500);

        DataCenterHardwareTopologyGraph graph =
            await topology.CaptureAsync(
                new DataCenterHardwareSnapshotQuery(
                    includeSceneObjects:
                        false,
                    includeResources:
                        true));

        Assert.Empty(
            reader.Queries);

        Assert.Empty(
            graph.Edges);
    }

    private static DataCenterHardwareTopology CreateTopology(
        FakeReader reader,
        int targetInstanceId,
        string targetName =
            "SFP_Slot2.003")
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
                        new DataCenterHardwareReference(
                            targetInstanceId,
                            targetName,
                            "Il2Cpp.CableLink"))
                },
                Array.Empty<DataCenterCableSnapshot>());

        return
            new DataCenterHardwareTopology(
                new FakeSnapshots(
                    snapshot),
                reader);
    }

    private static DataCenterHardwareSnapshotQuery Query(
        string sceneName =
            "BaseScene")
    {
        return
            new DataCenterHardwareSnapshotQuery(
                sceneName:
                    sceneName,
                includeSceneObjects:
                    true,
                includeResources:
                    true,
                maxPerType:
                    64);
    }

    private static DCMLGameComponentState State(
        int instanceId,
        string name)
    {
        return
            new DCMLGameComponentState(
                instanceId,
                name,
                "BaseScene",
                "Root/" + name,
                true,
                false,
                "Il2Cpp.CableLink",
                Array.Empty<
                    KeyValuePair<
                        string,
                        DCMLGameValue>>());
    }

    private static IReadOnlyList<DCMLGameComponentState> Page(
        params DCMLGameComponentState[] states)
    {
        return
            states;
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
            if (!query.IncludeSceneObjects)
            {
                return
                    Task.FromResult(
                        new DataCenterHardwareSnapshotSet(
                            Array.Empty<DataCenterServerSnapshot>(),
                            Array.Empty<DataCenterRackSnapshot>(),
                            Array.Empty<DataCenterNetworkDeviceSnapshot>(),
                            Array.Empty<DataCenterSfpModuleSnapshot>(),
                            Array.Empty<DataCenterCableSnapshot>()));
            }

            return
                Task.FromResult(
                    _snapshot);
        }
    }

    private sealed class FakeReader :
        IDCMLGameComponentStateReader
    {
        private readonly Queue<
            IReadOnlyList<DCMLGameComponentState>>
            _pages;

        public FakeReader(
            params IReadOnlyList<DCMLGameComponentState>[] pages)
        {
            _pages =
                new Queue<
                    IReadOnlyList<DCMLGameComponentState>>(
                    pages);
        }

        public List<DCMLGameComponentStateQuery> Queries { get; } =
            new List<DCMLGameComponentStateQuery>();

        public Task<IReadOnlyList<DCMLGameComponentState>> ReadAsync(
            DCMLGameComponentStateQuery query)
        {
            Queries.Add(
                query);

            if (_pages.Count == 0)
            {
                return
                    Task.FromResult<
                        IReadOnlyList<DCMLGameComponentState>>(
                        Array.Empty<DCMLGameComponentState>());
            }

            return
                Task.FromResult(
                    _pages.Dequeue());
        }
    }
}
