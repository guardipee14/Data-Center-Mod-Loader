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

public sealed class DCMLTargetedCableEndpointProbeTests
{
    [Fact]
    public void Query_PreservesDistinctComponentInstanceIds()
    {
        var query =
            new DCMLGameComponentStateQuery(
                "Il2Cpp.CableLink",
                componentInstanceIds:
                    new[] { 10, 20, 10 });

        Assert.Equal(
            new[] { 10, 20 },
            query.ComponentInstanceIds);
    }

    [Fact]
    public void Query_DefaultsToNoComponentFilter()
    {
        var query =
            new DCMLGameComponentStateQuery(
                "Il2Cpp.CableLink");

        Assert.Empty(
            query.ComponentInstanceIds);
    }

    [Fact]
    public void Edge_CanCarryTargetCableDetail()
    {
        DataCenterCableSnapshot cable =
            Cable(
                gameObjectId:
                    10,
                componentId:
                    20);

        var edge =
            new DataCenterHardwareTopologyEdge(
                "sfp-link",
                Ref(1, "SFP", "Il2Cpp.SFPModule"),
                Ref(20, "Slot", "Il2Cpp.CableLink"),
                true,
                "Slot",
                DataCenterHardwareTopologyTargetLocation.SceneObject,
                cable);

        Assert.Same(
            cable,
            edge.TargetCable);
    }

    [Fact]
    public async Task CaptureAsync_RequestsExactResolvedCableIdsForDetails()
    {
        var reader =
            new RoutingReader(
                resolvedComponentId:
                    900);

        DataCenterHardwareTopology topology =
            CreateTopology(
                reader,
                targetComponentId:
                    900);

        await topology.CaptureAsync(
            Query());

        DCMLGameComponentStateQuery detailQuery =
            reader.Queries.Single(
                value =>
                    value.MemberNames.Count > 0);

        Assert.Equal(
            new[] { 900 },
            detailQuery.ComponentInstanceIds);
    }

    [Fact]
    public async Task CaptureAsync_RequestsProvenCableRelationshipMembers()
    {
        var reader =
            new RoutingReader(
                resolvedComponentId:
                    900);

        DataCenterHardwareTopology topology =
            CreateTopology(
                reader,
                900);

        await topology.CaptureAsync(
            Query());

        DCMLGameComponentStateQuery detailQuery =
            reader.Queries.Single(
                value =>
                    value.MemberNames.Count > 0);

        Assert.Contains(
            "parentServer",
            detailQuery.MemberNames);

        Assert.Contains(
            "parentSwitch",
            detailQuery.MemberNames);

        Assert.Contains(
            "parentPatchPanel",
            detailQuery.MemberNames);

        Assert.Contains(
            "parentInternet",
            detailQuery.MemberNames);

        Assert.Contains(
            "insertedSFP",
            detailQuery.MemberNames);
    }

    [Fact]
    public async Task CaptureAsync_MapsTargetParentSwitchReference()
    {
        var reader =
            new RoutingReader(
                900,
                parentSwitch:
                    Ref(
                        1000,
                        "SwitchPort",
                        "Il2Cpp.NetworkSwitch"));

        DataCenterHardwareTopologyGraph graph =
            await CreateTopology(
                    reader,
                    900)
                .CaptureAsync(
                    Query());

        DataCenterHardwareTopologyEdge edge =
            Assert.Single(
                graph.Edges);

        Assert.Equal(
            1000,
            edge.TargetCable!.ParentSwitch!.InstanceId);
    }

    [Fact]
    public async Task CaptureAsync_ReportsRequestedAndFoundDetailCounts()
    {
        var reader =
            new RoutingReader(
                resolvedComponentId:
                    900);

        DataCenterHardwareTopologyGraph graph =
            await CreateTopology(
                    reader,
                    900)
                .CaptureAsync(
                    Query());

        Assert.Equal(
            1,
            graph.TargetedCableDetailRequestedCount);

        Assert.Equal(
            1,
            graph.TargetedCableDetailFoundCount);
    }

    [Fact]
    public async Task CaptureAsync_DetailIdentityMatchesResolvedTarget()
    {
        var reader =
            new RoutingReader(
                resolvedComponentId:
                    900);

        DataCenterHardwareTopologyGraph graph =
            await CreateTopology(
                    reader,
                    900)
                .CaptureAsync(
                    Query());

        DataCenterHardwareTopologyEdge edge =
            Assert.Single(
                graph.Edges);

        Assert.Equal(
            edge.Target.InstanceId,
            edge.TargetCable!.ComponentInstanceId);
    }

    private static DataCenterHardwareTopology CreateTopology(
        RoutingReader reader,
        int targetComponentId)
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
                            targetComponentId,
                            "SFP_Slot2.003",
                            "Il2Cpp.CableLink"),
                        componentInstanceId:
                            700)
                },
                Array.Empty<DataCenterCableSnapshot>());

        return
            new DataCenterHardwareTopology(
                new FixedSnapshots(
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

    private static DataCenterCableSnapshot Cable(
        int gameObjectId,
        int componentId,
        DataCenterHardwareReference? parentSwitch = null)
    {
        return
            new DataCenterCableSnapshot(
                gameObjectId,
                "SFP_Slot2.003",
                "BaseScene",
                false,
                0,
                0,
                2,
                false,
                false,
                true,
                false,
                0,
                0,
                string.Empty,
                "None",
                parentSwitch:
                    parentSwitch,
                componentInstanceId:
                    componentId);
    }

    private static DCMLGameComponentState CableState(
        int gameObjectId,
        int componentId,
        DataCenterHardwareReference? parentSwitch)
    {
        var values =
            new List<KeyValuePair<string, DCMLGameValue>>();

        if (parentSwitch is not null)
        {
            values.Add(
                new KeyValuePair<string, DCMLGameValue>(
                    "parentSwitch",
                    new DCMLGameValue(
                        DCMLGameValueKind.Reference,
                        parentSwitch.TypeName,
                        referenceValue:
                            new DCMLGameReference(
                                parentSwitch.InstanceId,
                                parentSwitch.Name,
                                parentSwitch.TypeName))));
        }

        return
            new DCMLGameComponentState(
                gameObjectId,
                "SFP_Slot2.003",
                "BaseScene",
                "Root/SFP_Slot2.003",
                true,
                false,
                "Il2Cpp.CableLink",
                values,
                componentId);
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

    private sealed class FixedSnapshots :
        IDataCenterHardwareSnapshots
    {
        private readonly DataCenterHardwareSnapshotSet
            _snapshot;

        public FixedSnapshots(
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

    private sealed class RoutingReader :
        IDCMLGameComponentStateReader
    {
        private readonly int
            _resolvedComponentId;

        private readonly DataCenterHardwareReference?
            _parentSwitch;

        public RoutingReader(
            int resolvedComponentId,
            DataCenterHardwareReference? parentSwitch = null)
        {
            _resolvedComponentId =
                resolvedComponentId;

            _parentSwitch =
                parentSwitch;
        }

        public List<DCMLGameComponentStateQuery> Queries { get; } =
            new List<DCMLGameComponentStateQuery>();

        public Task<IReadOnlyList<DCMLGameComponentState>> ReadAsync(
            DCMLGameComponentStateQuery query)
        {
            Queries.Add(
                query);

            DCMLGameComponentState state =
                CableState(
                    gameObjectId:
                        123,
                    componentId:
                        _resolvedComponentId,
                    parentSwitch:
                        _parentSwitch);

            if (query.MemberNames.Count > 0)
            {
                if (
                    query.ComponentInstanceIds.Contains(
                        _resolvedComponentId)
                )
                {
                    return
                        Task.FromResult<
                            IReadOnlyList<DCMLGameComponentState>>(
                            new[] { state });
                }

                return
                    Task.FromResult<
                        IReadOnlyList<DCMLGameComponentState>>(
                        Array.Empty<DCMLGameComponentState>());
            }

            // Identity search resolves immediately on the first page.
            return
                Task.FromResult<
                    IReadOnlyList<DCMLGameComponentState>>(
                    new[] { state });
        }
    }
}
