using System;
using System.Collections.Generic;

namespace DCML.Core.Models;

/// <summary>
/// Contains read-only package entries and issues returned by one package
/// source discovery operation.
/// </summary>
public sealed class DCMLPackageSourceDiscoveryResult
{
    private readonly List<DCMLPackageSourceEntry> _entries;
    private readonly List<DCMLPackageSourceIssue> _issues;

    public DCMLPackageSourceDiscoveryResult(
        string sourceId,
        IEnumerable<DCMLPackageSourceEntry> entries,
        IEnumerable<DCMLPackageSourceIssue>? issues = null)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            throw new ArgumentException(
                "Package source ID cannot be empty.",
                nameof(sourceId));
        }

        if (entries == null)
        {
            throw new ArgumentNullException(nameof(entries));
        }

        SourceId = sourceId.Trim();
        _entries = new List<DCMLPackageSourceEntry>(entries);
        _issues = issues == null
            ? new List<DCMLPackageSourceIssue>()
            : new List<DCMLPackageSourceIssue>(issues);

        ValidateSourceIds();
    }

    public string SourceId { get; }

    public IReadOnlyList<DCMLPackageSourceEntry> Entries => _entries;

    public IReadOnlyList<DCMLPackageSourceIssue> Issues => _issues;

    public bool Success => _issues.Count == 0;

    private void ValidateSourceIds()
    {
        foreach (DCMLPackageSourceEntry entry in _entries)
        {
            if (!string.Equals(
                SourceId,
                entry.SourceId,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Every package entry must belong to the result source ID.",
                    nameof(_entries));
            }
        }

        foreach (DCMLPackageSourceIssue issue in _issues)
        {
            if (!string.Equals(
                SourceId,
                issue.SourceId,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Every package source issue must belong to the result source ID.",
                    nameof(_issues));
            }
        }
    }
}
