using System;
using DCML.DataCenter;
using DCML.DataCenter.Models;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLLiveHardwareTopologyGraphTests
{
    [Fact]
    public void Node_PreservesStableIdentity()
    {
        var node =
            new DataCenterHardwareTopologyNode(
                12,
                "SFP_RJ45",
                "Il2Cpp.SFPModule",
                "sfp");

        Assert.Equal(12, node.InstanceId);
        Assert.Equal("SFP_RJ45", node.Name);
        Assert.Equal("Il2Cpp.SFPModule", node.TypeName);
        Assert.Equal("sfp", node.Kind);
    }

    [Fact]
    public void Edge_RequiresRelationship()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new DataCenterHardwareTopologyEdge(
                    " ",
                    Ref(1, "SFP", "Il2Cpp.SFPModule"),
                    Ref(2, "Cable", "Il2Cpp.CableLink"),
                    true));
    }

    [Fact]
    public void Graph_SplitsResolvedAndUnresolvedEdges()
    {
        var graph =
            new DataCenterHardwareTopologyGraph(
                Array.Empty<DataCenterHardwareTopologyNode>(),
                new[]
                {
                    new DataCenterHardwareTopologyEdge(
                        "test-link",
                        Ref(1, "SFP1", "Il2Cpp.SFPModule"),
                        Ref(10, "Cable1", "Il2Cpp.CableLink"),
                        true),
                    new DataCenterHardwareTopologyEdge(
                        "test-link",
                        Ref(2, "SFP2", "Il2Cpp.SFPModule"),
                        Ref(20, "Cable2", "Il2Cpp.CableLink"),
                        false)
                });

        Assert.Single(graph.ResolvedEdges);
        Assert.Single(graph.UnresolvedEdges);
    }

    [Fact]
    public void Build_UsesOnlyLiveSfpAndSfpSlotInstancesAsNodes()
    {
        DataCenterHardwareTopologyGraph graph =
            DataCenterHardwareTopology.Build(
                CreateSnapshot(true, true));

        Assert.Equal(2, graph.Nodes.Count);
        Assert.Contains(graph.Nodes, node => node.Kind == "sfp");
        Assert.Contains(graph.Nodes, node => node.Kind == "sfp-slot");
    }

    [Fact]
    public void Build_CreatesSfpModuleInsertionEdge()
    {
        DataCenterHardwareTopologyGraph graph =
            DataCenterHardwareTopology.Build(
                CreateSnapshot(false, true));

        DataCenterHardwareTopologyEdge edge =
            Assert.Single(graph.Edges);

        Assert.Equal(DataCenterHardwareTopologyRelationships.SfpModuleInsertion, edge.Relationship);
        Assert.Equal(101, edge.Source.InstanceId);
        Assert.Equal(201, edge.Target.InstanceId);
        Assert.Equal(
            DataCenterHardwareTopologyEdgeKind.Structural,
            edge.Kind);
        Assert.False(edge.IsNetworkConnection);
        Assert.Single(graph.StructuralEdges);
        Assert.Empty(graph.NetworkConnectionEdges);
    }

    [Fact]
    public void Build_ResolvesTargetByInstanceIdNotName()
    {
        DataCenterHardwareTopologyGraph graph =
            DataCenterHardwareTopology.Build(
                CreateSnapshot(false, true));

        DataCenterHardwareTopologyEdge edge =
            Assert.Single(graph.Edges);

        Assert.True(edge.TargetResolved);
        Assert.Equal("CableLink (201)", edge.ResolvedTargetName);
        Assert.NotEqual(edge.Target.Name, edge.ResolvedTargetName);
    }

    [Fact]
    public void Build_MarksMissingCableTargetUnresolved()
    {
        DataCenterHardwareTopologyGraph graph =
            DataCenterHardwareTopology.Build(
                CreateSnapshot(false, false));

        DataCenterHardwareTopologyEdge edge =
            Assert.Single(graph.Edges);

        Assert.False(edge.TargetResolved);
        Assert.Equal(string.Empty, edge.ResolvedTargetName);
    }

    [Fact]
    public void Build_DoesNotInferEdgesFromNullReferences()
    {
        var sfp =
            new DataCenterSfpModuleSnapshot(
                101,
                "SFP_RJ45",
                "BaseScene",
                false,
                2,
                0,
                0,
                false,
                link: null);

        var graph =
            DataCenterHardwareTopology.Build(
                new DataCenterHardwareSnapshotSet(
                    Array.Empty<DataCenterServerSnapshot>(),
                    Array.Empty<DataCenterRackSnapshot>(),
                    Array.Empty<DataCenterNetworkDeviceSnapshot>(),
                    new[] { sfp },
                    Array.Empty<DataCenterCableSnapshot>()));

        Assert.Empty(graph.Edges);
    }

    private static DataCenterHardwareSnapshotSet CreateSnapshot(
        bool includeDefinition,
        bool targetResolved)
    {
        var sfp =
            new DataCenterSfpModuleSnapshot(
                101,
                "SFP_RJ45",
                "BaseScene",
                false,
                2,
                0,
                0,
                false,
                Ref(
                    201,
                    "SFP_Slot2.003",
                    "Il2Cpp.CableLink"));

        DataCenterCableSnapshot[] cables =
            targetResolved
                ? new[]
                {
                    new DataCenterCableSnapshot(
                        201,
                        "CableLink (201)",
                        "BaseScene",
                        false,
                        0,
                        0,
                        2,
                        true,
                        false,
                        false,
                        true,
                        0,
                        0,
                        string.Empty,
                        "None")
                }
                : Array.Empty<DataCenterCableSnapshot>();

        DataCenterServerSnapshot[] servers =
            includeDefinition
                ? new[]
                {
                    new DataCenterServerSnapshot(
                        301,
                        "Server.Blue1",
                        string.Empty,
                        true,
                        "0.0.0.0",
                        string.Empty,
                        0,
                        0,
                        0.05,
                        false,
                        false,
                        0,
                        0)
                }
                : Array.Empty<DataCenterServerSnapshot>();

        return
            new DataCenterHardwareSnapshotSet(
                servers,
                Array.Empty<DataCenterRackSnapshot>(),
                Array.Empty<DataCenterNetworkDeviceSnapshot>(),
                new[] { sfp },
                cables);
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
}
