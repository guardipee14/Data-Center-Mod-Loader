using System;

namespace DCML.Core.Models;

/// <summary>
/// Identifies one package entry discovered from a package source.
/// PackageKey is opaque to DCML Core and is interpreted only by the source
/// that produced it.
/// </summary>
public sealed class DCMLPackageSourceEntry
{
    public DCMLPackageSourceEntry(string sourceId, string packageKey)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            throw new ArgumentException(
                "Package source ID cannot be empty.",
                nameof(sourceId));
        }

        if (string.IsNullOrWhiteSpace(packageKey))
        {
            throw new ArgumentException(
                "Package key cannot be empty.",
                nameof(packageKey));
        }

        SourceId = sourceId.Trim();
        PackageKey = packageKey.Trim();
    }

    public string SourceId { get; }

    public string PackageKey { get; }
}
