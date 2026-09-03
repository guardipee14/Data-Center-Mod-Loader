using System;
using System.Linq;
using DCML.DataCenter;
using DCML.DataCenter.Models;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLPhysicalCableTopologyTests
{
    [Fact]
    public void PersistenceReference_PreservesRealPersistentIdentityWithoutFakeRuntimeId()
    {
        var reference =
            new DataCenterHardwareReference(
                0,
                "Server.Blue1_-100",
                "DataCenter.Persistence.Server",
                "Server.Blue1_-100",
                "save-server");

        Assert.Equal(0, reference.InstanceId);
        Assert.False(reference.HasRuntimeInstance);
        Assert.True(reference.HasPersistentIdentity);
        Assert.Equal("Server.Blue1_-100", reference.PersistentID);
        Assert.Equal("save-server", reference.IdentityKind);
        Assert.Equal(
            "persistent:save-server:Server.Blue1_-100",
            reference.IdentityKey);
    }

    [Fact]
    public void Resolve_Type1UsesServerId()
    {
        DataCenterCablePersistenceIndex index =
            CreateIndex();

        DataCenterPhysicalCableEndpointResolution resolution =
            index.Resolve(
                Endpoint(
                    DataCenterPhysicalCableEndpointSide.Start,
                    1,
                    serverId:
                        "Server.Blue1_-100",
                    switchId:
                        string.Empty,
                    customerId:
                        9));

        Assert.True(resolution.IsResolved);
        Assert.Equal(
            DataCenterPhysicalCableEndpointKind.Server,
            resolution.Kind);
        Assert.Equal(
            "Server.Blue1_-100",
            resolution.PersistentID);
    }

    [Fact]
    public void Resolve_Type2UsesNetworkSideAndDoesNotAllowServerIdToOverrideIt()
    {
        DataCenterCablePersistenceIndex index =
            CreateIndex();

        DataCenterPhysicalCableEndpointResolution resolution =
            index.Resolve(
                Endpoint(
                    DataCenterPhysicalCableEndpointSide.Start,
                    2,
                    serverId:
                        "Server.Stale_-999",
                    switchId:
                        "Router4xQSXP16xSFP 1_-200",
                    customerId:
                        -1));

        Assert.True(resolution.IsResolved);
        Assert.Equal(
            DataCenterPhysicalCableEndpointKind.Router,
            resolution.Kind);
        Assert.Equal(
            "Router4xQSXP16xSFP 1_-200",
            resolution.PersistentID);
        Assert.Equal(
            "Server.Stale_-999",
            resolution.Raw.ServerID);
    }

    [Fact]
    public void Resolve_Type2RecognizesPatchPanelPortIdentity()
    {
        DataCenterCablePersistenceIndex index =
            CreateIndex();

        DataCenterPhysicalCableEndpointResolution resolution =
            index.Resolve(
                Endpoint(
                    DataCenterPhysicalCableEndpointSide.End,
                    2,
                    serverId:
                        string.Empty,
                    switchId:
                        "PatchPanel_-300_4",
                    customerId:
                        -1));

        Assert.True(resolution.IsResolved);
        Assert.Equal(
            DataCenterPhysicalCableEndpointKind.PatchPanelPort,
            resolution.Kind);
        Assert.Equal(
            "PatchPanel_-300_4",
            resolution.PersistentID);
        Assert.Equal(
            "PatchPanel_-300",
            resolution.ParentPersistentID);
    }

    [Fact]
    public void Resolve_Type3UsesCustomerId()
    {
        DataCenterCablePersistenceIndex index =
            CreateIndex();

        DataCenterPhysicalCableEndpointResolution resolution =
            index.Resolve(
                Endpoint(
                    DataCenterPhysicalCableEndpointSide.Start,
                    3,
                    serverId:
                        string.Empty,
                    switchId:
                        string.Empty,
                    customerId:
                        9));

        Assert.True(resolution.IsResolved);
        Assert.Equal(
            DataCenterPhysicalCableEndpointKind.CustomerBase,
            resolution.Kind);
        Assert.Equal(
            "9",
            resolution.PersistentID);
    }

    [Fact]
    public void Build_CreatesOneBidirectionalNetworkConnectionEdgePerPhysicalCable()
    {
        DataCenterHardwareTopologyGraph graph =
            DataCenterPhysicalCableTopology.Build(
                new[]
                {
                    Cable(
                        795,
                        Endpoint(
                            DataCenterPhysicalCableEndpointSide.Start,
                            1,
                            "Server.Blue1_-100",
                            string.Empty,
                            9),
                        Endpoint(
                            DataCenterPhysicalCableEndpointSide.End,
                            2,
                            string.Empty,
                            "PatchPanel_-300_4",
                            -1))
                },
                CreateIndex());

        DataCenterHardwareTopologyEdge edge =
            Assert.Single(
                graph.NetworkConnectionEdges);

        Assert.Equal(
            DataCenterHardwareTopologyRelationships.PhysicalCableConnection,
            edge.Relationship);
        Assert.Equal(
            DataCenterHardwareTopologyEdgeKind.NetworkConnection,
            edge.Kind);
        Assert.Equal(795, edge.PhysicalCableID);
        Assert.True(edge.IsBidirectional);
        Assert.True(edge.IsFullyResolved);
        Assert.Equal(
            DataCenterPhysicalCableTopology.EvidenceSource,
            edge.EvidenceSource);
        Assert.Equal(
            "Server.Blue1_-100",
            edge.Source.PersistentID);
        Assert.Equal(
            "PatchPanel_-300_4",
            edge.Target.PersistentID);
    }

    [Fact]
    public void Build_PreservesParallelPhysicalCablesBetweenSamePersistentEndpoints()
    {
        DataCenterCablePersistenceEndpoint start =
            Endpoint(
                DataCenterPhysicalCableEndpointSide.Start,
                1,
                "Server.Blue1_-100",
                string.Empty,
                9);

        DataCenterCablePersistenceEndpoint end =
            Endpoint(
                DataCenterPhysicalCableEndpointSide.End,
                2,
                string.Empty,
                "Switch4xQSXP16xSFP_-400",
                -1);

        DataCenterHardwareTopologyGraph graph =
            DataCenterPhysicalCableTopology.Build(
                new[]
                {
                    Cable(798, start, end),
                    Cable(799, start, end),
                    Cable(800, start, end)
                },
                CreateIndex());

        Assert.Equal(
            3,
            graph.NetworkConnectionEdges.Count);

        Assert.Equal(
            new[]
            {
                798,
                799,
                800
            },
            graph.NetworkConnectionEdges
                .Select(
                    value =>
                        value.PhysicalCableID!.Value)
                .OrderBy(
                    value =>
                        value)
                .ToArray());

        Assert.Equal(
            2,
            graph.Nodes.Count);
    }

    [Fact]
    public void Combine_PreservesStructuralEdgesAndAddsPhysicalNetworkConnections()
    {
        var live =
            new DataCenterHardwareTopologyGraph(
                Array.Empty<DataCenterHardwareTopologyNode>(),
                new[]
                {
                    new DataCenterHardwareTopologyEdge(
                        DataCenterHardwareTopologyRelationships.SfpModuleInsertion,
                        new DataCenterHardwareReference(
                            101,
                            "SFP",
                            "Il2Cpp.SFPModule"),
                        new DataCenterHardwareReference(
                            201,
                            "SFP Slot",
                            "Il2Cpp.CableLink"),
                        true,
                        kind:
                            DataCenterHardwareTopologyEdgeKind.Structural)
                });

        DataCenterHardwareTopologyGraph combined =
            DataCenterPhysicalCableTopology.Combine(
                live,
                new[]
                {
                    Cable(
                        831,
                        Endpoint(
                            DataCenterPhysicalCableEndpointSide.Start,
                            3,
                            string.Empty,
                            string.Empty,
                            0),
                        Endpoint(
                            DataCenterPhysicalCableEndpointSide.End,
                            2,
                            string.Empty,
                            "Router4xQSXP16xSFP 1_-200",
                            -1))
                },
                CreateIndex());

        Assert.Single(
            combined.StructuralEdges);

        DataCenterHardwareTopologyEdge physical =
            Assert.Single(
                combined.NetworkConnectionEdges);

        Assert.Equal(
            831,
            physical.PhysicalCableID);

        Assert.Equal(
            "0",
            physical.Source.PersistentID);

        Assert.Equal(
            "Router4xQSXP16xSFP 1_-200",
            physical.Target.PersistentID);
    }

    private static DataCenterCablePersistenceIndex CreateIndex()
    {
        return
            new DataCenterCablePersistenceIndex(
                serverIds:
                    new[]
                    {
                        "Server.Blue1_-100"
                    },
                switchIds:
                    new[]
                    {
                        "Switch4xQSXP16xSFP_-400",
                        "Router4xQSXP16xSFP 1_-200",
                        "Firewall_-500"
                    },
                routerIds:
                    new[]
                    {
                        "Router4xQSXP16xSFP 1_-200"
                    },
                firewallIds:
                    new[]
                    {
                        "Firewall_-500"
                    },
                patchPanelIds:
                    new[]
                    {
                        "PatchPanel_-300"
                    },
                customerIds:
                    Enumerable.Range(
                        0,
                        10));
    }

    private static DataCenterCablePersistenceRecord Cable(
        int cableId,
        DataCenterCablePersistenceEndpoint start,
        DataCenterCablePersistenceEndpoint end)
    {
        return
            new DataCenterCablePersistenceRecord(
                cableId,
                start,
                end);
    }

    private static DataCenterCablePersistenceEndpoint Endpoint(
        DataCenterPhysicalCableEndpointSide side,
        int linkType,
        string serverId,
        string switchId,
        int? customerId)
    {
        return
            new DataCenterCablePersistenceEndpoint(
                side,
                linkType,
                serverId,
                switchId,
                customerId,
                position:
                    "1,2,3");
    }
}
