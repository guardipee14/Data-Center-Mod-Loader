using System;
using System.Collections.Generic;
using System.Linq;
using DCML.DataCenter.Models;

namespace DCML.DataCenter;

public static class DataCenterPhysicalCableTopology
{
    public const string EvidenceSource =
        "Data Center save: NetworkSaveData.cables";

    public static DataCenterHardwareTopologyGraph Build(
        IEnumerable<DataCenterCablePersistenceRecord> cables,
        DataCenterCablePersistenceIndex index)
    {
        if (cables is null)
        {
            throw new ArgumentNullException(
                nameof(cables));
        }

        if (index is null)
        {
            throw new ArgumentNullException(
                nameof(index));
        }

        DataCenterPhysicalCableConnection[] connections =
            cables
                .Select(
                    index.Resolve)
                .OrderBy(
                    value =>
                        value.CableID)
                .ToArray();

        var nodes =
            new Dictionary<string, DataCenterHardwareTopologyNode>(
                StringComparer.Ordinal);

        var edges =
            new List<DataCenterHardwareTopologyEdge>();

        foreach (
            DataCenterPhysicalCableConnection connection in
            connections)
        {
            DataCenterHardwareReference source =
                ToReference(
                    connection.CableID,
                    connection.Start);

            DataCenterHardwareReference target =
                ToReference(
                    connection.CableID,
                    connection.End);

            AddNode(
                nodes,
                source,
                connection.Start);

            AddNode(
                nodes,
                target,
                connection.End);

            edges.Add(
                new DataCenterHardwareTopologyEdge(
                    relationship:
                        DataCenterHardwareTopologyRelationships.PhysicalCableConnection,
                    source:
                        source,
                    target:
                        target,
                    targetResolved:
                        connection.End.IsResolved,
                    resolvedTargetName:
                        connection.End.IsResolved
                            ? connection.End.PersistentID
                            : string.Empty,
                    targetLocation:
                        DataCenterHardwareTopologyTargetLocation.Unknown,
                    targetCable:
                        null,
                    kind:
                        DataCenterHardwareTopologyEdgeKind.NetworkConnection,
                    physicalCableId:
                        connection.CableID,
                    isBidirectional:
                        true,
                    evidenceSource:
                        EvidenceSource,
                    sourceResolved:
                        connection.Start.IsResolved));
        }

        return
            new DataCenterHardwareTopologyGraph(
                nodes.Values
                    .OrderBy(
                        value =>
                            value.Kind,
                        StringComparer.Ordinal)
                    .ThenBy(
                        value =>
                            value.PersistentID,
                        StringComparer.Ordinal)
                    .ThenBy(
                        value =>
                            value.InstanceId),
                edges
                    .OrderBy(
                        value =>
                            value.PhysicalCableID ??
                            int.MaxValue));
    }

    public static DataCenterHardwareTopologyGraph Combine(
        DataCenterHardwareTopologyGraph liveGraph,
        IEnumerable<DataCenterCablePersistenceRecord> cables,
        DataCenterCablePersistenceIndex index)
    {
        if (liveGraph is null)
        {
            throw new ArgumentNullException(
                nameof(liveGraph));
        }

        DataCenterHardwareTopologyGraph physical =
            Build(
                cables,
                index);

        DataCenterHardwareTopologyNode[] nodes =
            liveGraph.Nodes
                .Concat(
                    physical.Nodes)
                .GroupBy(
                    value =>
                        value.IdentityKey,
                    StringComparer.Ordinal)
                .Select(
                    group =>
                        group.First())
                .OrderBy(
                    value =>
                        value.Kind,
                    StringComparer.Ordinal)
                .ThenBy(
                    value =>
                        value.IdentityKey,
                    StringComparer.Ordinal)
                .ToArray();

        DataCenterHardwareTopologyEdge[] edges =
            liveGraph.Edges
                .Concat(
                    physical.Edges)
                .OrderBy(
                    value =>
                        value.Kind)
                .ThenBy(
                    value =>
                        value.Relationship,
                    StringComparer.Ordinal)
                .ThenBy(
                    value =>
                        value.PhysicalCableID ??
                        int.MaxValue)
                .ThenBy(
                    value =>
                        value.Source.IdentityKey,
                    StringComparer.Ordinal)
                .ThenBy(
                    value =>
                        value.Target.IdentityKey,
                    StringComparer.Ordinal)
                .ToArray();

        return
            new DataCenterHardwareTopologyGraph(
                nodes,
                edges,
                liveGraph.CableSearchPages,
                liveGraph.CableCandidatesScanned,
                liveGraph.CableSearchExhausted,
                liveGraph.NonSceneCableSearchPages,
                liveGraph.NonSceneCableCandidatesScanned,
                liveGraph.NonSceneTargetMatchCount,
                liveGraph.NonSceneCableSearchExhausted,
                liveGraph.TargetedCableDetailRequestedCount,
                liveGraph.TargetedCableDetailFoundCount);
    }

    private static void AddNode(
        IDictionary<string, DataCenterHardwareTopologyNode> nodes,
        DataCenterHardwareReference reference,
        DataCenterPhysicalCableEndpointResolution resolution)
    {
        if (
            nodes.ContainsKey(
                reference.IdentityKey)
        )
        {
            return;
        }

        nodes[reference.IdentityKey] =
            new DataCenterHardwareTopologyNode(
                instanceId:
                    0,
                name:
                    reference.Name,
                typeName:
                    reference.TypeName,
                kind:
                    ToNodeKind(
                        resolution.Kind),
                persistentId:
                    reference.PersistentID,
                identityKind:
                    reference.IdentityKind);
    }

    private static DataCenterHardwareReference ToReference(
        int cableId,
        DataCenterPhysicalCableEndpointResolution endpoint)
    {
        string persistentId =
            endpoint.IsResolved
                ? endpoint.PersistentID
                : "cable:" +
                    cableId +
                    ":" +
                    endpoint.Raw.Side
                        .ToString()
                        .ToLowerInvariant();

        string identityKind =
            endpoint.IsResolved
                ? "save-" +
                    endpoint.Kind
                        .ToString()
                        .ToLowerInvariant()
                : "save-endpoint";

        string typeName =
            endpoint.IsResolved
                ? "DataCenter.Persistence." +
                    endpoint.Kind
                : "DataCenter.Persistence.UnknownEndpoint";

        return
            new DataCenterHardwareReference(
                instanceId:
                    0,
                name:
                    persistentId,
                typeName:
                    typeName,
                persistentId:
                    persistentId,
                identityKind:
                    identityKind);
    }

    private static string ToNodeKind(
        DataCenterPhysicalCableEndpointKind kind)
    {
        return
            kind switch
            {
                DataCenterPhysicalCableEndpointKind.Server =>
                    "server",
                DataCenterPhysicalCableEndpointKind.Switch =>
                    "switch",
                DataCenterPhysicalCableEndpointKind.Router =>
                    "router",
                DataCenterPhysicalCableEndpointKind.Firewall =>
                    "firewall",
                DataCenterPhysicalCableEndpointKind.PatchPanel =>
                    "patch-panel",
                DataCenterPhysicalCableEndpointKind.PatchPanelPort =>
                    "patch-panel-port",
                DataCenterPhysicalCableEndpointKind.CustomerBase =>
                    "customer-base",
                _ =>
                    "physical-cable-endpoint"
            };
    }
}
