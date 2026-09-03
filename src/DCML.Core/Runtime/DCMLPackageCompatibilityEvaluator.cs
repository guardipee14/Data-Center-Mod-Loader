using System;
using System.Collections.Generic;
using System.Linq;
using DCML.Core.Abstractions;
using DCML.Core.Models;

namespace DCML.Core.Runtime;

public static class DCMLPackageCompatibilityEvaluator
{
    public static DCMLPackageCompatibilityResult Evaluate(
        IReadOnlyList<DCMLModulePackage> packages,
        string dcmlVersion,
        IEnumerable<DCMLCapabilityDescriptor> capabilities)
    {
        if (packages is null)
        {
            throw new ArgumentNullException(
                nameof(packages));
        }

        if (
            string.IsNullOrWhiteSpace(dcmlVersion) ||
            !DCMLSemanticVersion.IsValid(
                dcmlVersion))
        {
            throw new ArgumentException(
                "The active DCML version must be a valid Semantic Versioning 2.0.0 value.",
                nameof(dcmlVersion));
        }

        if (capabilities is null)
        {
            throw new ArgumentNullException(
                nameof(capabilities));
        }

        var capabilityById =
            BuildCapabilityMap(
                capabilities);

        var result =
            new DCMLPackageCompatibilityResult();

        var blocked =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (
            DCMLModulePackage package
            in packages.Where(
                package =>
                    package is not null))
        {
            EvaluateDirectRequirements(
                package,
                dcmlVersion,
                capabilityById,
                blocked,
                result);
        }

        PropagateRequiredDependencyIncompatibility(
            packages,
            blocked,
            result);

        foreach (
            DCMLModulePackage package
            in packages.Where(
                package =>
                    package is not null))
        {
            if (
                blocked.Contains(
                    package.Manifest.Id))
            {
                result.AddIncompatible(
                    package);
            }
            else
            {
                result.AddCompatible(
                    package);
            }
        }

        return result;
    }

    private static Dictionary<string, DCMLCapabilityDescriptor> BuildCapabilityMap(
        IEnumerable<DCMLCapabilityDescriptor> capabilities)
    {
        var capabilityById =
            new Dictionary<string, DCMLCapabilityDescriptor>(
                StringComparer.OrdinalIgnoreCase);

        foreach (DCMLCapabilityDescriptor descriptor in capabilities)
        {
            if (descriptor is null)
            {
                throw new ArgumentException(
                    "Capability descriptors cannot contain null entries.",
                    nameof(capabilities));
            }

            if (
                capabilityById.TryGetValue(
                    descriptor.Id,
                    out DCMLCapabilityDescriptor? existing))
            {
                if (
                    !string.Equals(
                        existing.Version,
                        descriptor.Version,
                        StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        "Capability '" +
                        descriptor.Id +
                        "' was advertised with conflicting versions.");
                }

                continue;
            }

            capabilityById.Add(
                descriptor.Id,
                descriptor);
        }

        return capabilityById;
    }

    private static void EvaluateDirectRequirements(
        DCMLModulePackage package,
        string dcmlVersion,
        IReadOnlyDictionary<string, DCMLCapabilityDescriptor> capabilityById,
        HashSet<string> blocked,
        DCMLPackageCompatibilityResult result)
    {
        string moduleId =
            package.Manifest.Id;

        if (
            !string.IsNullOrWhiteSpace(
                package.Manifest.MinimumDCMLVersion))
        {
            if (
                !DCMLSemanticVersion.TryCompare(
                    dcmlVersion,
                    package.Manifest.MinimumDCMLVersion,
                    out int comparison))
            {
                Block(
                    package,
                    blocked,
                    result,
                    "DCML_COMPATIBILITY_DCML_VERSION_INVALID",
                    "The package MinimumDCMLVersion could not be compared with the active DCML version.");

                return;
            }

            if (comparison < 0)
            {
                Block(
                    package,
                    blocked,
                    result,
                    "DCML_COMPATIBILITY_DCML_VERSION_UNSATISFIED",
                    "Module '" +
                    moduleId +
                    "' requires DCML " +
                    package.Manifest.MinimumDCMLVersion +
                    " or newer, but the active version is " +
                    dcmlVersion +
                    ".");

                return;
            }
        }

        if (package.Manifest.RequiredCapabilities is null)
        {
            Block(
                package,
                blocked,
                result,
                "DCML_COMPATIBILITY_CAPABILITY_REQUIREMENTS_INVALID",
                "The package RequiredCapabilities collection is null.");

            return;
        }

        foreach (
            DCMLCapabilityRequirement requirement
            in package.Manifest.RequiredCapabilities)
        {
            if (
                requirement is null ||
                string.IsNullOrWhiteSpace(
                    requirement.Id))
            {
                Block(
                    package,
                    blocked,
                    result,
                    "DCML_COMPATIBILITY_CAPABILITY_REQUIREMENT_INVALID",
                    "The package contains an invalid capability requirement.");

                return;
            }

            string capabilityId =
                requirement.Id.Trim();

            if (
                !capabilityById.TryGetValue(
                    capabilityId,
                    out DCMLCapabilityDescriptor? available))
            {
                Block(
                    package,
                    blocked,
                    result,
                    "DCML_COMPATIBILITY_CAPABILITY_MISSING",
                    "Module '" +
                    moduleId +
                    "' requires runtime capability '" +
                    capabilityId +
                    "', but the active host does not advertise it.",
                    capabilityId);

                return;
            }

            if (
                string.IsNullOrWhiteSpace(
                    requirement.MinimumVersion))
            {
                continue;
            }

            if (
                !DCMLSemanticVersion.TryCompare(
                    available.Version,
                    requirement.MinimumVersion,
                    out int capabilityComparison))
            {
                Block(
                    package,
                    blocked,
                    result,
                    "DCML_COMPATIBILITY_CAPABILITY_VERSION_INVALID",
                    "Capability version information for '" +
                    capabilityId +
                    "' could not be compared.",
                    capabilityId);

                return;
            }

            if (capabilityComparison < 0)
            {
                Block(
                    package,
                    blocked,
                    result,
                    "DCML_COMPATIBILITY_CAPABILITY_VERSION_UNSATISFIED",
                    "Module '" +
                    moduleId +
                    "' requires capability '" +
                    capabilityId +
                    "' version " +
                    requirement.MinimumVersion +
                    " or newer, but the host advertises " +
                    available.Version +
                    ".",
                    capabilityId);

                return;
            }
        }
    }

    private static void PropagateRequiredDependencyIncompatibility(
        IReadOnlyList<DCMLModulePackage> packages,
        HashSet<string> blocked,
        DCMLPackageCompatibilityResult result)
    {
        bool changed;

        do
        {
            changed =
                false;

            foreach (
                DCMLModulePackage package
                in packages.Where(
                    package =>
                        package is not null))
            {
                string moduleId =
                    package.Manifest.Id;

                if (
                    blocked.Contains(
                        moduleId))
                {
                    continue;
                }

                IEnumerable<DCMLModuleDependency> requiredDependencies =
                    (package.Manifest.Dependencies ??
                        new List<DCMLModuleDependency>())
                    .Where(
                        dependency =>
                            dependency is not null &&
                            !dependency.Optional &&
                            !string.IsNullOrWhiteSpace(
                                dependency.Id));

                foreach (
                    DCMLModuleDependency dependency
                    in requiredDependencies)
                {
                    if (
                        !blocked.Contains(
                            dependency.Id))
                    {
                        continue;
                    }

                    Block(
                        package,
                        blocked,
                        result,
                        "DCML_COMPATIBILITY_DEPENDENCY_INCOMPATIBLE",
                        "Required dependency '" +
                        dependency.Id +
                        "' is incompatible with the active DCML runtime or host capabilities.",
                        dependency.Id);

                    changed =
                        true;

                    break;
                }
            }
        }
        while (changed);
    }

    private static void Block(
        DCMLModulePackage package,
        HashSet<string> blocked,
        DCMLPackageCompatibilityResult result,
        string code,
        string message,
        string? requirementId = null)
    {
        if (
            !blocked.Add(
                package.Manifest.Id))
        {
            return;
        }

        result.AddIssue(
            new DCMLPackageCompatibilityIssue(
                package.Manifest.Id,
                code,
                message,
                requirementId));
    }
}
