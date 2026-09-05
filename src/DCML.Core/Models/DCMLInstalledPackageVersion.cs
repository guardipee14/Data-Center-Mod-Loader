using System;
using DCML.Core.Runtime;

namespace DCML.Core.Models;

/// <summary>
/// Describes installed package-version evidence used by update planning.
/// </summary>
public sealed class DCMLInstalledPackageVersion
{
    public DCMLInstalledPackageVersion(
        string moduleId,
        string version)
    {
        if (string.IsNullOrWhiteSpace(moduleId))
        {
            throw new ArgumentException(
                "Module ID cannot be empty.",
                nameof(moduleId));
        }

        string normalizedVersion =
            version?.Trim() ??
            string.Empty;

        if (!DCMLSemanticVersion.IsValid(normalizedVersion))
        {
            throw new ArgumentException(
                "Installed package version must be a valid Semantic Versioning 2.0.0 value.",
                nameof(version));
        }

        ModuleId =
            moduleId.Trim();

        Version =
            normalizedVersion;
    }

    public string ModuleId { get; }

    public string Version { get; }
}
