using System;
using System.Collections.Generic;
using System.Linq;

namespace DCML.DataCenter.Models;

public enum DataCenterHardwareTopologyTargetLocation
{
    Unknown = 0,
    SceneObject = 1,
    NonSceneObject = 2
}

public sealed class DataCenterHardwareTopologyNode
{
    public DataCenterHardwareTopologyNode(
        int instanceId,
        string? name,
        string? typeName,
        string? kind)
    {
        InstanceId = instanceId;
        Name = name ?? string.Empty;
        TypeName =
            string.IsNullOrWhiteSpace(typeName)
                ? string.Empty
                : typeName.Trim();
        Kind =
            string.IsNullOrWhiteSpace(kind)
                ? string.Empty
                : kind.Trim();
    }

    public int InstanceId { get; }

    public string Name { get; }

    public string TypeName { get; }

    public string Kind { get; }
}

public sealed class DataCenterHardwareTopologyEdge
{
    public DataCenterHardwareTopologyEdge(
        string relationship,
        DataCenterHardwareReference source,
        DataCenterHardwareReference target,
        bool targetResolved,
        string? resolvedTargetName = null,
        DataCenterHardwareTopologyTargetLocation targetLocation =
            DataCenterHardwareTopologyTargetLocation.Unknown,
        DataCenterCableSnapshot? targetCable = null)
    {
        if (string.IsNullOrWhiteSpace(relationship))
        {
            throw new ArgumentException(
                "A topology relationship is required.",
                nameof(relationship));
        }

        Relationship = relationship.Trim();

        Source =
            source ??
            throw new ArgumentNullException(
                nameof(source));

        Target =
            target ??
            throw new ArgumentNullException(
                nameof(target));

        TargetResolved = targetResolved;
        ResolvedTargetName = resolvedTargetName ?? string.Empty;
        TargetLocation = targetLocation;
        TargetCable = targetCable;
    }

    public string Relationship { get; }

    public DataCenterHardwareReference Source { get; }

    public DataCenterHardwareReference Target { get; }

    public bool TargetResolved { get; }

    public string ResolvedTargetName { get; }

    public DataCenterHardwareTopologyTargetLocation TargetLocation { get; }

    public bool TargetObserved =>
        TargetLocation !=
            DataCenterHardwareTopologyTargetLocation.Unknown;

    public DataCenterCableSnapshot? TargetCable { get; }
}

public sealed class DataCenterHardwareTopologyGraph
{
    private readonly IReadOnlyList<DataCenterHardwareTopologyNode> _nodes;
    private readonly IReadOnlyList<DataCenterHardwareTopologyEdge> _edges;

    public DataCenterHardwareTopologyGraph(
        IEnumerable<DataCenterHardwareTopologyNode>? nodes,
        IEnumerable<DataCenterHardwareTopologyEdge>? edges,
        int cableSearchPages = 0,
        int cableCandidatesScanned = 0,
        bool cableSearchExhausted = false,
        int nonSceneCableSearchPages = 0,
        int nonSceneCableCandidatesScanned = 0,
        int nonSceneTargetMatchCount = 0,
        bool nonSceneCableSearchExhausted = false,
        int targetedCableDetailRequestedCount = 0,
        int targetedCableDetailFoundCount = 0)
    {
        if (cableSearchPages < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cableSearchPages));
        }

        if (cableCandidatesScanned < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cableCandidatesScanned));
        }

        if (nonSceneCableSearchPages < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nonSceneCableSearchPages));
        }

        if (nonSceneCableCandidatesScanned < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nonSceneCableCandidatesScanned));
        }

        if (nonSceneTargetMatchCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nonSceneTargetMatchCount));
        }

        if (targetedCableDetailRequestedCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetedCableDetailRequestedCount));
        }

        if (targetedCableDetailFoundCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetedCableDetailFoundCount));
        }

        _nodes =
            nodes?.ToArray() ??
            Array.Empty<DataCenterHardwareTopologyNode>();

        _edges =
            edges?.ToArray() ??
            Array.Empty<DataCenterHardwareTopologyEdge>();

        CableSearchPages = cableSearchPages;
        CableCandidatesScanned = cableCandidatesScanned;
        CableSearchExhausted = cableSearchExhausted;
        NonSceneCableSearchPages = nonSceneCableSearchPages;
        NonSceneCableCandidatesScanned = nonSceneCableCandidatesScanned;
        NonSceneTargetMatchCount = nonSceneTargetMatchCount;
        NonSceneCableSearchExhausted = nonSceneCableSearchExhausted;
        TargetedCableDetailRequestedCount =
            targetedCableDetailRequestedCount;
        TargetedCableDetailFoundCount =
            targetedCableDetailFoundCount;
    }

    public IReadOnlyList<DataCenterHardwareTopologyNode> Nodes =>
        _nodes;

    public IReadOnlyList<DataCenterHardwareTopologyEdge> Edges =>
        _edges;

    public IReadOnlyList<DataCenterHardwareTopologyEdge> ResolvedEdges =>
        _edges
            .Where(value => value.TargetResolved)
            .ToArray();

    public IReadOnlyList<DataCenterHardwareTopologyEdge> UnresolvedEdges =>
        _edges
            .Where(value => !value.TargetResolved)
            .ToArray();

    public int CableSearchPages { get; }

    public int CableCandidatesScanned { get; }

    public bool CableSearchExhausted { get; }

    public int NonSceneCableSearchPages { get; }

    public int NonSceneCableCandidatesScanned { get; }

    public int NonSceneTargetMatchCount { get; }

    public bool NonSceneCableSearchExhausted { get; }

    public int TargetedCableDetailRequestedCount { get; }

    public int TargetedCableDetailFoundCount { get; }

    public IReadOnlyList<DataCenterHardwareTopologyEdge> ObservedNonSceneEdges =>
        _edges
            .Where(
                value =>
                    value.TargetLocation ==
                        DataCenterHardwareTopologyTargetLocation.NonSceneObject)
            .ToArray();
}
