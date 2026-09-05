using System;
using DCML.Core.Runtime;

namespace DCML.Core.Models;

/// <summary>
/// Describes one dependency requirement associated with source-provided
/// package/update metadata.
/// </summary>
public sealed class DCMLPackageUpdateDependency
{
    public DCMLPackageUpdateDependency(
        string id,
        string? minimumVersion = null,
        bool optional = false)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException(
                "Dependency ID cannot be empty.",
                nameof(id));
        }

        string? normalizedMinimumVersion =
            string.IsNullOrWhiteSpace(minimumVersion)
                ? null
                : minimumVersion.Trim();

        if (
            normalizedMinimumVersion != null &&
            !DCMLSemanticVersion.IsValid(
                normalizedMinimumVersion))
        {
            throw new ArgumentException(
                "Dependency minimum version must be a valid Semantic Versioning 2.0.0 value.",
                nameof(minimumVersion));
        }

        Id =
            id.Trim();

        MinimumVersion =
            normalizedMinimumVersion;

        Optional =
            optional;
    }

    public string Id { get; }

    public string? MinimumVersion { get; }

    public bool Optional { get; }
}
