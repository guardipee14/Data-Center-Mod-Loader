using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DCML.Core.Abstractions;
using DCML.DataCenter;
using DCML.DataCenter.Abstractions;
using DCML.DataCenter.Models;
using DCML.DataCenter.Persistence;

namespace DCML.Examples.DataCenter.TopologyCapture;

/// <summary>
/// Copy/paste-oriented examples for capturing Data Center hardware topology
/// through the public DCML API.
/// </summary>
public static class TopologyCaptureExamples
{
    /// <summary>
    /// Captures live topology only.
    /// </summary>
    /// <remarks>
    /// Returns null when the current host does not expose the optional
    /// component-state capability required by DataCenterApi.Topology.
    /// </remarks>
    public static async Task<DataCenterHardwareTopologyGraph?> CaptureLiveAsync(
        IDCMLModuleContext context,
        DataCenterHardwareSnapshotQuery query)
    {
        if (context is null)
        {
            throw new ArgumentNullException(
                nameof(context));
        }

        if (query is null)
        {
            throw new ArgumentNullException(
                nameof(query));
        }

        DataCenterApi api =
            DataCenterApi.Create(
                context);

        if (api.Topology is null)
        {
            return null;
        }

        return await api.Topology
            .CaptureAsync(
                query)
            .ConfigureAwait(
                false);
    }

    /// <summary>
    /// Captures live topology and, when explicitly enabled and completely
    /// configured, combines the selected save's physical cable evidence.
    /// </summary>
    /// <remarks>
    /// Disabled or incomplete persistence settings remain live-only because
    /// the factory returns null. No save is discovered or selected here.
    /// </remarks>
    public static async Task<DataCenterHardwareTopologyGraph?>
        CaptureWithOptionalPersistenceAsync(
            IDCMLModuleContext context,
            DataCenterHardwareSnapshotQuery query,
            DataCenterProcessCablePersistenceSettings? persistenceSettings)
    {
        if (context is null)
        {
            throw new ArgumentNullException(
                nameof(context));
        }

        if (query is null)
        {
            throw new ArgumentNullException(
                nameof(query));
        }

        IDataCenterCablePersistenceSource? persistence =
            DataCenterProcessCablePersistenceSourceFactory.Create(
                persistenceSettings);

        DataCenterApi api =
            DataCenterApi.Create(
                context,
                persistence);

        if (api.Topology is null)
        {
            return null;
        }

        return await api.Topology
            .CaptureAsync(
                query)
            .ConfigureAwait(
                false);
    }

    /// <summary>
    /// Captures a caller-supplied query only when it explicitly targets scene
    /// objects and names the scene to capture.
    /// </summary>
    /// <remarks>
    /// DCML does not guess the active scene here. The caller owns scene
    /// selection and supplies the query.
    /// </remarks>
    public static Task<DataCenterHardwareTopologyGraph?> CaptureExplicitSceneAsync(
        IDCMLModuleContext context,
        DataCenterHardwareSnapshotQuery sceneQuery)
    {
        if (sceneQuery is null)
        {
            throw new ArgumentNullException(
                nameof(sceneQuery));
        }

        if (!sceneQuery.IncludeSceneObjects)
        {
            throw new ArgumentException(
                "The topology query must explicitly include scene objects.",
                nameof(sceneQuery));
        }

        if (string.IsNullOrWhiteSpace(
            sceneQuery.SceneName))
        {
            throw new ArgumentException(
                "The topology query must explicitly name the target scene.",
                nameof(sceneQuery));
        }

        return CaptureLiveAsync(
            context,
            sceneQuery);
    }

    /// <summary>
    /// Returns every network-connection edge observed by the graph.
    /// </summary>
    public static IReadOnlyList<DataCenterHardwareTopologyEdge>
        GetNetworkConnections(
            DataCenterHardwareTopologyGraph graph)
    {
        if (graph is null)
        {
            throw new ArgumentNullException(
                nameof(graph));
        }

        return graph.NetworkConnectionEdges;
    }

    /// <summary>
    /// Returns persisted physical-cable edges only.
    /// </summary>
    public static IReadOnlyList<DataCenterHardwareTopologyEdge>
        GetPhysicalCableConnections(
            DataCenterHardwareTopologyGraph graph)
    {
        if (graph is null)
        {
            throw new ArgumentNullException(
                nameof(graph));
        }

        return graph.PhysicalCableEdges;
    }

    /// <summary>
    /// Returns only physical cable edges for which both persisted endpoints
    /// were resolved.
    /// </summary>
    public static IReadOnlyList<DataCenterHardwareTopologyEdge>
        GetFullyResolvedPhysicalCableConnections(
            DataCenterHardwareTopologyGraph graph)
    {
        if (graph is null)
        {
            throw new ArgumentNullException(
                nameof(graph));
        }

        return graph.PhysicalCableEdges
            .Where(
                edge =>
                    edge.IsFullyResolved)
            .ToArray();
    }

    /// <summary>
    /// Returns a compact evidence view without guessing relationships.
    /// </summary>
    public static IReadOnlyList<PhysicalCableEvidence>
        GetPhysicalCableEvidence(
            DataCenterHardwareTopologyGraph graph)
    {
        if (graph is null)
        {
            throw new ArgumentNullException(
                nameof(graph));
        }

        return graph.PhysicalCableEdges
            .Select(
                edge =>
                    new PhysicalCableEvidence(
                        edge.PhysicalCableID,
                        edge.Source.IdentityKey,
                        edge.Target.IdentityKey,
                        edge.SourceResolved,
                        edge.TargetResolved,
                        edge.IsBidirectional,
                        edge.EvidenceSource))
            .ToArray();
    }
}

/// <summary>
/// Small DTO suitable for logs, diagnostics, or higher-level analysis.
/// </summary>
public sealed class PhysicalCableEvidence
{
    public PhysicalCableEvidence(
        int? physicalCableId,
        string sourceIdentity,
        string targetIdentity,
        bool sourceResolved,
        bool targetResolved,
        bool isBidirectional,
        string evidenceSource)
    {
        PhysicalCableID =
            physicalCableId;

        SourceIdentity =
            sourceIdentity ??
            string.Empty;

        TargetIdentity =
            targetIdentity ??
            string.Empty;

        SourceResolved =
            sourceResolved;

        TargetResolved =
            targetResolved;

        IsBidirectional =
            isBidirectional;

        EvidenceSource =
            evidenceSource ??
            string.Empty;
    }

    public int? PhysicalCableID { get; }

    public string SourceIdentity { get; }

    public string TargetIdentity { get; }

    public bool SourceResolved { get; }

    public bool TargetResolved { get; }

    public bool IsFullyResolved =>
        SourceResolved &&
        TargetResolved;

    public bool IsBidirectional { get; }

    public string EvidenceSource { get; }
}
