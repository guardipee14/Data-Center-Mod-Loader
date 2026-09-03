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

public enum DataCenterHardwareTopologyEdgeKind
{
    Unknown = 0,
    Structural = 1,
    NetworkConnection = 2
}

public static class DataCenterHardwareTopologyRelationships
{
    public const string SfpModuleInsertion =
        "sfp-module-insertion";

    public const string PhysicalCableConnection =
        "physical-cable-connection";
}

public sealed class DataCenterHardwareTopologyNode
{
    public DataCenterHardwareTopologyNode(
        int instanceId,
        string? name,
        string? typeName,
        string? kind,
        string? persistentId = null,
        string? identityKind = null)
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
        PersistentID =
            string.IsNullOrWhiteSpace(persistentId)
                ? string.Empty
                : persistentId.Trim();
        IdentityKind =
            string.IsNullOrWhiteSpace(identityKind)
                ? string.Empty
                : identityKind.Trim();
    }

    public int InstanceId { get; }

    public string Name { get; }

    public string TypeName { get; }

    public string Kind { get; }

    public string PersistentID { get; }

    public string IdentityKind { get; }

    public bool HasRuntimeInstance =>
        InstanceId != 0;

    public bool HasPersistentIdentity =>
        PersistentID.Length > 0;

    public string IdentityKey =>
        HasPersistentIdentity
            ? "persistent:" +
                (
                    IdentityKind.Length == 0
                        ? "unknown"
                        : IdentityKind
                ) +
                ":" +
                PersistentID
            : "runtime:" +
                InstanceId;
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
        DataCenterCableSnapshot? targetCable = null,
        DataCenterHardwareTopologyEdgeKind kind =
            DataCenterHardwareTopologyEdgeKind.Unknown,
        int? physicalCableId = null,
        bool isBidirectional = false,
        string? evidenceSource = null,
        bool sourceResolved = true)
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
        Kind = kind;

        if (
            physicalCableId.HasValue &&
            physicalCableId.Value < 0
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(physicalCableId));
        }

        PhysicalCableID = physicalCableId;
        IsBidirectional = isBidirectional;
        EvidenceSource =
            string.IsNullOrWhiteSpace(
                evidenceSource)
                ? string.Empty
                : evidenceSource.Trim();
        SourceResolved = sourceResolved;
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

    public DataCenterHardwareTopologyEdgeKind Kind { get; }

    public int? PhysicalCableID { get; }

    public bool IsBidirectional { get; }

    public string EvidenceSource { get; }

    public bool SourceResolved { get; }

    public bool IsFullyResolved =>
        SourceResolved &&
        TargetResolved;

    public bool IsNetworkConnection =>
        Kind ==
            DataCenterHardwareTopologyEdgeKind.NetworkConnection;
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

    public IReadOnlyList<DataCenterHardwareTopologyEdge> StructuralEdges =>
        _edges
            .Where(
                value =>
                    value.Kind ==
                        DataCenterHardwareTopologyEdgeKind.Structural)
            .ToArray();

    public IReadOnlyList<DataCenterHardwareTopologyEdge> NetworkConnectionEdges =>
        _edges
            .Where(
                value =>
                    value.Kind ==
                        DataCenterHardwareTopologyEdgeKind.NetworkConnection)
            .ToArray();

    public IReadOnlyList<DataCenterHardwareTopologyEdge> PhysicalCableEdges =>
        _edges
            .Where(
                value =>
                    value.Kind ==
                        DataCenterHardwareTopologyEdgeKind.NetworkConnection &&
                    string.Equals(
                        value.Relationship,
                        DataCenterHardwareTopologyRelationships.PhysicalCableConnection,
                        StringComparison.OrdinalIgnoreCase))
            .ToArray();

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
