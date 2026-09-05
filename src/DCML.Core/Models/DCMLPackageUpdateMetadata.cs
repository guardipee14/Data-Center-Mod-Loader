using System;
using System.Collections.Generic;
using DCML.Core.Runtime;

namespace DCML.Core.Models;

/// <summary>
/// Describes source-provided package version metadata. This model is
/// descriptive only and does not authorize staging, installation, updating,
/// downloading, subscription changes, or any other mutation.
/// </summary>
public sealed class DCMLPackageUpdateMetadata
{
    private readonly List<DCMLPackageUpdateDependency> _dependencies;

    public DCMLPackageUpdateMetadata(
        string sourceId,
        string packageKey,
        string moduleId,
        string version,
        string? minimumDCMLVersion = null,
        bool requiresRestart = false,
        IEnumerable<DCMLPackageUpdateDependency>? dependencies = null)
    {
        SourceId =
            RequireValue(
                sourceId,
                nameof(sourceId));

        PackageKey =
            RequireValue(
                packageKey,
                nameof(packageKey));

        ModuleId =
            RequireValue(
                moduleId,
                nameof(moduleId));

        string normalizedVersion =
            RequireValue(
                version,
                nameof(version));

        if (!DCMLSemanticVersion.IsValid(normalizedVersion))
        {
            throw new ArgumentException(
                "Package version must be a valid Semantic Versioning 2.0.0 value.",
                nameof(version));
        }

        string? normalizedMinimumDCMLVersion =
            string.IsNullOrWhiteSpace(minimumDCMLVersion)
                ? null
                : minimumDCMLVersion.Trim();

        if (
            normalizedMinimumDCMLVersion != null &&
            !DCMLSemanticVersion.IsValid(
                normalizedMinimumDCMLVersion))
        {
            throw new ArgumentException(
                "Minimum DCML version must be a valid Semantic Versioning 2.0.0 value.",
                nameof(minimumDCMLVersion));
        }

        Version =
            normalizedVersion;

        MinimumDCMLVersion =
            normalizedMinimumDCMLVersion;

        RequiresRestart =
            requiresRestart;

        _dependencies =
            dependencies == null
                ? new List<DCMLPackageUpdateDependency>()
                : new List<DCMLPackageUpdateDependency>(
                    dependencies);

        ValidateDependencies();
    }

    public string SourceId { get; }

    public string PackageKey { get; }

    public string ModuleId { get; }

    public string Version { get; }

    public string? MinimumDCMLVersion { get; }

    public bool RequiresRestart { get; }

    public IReadOnlyList<DCMLPackageUpdateDependency> Dependencies =>
        _dependencies;

    private void ValidateDependencies()
    {
        var ids =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (
            DCMLPackageUpdateDependency? dependency
            in _dependencies)
        {
            if (dependency == null)
            {
                throw new ArgumentException(
                    "Package update dependencies cannot contain null entries.",
                    nameof(_dependencies));
            }

            if (!ids.Add(dependency.Id))
            {
                throw new ArgumentException(
                    "Package update dependencies cannot contain duplicate dependency IDs.",
                    nameof(_dependencies));
            }
        }
    }

    private static string RequireValue(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A non-empty value is required.",
                parameterName);
        }

        return
            value.Trim();
    }
}
