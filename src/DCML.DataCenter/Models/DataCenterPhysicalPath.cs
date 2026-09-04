using System;
using System.Collections.Generic;
using System.Linq;

namespace DCML.DataCenter.Models;

/// <summary>
/// One traversal step in a proven physical path.
/// </summary>
public sealed class DataCenterPhysicalPathStep
{
    public DataCenterPhysicalPathStep(
        string fromIdentityKey,
        string toIdentityKey,
        DataCenterHardwareTopologyEdge evidence)
    {
        if (string.IsNullOrWhiteSpace(fromIdentityKey))
        {
            throw new ArgumentException(
                "A source identity key is required.",
                nameof(fromIdentityKey));
        }

        if (string.IsNullOrWhiteSpace(toIdentityKey))
        {
            throw new ArgumentException(
                "A target identity key is required.",
                nameof(toIdentityKey));
        }

        FromIdentityKey =
            fromIdentityKey.Trim();

        ToIdentityKey =
            toIdentityKey.Trim();

        Evidence =
            evidence ??
            throw new ArgumentNullException(
                nameof(evidence));
    }

    public string FromIdentityKey { get; }

    public string ToIdentityKey { get; }

    public DataCenterHardwareTopologyEdge Evidence { get; }

    public int? PhysicalCableID =>
        Evidence.PhysicalCableID;

    public string EvidenceSource =>
        Evidence.EvidenceSource;
}

/// <summary>
/// Result of evidence-backed physical-path reasoning.
/// </summary>
public sealed class DataCenterPhysicalPathResult
{
    private readonly IReadOnlyList<DataCenterPhysicalPathStep>
        _steps;

    public DataCenterPhysicalPathResult(
        string sourceIdentityKey,
        string targetIdentityKey,
        bool found,
        IEnumerable<DataCenterPhysicalPathStep>? steps)
    {
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

        SourceIdentityKey =
            sourceIdentityKey.Trim();

        TargetIdentityKey =
            targetIdentityKey.Trim();

        Found =
            found;

        _steps =
            steps?.ToArray() ??
            Array.Empty<DataCenterPhysicalPathStep>();
    }

    public string SourceIdentityKey { get; }

    public string TargetIdentityKey { get; }

    /// <summary>
    /// Gets whether a complete evidence-backed path was found.
    /// </summary>
    public bool Found { get; }

    public IReadOnlyList<DataCenterPhysicalPathStep> Steps =>
        _steps;

    public int HopCount =>
        _steps.Count;

    public IReadOnlyList<string> EvidenceSources =>
        _steps
            .Select(
                step =>
                    step.EvidenceSource)
            .Where(
                source =>
                    !string.IsNullOrWhiteSpace(
                        source))
            .Distinct(
                StringComparer.Ordinal)
            .ToArray();
}
