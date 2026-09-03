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

public sealed class DCMLComponentIdentityAlignmentTests
{
    [Fact]
    public void ComponentState_LegacyConstructorFallsBackToGameObjectId()
    {
        DCMLGameComponentState state =
            State(
                gameObjectId:
                    10,
                componentId:
                    null);

        Assert.Equal(
            10,
            state.InstanceId);

        Assert.Equal(
            10,
            state.GameObjectInstanceId);

        Assert.Equal(
            10,
            state.ComponentInstanceId);
    }

    [Fact]
    public void ComponentState_PreservesSeparateComponentIdentity()
    {
        DCMLGameComponentState state =
            State(
                gameObjectId:
                    10,
                componentId:
                    20);

        Assert.Equal(
            10,
            state.InstanceId);

        Assert.Equal(
            10,
            state.GameObjectInstanceId);

        Assert.Equal(
            20,
            state.ComponentInstanceId);
    }

    [Fact]
    public void HardwareSnapshot_PreservesBothIdentities()
    {
        var snapshot =
            new DataCenterSfpModuleSnapshot(
                instanceId:
                    100,
                name:
                    "SFP_RJ45",
                sceneName:
                    "BaseScene",
                isResource:
                    false,
                speed:
                    2,
                sfpType:
                    0,
                positionInBox:
                    0,
                isInTheBox:
                    false,
                link:
                    null,
                componentInstanceId:
                    200);

        Assert.Equal(
            100,
            snapshot.GameObjectInstanceId);

        Assert.Equal(
            200,
            snapshot.ComponentInstanceId);
    }

    [Fact]
    public async Task HardwareService_MapsComponentIdentityIntoSfpSnapshot()
    {
        var reader =
            new SingleStateReader(
                "Il2Cpp.SFPModule",
                State(
                    100,
                    200,
                    "Il2Cpp.SFPModule"));

        var service =
            new DataCenterHardwareSnapshots(
                reader);

        DataCenterHardwareSnapshotSet set =
            await service.CaptureAsync(
                new DataCenterHardwareSnapshotQuery(
                    sceneName:
                        "BaseScene",
                    includeSceneObjects:
                        true,
                    includeResources:
                        false));

        DataCenterSfpModuleSnapshot sfp =
            Assert.Single(
                set.SfpModules);

        Assert.Equal(
            100,
            sfp.GameObjectInstanceId);

        Assert.Equal(
            200,
            sfp.ComponentInstanceId);
    }

    [Fact]
    public async Task HardwareService_MapsComponentIdentityIntoCableSnapshot()
    {
        var reader =
            new SingleStateReader(
                "Il2Cpp.CableLink",
                State(
                    300,
                    400,
                    "Il2Cpp.CableLink"));

        var service =
            new DataCenterHardwareSnapshots(
                reader);

        DataCenterHardwareSnapshotSet set =
            await service.CaptureAsync(
                new DataCenterHardwareSnapshotQuery(
                    sceneName:
                        "BaseScene",
                    includeSceneObjects:
                        true,
                    includeResources:
                        false));

        DataCenterCableSnapshot cable =
            Assert.Single(
                set.Cables);

        Assert.Equal(
            300,
            cable.GameObjectInstanceId);

        Assert.Equal(
            400,
            cable.ComponentInstanceId);
    }

    [Fact]
    public void TopologyBuild_ResolvesByComponentIdentityNotGameObjectIdentity()
    {
        var target =
            new DataCenterHardwareReference(
                900,
                "SFP_Slot2.003",
                "Il2Cpp.CableLink");

        var sfp =
            new DataCenterSfpModuleSnapshot(
                100,
                "SFP_RJ45",
                "BaseScene",
                false,
                2,
                0,
                0,
                false,
                target,
                componentInstanceId:
                    700);

        var cable =
            new DataCenterCableSnapshot(
                200,
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
                componentInstanceId:
                    900);

        DataCenterHardwareTopologyGraph graph =
            DataCenterHardwareTopology.Build(
                new DataCenterHardwareSnapshotSet(
                    Array.Empty<DataCenterServerSnapshot>(),
                    Array.Empty<DataCenterRackSnapshot>(),
                    Array.Empty<DataCenterNetworkDeviceSnapshot>(),
                    new[] { sfp },
                    new[] { cable }));

        DataCenterHardwareTopologyEdge edge =
            Assert.Single(
                graph.Edges);

        Assert.True(
            edge.TargetResolved);

        Assert.Equal(
            900,
            edge.Target.InstanceId);
    }

    [Fact]
    public void TopologyNode_UsesSfpComponentIdentity()
    {
        var sfp =
            new DataCenterSfpModuleSnapshot(
                100,
                "SFP_RJ45",
                "BaseScene",
                false,
                2,
                0,
                0,
                false,
                link:
                    null,
                componentInstanceId:
                    700);

        DataCenterHardwareTopologyGraph graph =
            DataCenterHardwareTopology.Build(
                new DataCenterHardwareSnapshotSet(
                    Array.Empty<DataCenterServerSnapshot>(),
                    Array.Empty<DataCenterRackSnapshot>(),
                    Array.Empty<DataCenterNetworkDeviceSnapshot>(),
                    new[] { sfp },
                    Array.Empty<DataCenterCableSnapshot>()));

        DataCenterHardwareTopologyNode node =
            Assert.Single(
                graph.Nodes);

        Assert.Equal(
            700,
            node.InstanceId);
    }

    [Fact]
    public async Task PagedTopologySearch_MatchesLowLevelComponentIdentity()
    {
        var target =
            new DataCenterHardwareReference(
                900,
                "SFP_Slot2.003",
                "Il2Cpp.CableLink");

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
                        target,
                        componentInstanceId:
                            700)
                },
                Array.Empty<DataCenterCableSnapshot>());

        var lowLevelReader =
            new SingleStateReader(
                "Il2Cpp.CableLink",
                State(
                    gameObjectId:
                        123,
                    componentId:
                        900,
                    componentTypeName:
                        "Il2Cpp.CableLink"));

        var topology =
            new DataCenterHardwareTopology(
                new FixedSnapshots(
                    snapshot),
                lowLevelReader);

        DataCenterHardwareTopologyGraph graph =
            await topology.CaptureAsync(
                new DataCenterHardwareSnapshotQuery(
                    sceneName:
                        "BaseScene",
                    includeSceneObjects:
                        true,
                    includeResources:
                        true,
                    maxPerType:
                        64));

        DataCenterHardwareTopologyEdge edge =
            Assert.Single(
                graph.Edges);

        Assert.True(
            edge.TargetResolved);

        Assert.Equal(
            DataCenterHardwareTopologyTargetLocation.SceneObject,
            edge.TargetLocation);
    }

    private static DCMLGameComponentState State(
        int gameObjectId,
        int? componentId,
        string componentTypeName =
            "Il2Cpp.SFPModule")
    {
        return
            new DCMLGameComponentState(
                gameObjectId,
                "Object",
                "BaseScene",
                "Root/Object",
                true,
                false,
                componentTypeName,
                Array.Empty<
                    KeyValuePair<
                        string,
                        DCMLGameValue>>(),
                componentId);
    }

    private sealed class SingleStateReader :
        IDCMLGameComponentStateReader
    {
        private readonly string _typeName;
        private readonly DCMLGameComponentState _state;

        public SingleStateReader(
            string typeName,
            DCMLGameComponentState state)
        {
            _typeName =
                typeName;

            _state =
                state;
        }

        public Task<IReadOnlyList<DCMLGameComponentState>> ReadAsync(
            DCMLGameComponentStateQuery query)
        {
            if (
                string.Equals(
                    query.ComponentTypeName,
                    _typeName,
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                return
                    Task.FromResult<
                        IReadOnlyList<DCMLGameComponentState>>(
                        new[] { _state });
            }

            return
                Task.FromResult<
                    IReadOnlyList<DCMLGameComponentState>>(
                    Array.Empty<DCMLGameComponentState>());
        }
    }

    private sealed class FixedSnapshots :
        IDataCenterHardwareSnapshots
    {
        private readonly DataCenterHardwareSnapshotSet _snapshot;

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
}
