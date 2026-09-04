using System;
using System.Linq;
using DCML.DataCenter;
using DCML.DataCenter.Models;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLPhysicalPathReasoningTests
{
    [Fact]
    public void CanTraverse_RequiresResolvedPersistentPhysicalEvidence()
    {
        DataCenterHardwareTopologyEdge edge =
            PhysicalEdge(
                "server:a",
                "switch:b",
                100);

        Assert.True(
            DataCenterPhysicalPathReasoning.CanTraversePhysicalEdge(
                edge));
    }

    [Fact]
    public void CanTraverse_RejectsUnresolvedPhysicalEvidence()
    {
        DataCenterHardwareTopologyEdge edge =
            PhysicalEdge(
                "server:a",
                "switch:b",
                101,
                targetResolved:
                    false);

        Assert.False(
            DataCenterPhysicalPathReasoning.CanTraversePhysicalEdge(
                edge));
    }

    [Fact]
    public void CanTraverse_RejectsPhysicalEvidenceWithoutProvenance()
    {
        DataCenterHardwareTopologyEdge edge =
            PhysicalEdge(
                "server:a",
                "switch:b",
                102,
                evidenceSource:
                    string.Empty);

        Assert.False(
            DataCenterPhysicalPathReasoning.CanTraversePhysicalEdge(
                edge));
    }

    [Fact]
    public void FindPath_FollowsEvidenceBackedPhysicalCableChain()
    {
        DataCenterHardwareTopologyGraph graph =
            Graph(
                PhysicalEdge(
                    "server:a",
                    "switch:b",
                    200),
                PhysicalEdge(
                    "switch:b",
                    "router:c",
                    201));

        DataCenterPhysicalPathResult result =
            DataCenterPhysicalPathReasoning.FindPath(
                graph,
                PersistentReference(
                    "server:a",
                    "server").IdentityKey,
                PersistentReference(
                    "router:c",
                    "router").IdentityKey);

        Assert.True(
            result.Found);

        Assert.Equal(
            2,
            result.HopCount);

        Assert.Collection(
            result.Steps,
            step =>
                Assert.Equal(
                    200,
                    step.PhysicalCableID),
            step =>
                Assert.Equal(
                    201,
                    step.PhysicalCableID));

        Assert.Single(
            result.EvidenceSources);

        Assert.Equal(
            DataCenterPhysicalCableTopology.EvidenceSource,
            result.EvidenceSources[0]);
    }

    [Fact]
    public void FindPath_DoesNotUseLiveStructuralInsertionAsPhysicalBridge()
    {
        DataCenterHardwareReference server =
            PersistentReference(
                "server:a",
                "server");

        DataCenterHardwareReference switchReference =
            PersistentReference(
                "switch:b",
                "switch");

        DataCenterHardwareReference runtimeSfp =
            new(
                9001,
                "SFP",
                "Il2Cpp.SFPModule");

        DataCenterHardwareReference runtimeSlot =
            new(
                9002,
                "SFP Slot",
                "Il2Cpp.CableLink");

        DataCenterHardwareTopologyGraph graph =
            Graph(
                PhysicalEdge(
                    "server:a",
                    "switch:b",
                    300),
                new DataCenterHardwareTopologyEdge(
                    relationship:
                        DataCenterHardwareTopologyRelationships.SfpModuleInsertion,
                    source:
                        runtimeSfp,
                    target:
                        runtimeSlot,
                    targetResolved:
                        true,
                    kind:
                        DataCenterHardwareTopologyEdgeKind.Structural));

        DataCenterPhysicalPathResult result =
            DataCenterPhysicalPathReasoning.FindPath(
                graph,
                server.IdentityKey,
                PersistentReference(
                    "router:c",
                    "router").IdentityKey);

        Assert.False(
            result.Found);

        Assert.Empty(
            result.Steps);

        Assert.Single(
            DataCenterPhysicalPathReasoning.GetLiveStructuralEvidence(
                graph));

        Assert.Equal(
            switchReference.IdentityKey,
            graph.PhysicalCableEdges[0].Target.IdentityKey);
    }

    [Fact]
    public void FindPath_DoesNotGuessAcrossDisconnectedPersistentEndpoints()
    {
        DataCenterHardwareTopologyGraph graph =
            Graph(
                PhysicalEdge(
                    "server:a",
                    "switch:b",
                    400),
                PhysicalEdge(
                    "router:c",
                    "firewall:d",
                    401));

        DataCenterPhysicalPathResult result =
            DataCenterPhysicalPathReasoning.FindPath(
                graph,
                PersistentReference(
                    "server:a",
                    "server").IdentityKey,
                PersistentReference(
                    "firewall:d",
                    "firewall").IdentityKey);

        Assert.False(
            result.Found);

        Assert.Empty(
            result.Steps);
    }

    [Fact]
    public void IncompletePhysicalEvidence_RemainsVisibleButNotTraversable()
    {
        DataCenterHardwareTopologyEdge complete =
            PhysicalEdge(
                "server:a",
                "switch:b",
                500);

        DataCenterHardwareTopologyEdge incomplete =
            PhysicalEdge(
                "switch:b",
                "router:c",
                501,
                sourceResolved:
                    false);

        DataCenterHardwareTopologyGraph graph =
            Graph(
                complete,
                incomplete);

        Assert.Single(
            DataCenterPhysicalPathReasoning.GetTraversablePhysicalEdges(
                graph));

        Assert.Single(
            DataCenterPhysicalPathReasoning.GetIncompletePhysicalEdges(
                graph));
    }

    [Fact]
    public void FindPath_SameIdentityRequiresObservedPhysicalEvidence()
    {
        DataCenterHardwareTopologyGraph graph =
            Graph(
                PhysicalEdge(
                    "server:a",
                    "switch:b",
                    550));

        DataCenterPhysicalPathResult unknown =
            DataCenterPhysicalPathReasoning.FindPath(
                graph,
                "persistent:unknown",
                "persistent:unknown");

        Assert.False(
            unknown.Found);

        Assert.Empty(
            unknown.Steps);

        string observedIdentity =
            PersistentReference(
                "server:a",
                "server").IdentityKey;

        DataCenterPhysicalPathResult observed =
            DataCenterPhysicalPathReasoning.FindPath(
                graph,
                observedIdentity,
                observedIdentity);

        Assert.True(
            observed.Found);

        Assert.Equal(
            0,
            observed.HopCount);

        Assert.Empty(
            observed.Steps);
    }

    [Fact]
    public void Reasoning_IsReadOnlyOverCapturedGraph()
    {
        DataCenterHardwareTopologyGraph graph =
            Graph(
                PhysicalEdge(
                    "server:a",
                    "switch:b",
                    600));

        int nodeCount =
            graph.Nodes.Count;

        int edgeCount =
            graph.Edges.Count;

        _ =
            DataCenterPhysicalPathReasoning.FindPath(
                graph,
                PersistentReference(
                    "server:a",
                    "server").IdentityKey,
                PersistentReference(
                    "switch:b",
                    "switch").IdentityKey);

        Assert.Equal(
            nodeCount,
            graph.Nodes.Count);

        Assert.Equal(
            edgeCount,
            graph.Edges.Count);
    }

    private static DataCenterHardwareTopologyGraph Graph(
        params DataCenterHardwareTopologyEdge[] edges)
    {
        return
            new DataCenterHardwareTopologyGraph(
                Array.Empty<DataCenterHardwareTopologyNode>(),
                edges);
    }

    private static DataCenterHardwareTopologyEdge PhysicalEdge(
        string sourcePersistentId,
        string targetPersistentId,
        int cableId,
        bool sourceResolved = true,
        bool targetResolved = true,
        string? evidenceSource = null)
    {
        string sourceKind =
            sourcePersistentId.Split(':')[0];

        string targetKind =
            targetPersistentId.Split(':')[0];

        return
            new DataCenterHardwareTopologyEdge(
                relationship:
                    DataCenterHardwareTopologyRelationships.PhysicalCableConnection,
                source:
                    PersistentReference(
                        sourcePersistentId,
                        sourceKind),
                target:
                    PersistentReference(
                        targetPersistentId,
                        targetKind),
                targetResolved:
                    targetResolved,
                kind:
                    DataCenterHardwareTopologyEdgeKind.NetworkConnection,
                physicalCableId:
                    cableId,
                isBidirectional:
                    true,
                evidenceSource:
                    evidenceSource ??
                    DataCenterPhysicalCableTopology.EvidenceSource,
                sourceResolved:
                    sourceResolved);
    }

    private static DataCenterHardwareReference PersistentReference(
        string persistentId,
        string kind)
    {
        return
            new DataCenterHardwareReference(
                instanceId:
                    0,
                name:
                    persistentId,
                typeName:
                    "DataCenter.Persistence." +
                    kind,
                persistentId:
                    persistentId,
                identityKind:
                    "save-" +
                    kind);
    }
}
