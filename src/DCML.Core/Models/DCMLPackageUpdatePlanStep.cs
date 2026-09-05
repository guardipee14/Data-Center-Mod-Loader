using System;

namespace DCML.Core.Models;

/// <summary>
/// Describes one non-mutating update-plan step. Steps are ordered so required
/// dependency updates appear before their dependents.
/// </summary>
public sealed class DCMLPackageUpdatePlanStep
{
    public DCMLPackageUpdatePlanStep(
        DCMLInstalledPackageVersion installed,
        DCMLPackageUpdateMetadata target,
        DCMLPackageVersionDecision versionDecision)
    {
        Installed =
            installed ?? throw new ArgumentNullException(
                nameof(installed));

        Target =
            target ?? throw new ArgumentNullException(
                nameof(target));

        VersionDecision =
            versionDecision ?? throw new ArgumentNullException(
                nameof(versionDecision));

        if (
            !string.Equals(
                Installed.ModuleId,
                Target.ModuleId,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Installed and target module IDs must match.",
                nameof(target));
        }
    }

    public DCMLInstalledPackageVersion Installed { get; }

    public DCMLPackageUpdateMetadata Target { get; }

    public DCMLPackageVersionDecision VersionDecision { get; }

    public string ModuleId =>
        Target.ModuleId;
}
