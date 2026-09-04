using System;
using System.Collections.Generic;
using System.Linq;
using DCML.DataCenter.Models;

namespace DCML.DataCenter;

/// <summary>
/// Performs read-only physical-path reasoning over evidence already present in
/// a captured Data Center topology graph.
/// </summary>
/// <remarks>
/// The reasoner never discovers saves, reads game objects, mutates the graph,
/// or infers connectivity from names, ordering, proximity, or scene membership.
/// </remarks>
public static class DataCenterPhysicalPathReasoning
{
    /// <summary>
    /// Determines whether an edge is strong enough to use as a physical path
    /// traversal step.
    /// </summary>
    public static bool CanTraversePhysicalEdge(
        DataCenterHardwareTopologyEdge? edge)
    {
        if (edge is null)
        {
            return false;
        }

        return
            edge.Kind ==
                DataCenterHardwareTopologyEdgeKind.NetworkConnection &&
            string.Equals(
                edge.Relationship,
                DataCenterHardwareTopologyRelationships.PhysicalCableConnection,
                StringComparison.OrdinalIgnoreCase) &&
            edge.PhysicalCableID.HasValue &&
            edge.IsBidirectional &&
            edge.IsFullyResolved &&
            edge.Source.HasPersistentIdentity &&
            edge.Target.HasPersistentIdentity &&
            !string.IsNullOrWhiteSpace(
                edge.EvidenceSource);
    }

    /// <summary>
    /// Returns only persisted physical edges that satisfy every traversal
    /// requirement.
    /// </summary>
    public static IReadOnlyList<DataCenterHardwareTopologyEdge>
        GetTraversablePhysicalEdges(
            DataCenterHardwareTopologyGraph graph)
    {
        if (graph is null)
        {
            throw new ArgumentNullException(
                nameof(graph));
        }

        return
            graph.PhysicalCableEdges
                .Where(
                    CanTraversePhysicalEdge)
                .ToArray();
    }

    /// <summary>
    /// Returns physical cable evidence that is present but not strong enough to
    /// traverse.
    /// </summary>
    public static IReadOnlyList<DataCenterHardwareTopologyEdge>
        GetIncompletePhysicalEdges(
            DataCenterHardwareTopologyGraph graph)
    {
        if (graph is null)
        {
            throw new ArgumentNullException(
                nameof(graph));
        }

        return
            graph.PhysicalCableEdges
                .Where(
                    edge =>
                        !CanTraversePhysicalEdge(
                            edge))
                .ToArray();
    }

    /// <summary>
    /// Returns live SFP insertion observations as structural context.
    /// </summary>
    /// <remarks>
    /// These edges are intentionally not considered physical cable traversal
    /// steps.
    /// </remarks>
    public static IReadOnlyList<DataCenterHardwareTopologyEdge>
        GetLiveStructuralEvidence(
            DataCenterHardwareTopologyGraph graph)
    {
        if (graph is null)
        {
            throw new ArgumentNullException(
                nameof(graph));
        }

        return
            graph.StructuralEdges
                .Where(
                    edge =>
                        string.Equals(
                            edge.Relationship,
                            DataCenterHardwareTopologyRelationships.SfpModuleInsertion,
                            StringComparison.OrdinalIgnoreCase))
                .ToArray();
    }

    /// <summary>
    /// Finds the shortest proven physical path between two persistent topology
    /// identity keys.
    /// </summary>
    /// <remarks>
    /// Only <see cref="CanTraversePhysicalEdge"/> edges participate. When no
    /// complete path exists, the result reports Found=false and contains no
    /// guessed steps.
    /// </remarks>
    public static DataCenterPhysicalPathResult FindPath(
        DataCenterHardwareTopologyGraph graph,
        string sourceIdentityKey,
        string targetIdentityKey)
    {
        if (graph is null)
        {
            throw new ArgumentNullException(
                nameof(graph));
        }

        if (string.IsNullOrWhiteSpace(sourceIdentityKey))
        {
            throw new ArgumentException(
                "A source identity key is required.",
                nameof(sourceIdentityKey));
        }

        if (string.IsNullOrWhiteSpace(targetIdentityKey))
        {
            throw new ArgumentException(
                "A target identity key is required.",
                nameof(targetIdentityKey));
        }

        string source =
            sourceIdentityKey.Trim();

        string target =
            targetIdentityKey.Trim();

        IReadOnlyList<DataCenterHardwareTopologyEdge> edges =
            GetTraversablePhysicalEdges(
                graph);

        if (
            string.Equals(
                source,
                target,
                StringComparison.Ordinal)
        )
        {
            bool identityObserved =
                edges.Any(
                    edge =>
                        string.Equals(
                            edge.Source.IdentityKey,
                            source,
                            StringComparison.Ordinal) ||
                        string.Equals(
                            edge.Target.IdentityKey,
                            source,
                            StringComparison.Ordinal));

            return
                new DataCenterPhysicalPathResult(
                    source,
                    target,
                    found:
                        identityObserved,
                    Array.Empty<DataCenterPhysicalPathStep>());
        }

        var adjacency =
            new Dictionary<
                string,
                List<TraversalCandidate>>(
                    StringComparer.Ordinal);

        foreach (
            DataCenterHardwareTopologyEdge edge in
            edges)
        {
            AddCandidate(
                adjacency,
                edge.Source.IdentityKey,
                edge.Target.IdentityKey,
                edge);

            AddCandidate(
                adjacency,
                edge.Target.IdentityKey,
                edge.Source.IdentityKey,
                edge);
        }

        var queue =
            new Queue<string>();

        var visited =
            new HashSet<string>(
                StringComparer.Ordinal);

        var previous =
            new Dictionary<
                string,
                PreviousStep>(
                    StringComparer.Ordinal);

        queue.Enqueue(
            source);

        visited.Add(
            source);

        while (queue.Count > 0)
        {
            string current =
                queue.Dequeue();

            if (
                !adjacency.TryGetValue(
                    current,
                    out List<TraversalCandidate>? candidates)
            )
            {
                continue;
            }

            foreach (
                TraversalCandidate candidate in
                candidates)
            {
                if (
                    !visited.Add(
                        candidate.ToIdentityKey)
                )
                {
                    continue;
                }

                previous[candidate.ToIdentityKey] =
                    new PreviousStep(
                        current,
                        candidate.Edge);

                if (
                    string.Equals(
                        candidate.ToIdentityKey,
                        target,
                        StringComparison.Ordinal)
                )
                {
                    return
                        CreateFoundResult(
                            source,
                            target,
                            previous);
                }

                queue.Enqueue(
                    candidate.ToIdentityKey);
            }
        }

        return
            new DataCenterPhysicalPathResult(
                source,
                target,
                found:
                    false,
                Array.Empty<DataCenterPhysicalPathStep>());
    }

    private static DataCenterPhysicalPathResult CreateFoundResult(
        string source,
        string target,
        IReadOnlyDictionary<string, PreviousStep> previous)
    {
        var reversed =
            new List<DataCenterPhysicalPathStep>();

        string current =
            target;

        while (
            !string.Equals(
                current,
                source,
                StringComparison.Ordinal)
        )
        {
            PreviousStep step =
                previous[current];

            reversed.Add(
                new DataCenterPhysicalPathStep(
                    step.PreviousIdentityKey,
                    current,
                    step.Edge));

            current =
                step.PreviousIdentityKey;
        }

        reversed.Reverse();

        return
            new DataCenterPhysicalPathResult(
                source,
                target,
                found:
                    true,
                reversed);
    }

    private static void AddCandidate(
        IDictionary<string, List<TraversalCandidate>> adjacency,
        string fromIdentityKey,
        string toIdentityKey,
        DataCenterHardwareTopologyEdge edge)
    {
        if (
            !adjacency.TryGetValue(
                fromIdentityKey,
                out List<TraversalCandidate>? candidates)
        )
        {
            candidates =
                new List<TraversalCandidate>();

            adjacency[fromIdentityKey] =
                candidates;
        }

        candidates.Add(
            new TraversalCandidate(
                toIdentityKey,
                edge));
    }

    private sealed class TraversalCandidate
    {
        public TraversalCandidate(
            string toIdentityKey,
            DataCenterHardwareTopologyEdge edge)
        {
            ToIdentityKey =
                toIdentityKey;

            Edge =
                edge;
        }

        public string ToIdentityKey { get; }

        public DataCenterHardwareTopologyEdge Edge { get; }
    }

    private sealed class PreviousStep
    {
        public PreviousStep(
            string previousIdentityKey,
            DataCenterHardwareTopologyEdge edge)
        {
            PreviousIdentityKey =
                previousIdentityKey;

            Edge =
                edge;
        }

        public string PreviousIdentityKey { get; }

        public DataCenterHardwareTopologyEdge Edge { get; }
    }
}
