using System;
using System.Collections.Generic;
using System.Linq;
using DCML.Core.Models;

namespace DCML.Core.Runtime;

/// <summary>
/// Builds deterministic, dependency-aware update plans from installed-version
/// evidence and source-provided metadata. Planning is non-mutating.
/// </summary>
public static class DCMLPackageUpdatePlanner
{
    public static DCMLPackageUpdatePlanResult Plan(
        IReadOnlyList<DCMLInstalledPackageVersion>? installedPackages,
        IReadOnlyList<DCMLPackageUpdateMetadata>? availableMetadata,
        IReadOnlyList<string>? requestedModuleIds,
        DCMLPackageVersionPolicyOptions? policyOptions = null)
    {
        var result =
            new DCMLPackageUpdatePlanResult();

        if (installedPackages == null)
        {
            result.AddIssue(
                new DCMLPackageUpdatePlanIssue(
                    string.Empty,
                    "DCML_UPDATE_PLAN_INSTALLED_REQUIRED",
                    "Installed package-version evidence is required."));

            return result;
        }

        if (availableMetadata == null)
        {
            result.AddIssue(
                new DCMLPackageUpdatePlanIssue(
                    string.Empty,
                    "DCML_UPDATE_PLAN_METADATA_REQUIRED",
                    "Available package update metadata is required."));

            return result;
        }

        if (requestedModuleIds == null)
        {
            result.AddIssue(
                new DCMLPackageUpdatePlanIssue(
                    string.Empty,
                    "DCML_UPDATE_PLAN_REQUEST_REQUIRED",
                    "At least one requested module ID is required."));

            return result;
        }

        var installedById =
            new Dictionary<string, DCMLInstalledPackageVersion>(
                StringComparer.OrdinalIgnoreCase);

        foreach (
            DCMLInstalledPackageVersion? installed
            in installedPackages
                .Where(value => value != null)
                .OrderBy(
                    value => value.ModuleId,
                    StringComparer.OrdinalIgnoreCase))
        {
            if (installedById.ContainsKey(installed.ModuleId))
            {
                result.AddIssue(
                    new DCMLPackageUpdatePlanIssue(
                        installed.ModuleId,
                        "DCML_UPDATE_PLAN_DUPLICATE_INSTALLED_MODULE",
                        "More than one installed-version record uses module ID '" +
                        installed.ModuleId +
                        "'."));

                continue;
            }

            installedById.Add(
                installed.ModuleId,
                installed);
        }

        var metadataById =
            new Dictionary<string, DCMLPackageUpdateMetadata>(
                StringComparer.OrdinalIgnoreCase);

        foreach (
            DCMLPackageUpdateMetadata? metadata
            in availableMetadata
                .Where(value => value != null)
                .OrderBy(
                    value => value.ModuleId,
                    StringComparer.OrdinalIgnoreCase))
        {
            if (metadataById.ContainsKey(metadata.ModuleId))
            {
                result.AddIssue(
                    new DCMLPackageUpdatePlanIssue(
                        metadata.ModuleId,
                        "DCML_UPDATE_PLAN_DUPLICATE_METADATA_MODULE",
                        "More than one metadata record uses module ID '" +
                        metadata.ModuleId +
                        "'."));

                continue;
            }

            metadataById.Add(
                metadata.ModuleId,
                metadata);
        }

        if (!result.Success)
        {
            return result;
        }

        string[] requested =
            requestedModuleIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToArray();

        if (requested.Length == 0)
        {
            result.AddIssue(
                new DCMLPackageUpdatePlanIssue(
                    string.Empty,
                    "DCML_UPDATE_PLAN_REQUEST_REQUIRED",
                    "At least one non-empty requested module ID is required."));

            return result;
        }

        var visited =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        var visiting =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        DCMLPackageVersionPolicyOptions effectivePolicy =
            policyOptions ??
            DCMLPackageVersionPolicyOptions.SafeDefault;

        foreach (string moduleId in requested)
        {
            if (!installedById.ContainsKey(moduleId))
            {
                result.AddIssue(
                    new DCMLPackageUpdatePlanIssue(
                        moduleId,
                        "DCML_UPDATE_PLAN_REQUEST_NOT_INSTALLED",
                        "Requested module '" + moduleId + "' is not installed."));

                continue;
            }

            if (!metadataById.ContainsKey(moduleId))
            {
                result.AddIssue(
                    new DCMLPackageUpdatePlanIssue(
                        moduleId,
                        "DCML_UPDATE_PLAN_REQUEST_METADATA_MISSING",
                        "No update metadata is available for requested module '" +
                        moduleId +
                        "'."));

                continue;
            }

            Visit(
                moduleId,
                installedById,
                metadataById,
                effectivePolicy,
                visited,
                visiting,
                result);
        }

        return result;
    }

    private static bool Visit(
        string moduleId,
        IReadOnlyDictionary<string, DCMLInstalledPackageVersion> installedById,
        IReadOnlyDictionary<string, DCMLPackageUpdateMetadata> metadataById,
        DCMLPackageVersionPolicyOptions policyOptions,
        HashSet<string> visited,
        HashSet<string> visiting,
        DCMLPackageUpdatePlanResult result)
    {
        if (visited.Contains(moduleId))
        {
            return true;
        }

        if (!visiting.Add(moduleId))
        {
            result.AddIssue(
                new DCMLPackageUpdatePlanIssue(
                    moduleId,
                    "DCML_UPDATE_PLAN_DEPENDENCY_CYCLE",
                    "The planned update graph contains a required dependency cycle involving module '" +
                    moduleId +
                    "'."));

            return false;
        }

        DCMLInstalledPackageVersion installed =
            installedById[moduleId];

        DCMLPackageUpdateMetadata metadata =
            metadataById[moduleId];

        DCMLPackageVersionDecision decision =
            DCMLPackageVersionPolicy.Evaluate(
                installed.Version,
                metadata.Version,
                policyOptions);

        if (
            decision.Recommendation ==
            DCMLPackageVersionRecommendation.Blocked)
        {
            result.AddIssue(
                new DCMLPackageUpdatePlanIssue(
                    moduleId,
                    "DCML_UPDATE_PLAN_VERSION_BLOCKED",
                    "Version policy blocked module '" +
                    moduleId +
                    "': " +
                    decision.ReasonCode +
                    "."));

            visiting.Remove(moduleId);
            return false;
        }

        if (
            decision.Recommendation ==
            DCMLPackageVersionRecommendation.NoAction)
        {
            visiting.Remove(moduleId);
            visited.Add(moduleId);
            return true;
        }

        bool dependenciesSatisfied =
            true;

        foreach (
            DCMLPackageUpdateDependency dependency
            in metadata.Dependencies
                .Where(value => !value.Optional)
                .OrderBy(
                    value => value.Id,
                    StringComparer.OrdinalIgnoreCase))
        {
            if (
                !installedById.TryGetValue(
                    dependency.Id,
                    out DCMLInstalledPackageVersion? installedDependency))
            {
                result.AddIssue(
                    new DCMLPackageUpdatePlanIssue(
                        moduleId,
                        "DCML_UPDATE_PLAN_DEPENDENCY_MISSING",
                        "Required dependency '" +
                        dependency.Id +
                        "' is not installed.",
                        dependency.Id));

                dependenciesSatisfied =
                    false;
                continue;
            }

            if (string.IsNullOrWhiteSpace(dependency.MinimumVersion))
            {
                continue;
            }

            if (
                !DCMLSemanticVersion.TryCompare(
                    installedDependency.Version,
                    dependency.MinimumVersion,
                    out int installedComparison))
            {
                result.AddIssue(
                    new DCMLPackageUpdatePlanIssue(
                        moduleId,
                        "DCML_UPDATE_PLAN_DEPENDENCY_VERSION_INVALID",
                        "Installed dependency version information for '" +
                        dependency.Id +
                        "' could not be compared.",
                        dependency.Id));

                dependenciesSatisfied =
                    false;
                continue;
            }

            if (installedComparison >= 0)
            {
                continue;
            }

            if (
                !metadataById.TryGetValue(
                    dependency.Id,
                    out DCMLPackageUpdateMetadata? dependencyMetadata))
            {
                result.AddIssue(
                    new DCMLPackageUpdatePlanIssue(
                        moduleId,
                        "DCML_UPDATE_PLAN_DEPENDENCY_METADATA_MISSING",
                        "Required dependency '" +
                        dependency.Id +
                        "' is too old and no update metadata is available.",
                        dependency.Id));

                dependenciesSatisfied =
                    false;
                continue;
            }

            if (
                !DCMLSemanticVersion.TryCompare(
                    dependencyMetadata.Version,
                    dependency.MinimumVersion,
                    out int targetComparison) ||
                targetComparison < 0)
            {
                result.AddIssue(
                    new DCMLPackageUpdatePlanIssue(
                        moduleId,
                        "DCML_UPDATE_PLAN_DEPENDENCY_TARGET_UNSATISFIED",
                        "Available update metadata for dependency '" +
                        dependency.Id +
                        "' does not satisfy minimum version '" +
                        dependency.MinimumVersion +
                        "'.",
                        dependency.Id));

                dependenciesSatisfied =
                    false;
                continue;
            }

            DCMLPackageVersionDecision dependencyDecision =
                DCMLPackageVersionPolicy.Evaluate(
                    installedDependency.Version,
                    dependencyMetadata.Version,
                    policyOptions);

            if (
                dependencyDecision.Recommendation ==
                    DCMLPackageVersionRecommendation.Blocked ||
                dependencyDecision.Recommendation ==
                    DCMLPackageVersionRecommendation.NoAction)
            {
                result.AddIssue(
                    new DCMLPackageUpdatePlanIssue(
                        moduleId,
                        "DCML_UPDATE_PLAN_DEPENDENCY_UPDATE_BLOCKED",
                        "Dependency '" +
                        dependency.Id +
                        "' requires an update, but version policy did not permit an actionable transition.",
                        dependency.Id));

                dependenciesSatisfied =
                    false;
                continue;
            }

            if (
                !Visit(
                    dependency.Id,
                    installedById,
                    metadataById,
                    policyOptions,
                    visited,
                    visiting,
                    result))
            {
                dependenciesSatisfied =
                    false;
            }
        }

        visiting.Remove(moduleId);

        if (!dependenciesSatisfied)
        {
            return false;
        }

        visited.Add(moduleId);

        result.AddStep(
            new DCMLPackageUpdatePlanStep(
                installed,
                metadata,
                decision));

        return true;
    }
}
