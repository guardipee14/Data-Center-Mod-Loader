using System;
using System.Collections.Generic;
using System.Linq;
using DCML.Core.Models;

namespace DCML.Core.Runtime
{
    /// <summary>
    /// Combines DCML discovery, compatibility, dependency-resolution,
    /// and runtime results into one host-neutral diagnostics snapshot.
    /// </summary>
    public static class DCMLDiagnosticsBuilder
    {
        public static DCMLDiagnosticsSnapshot Build(
            DCMLPackageDiscoveryResult? discovery = null,
            DCMLPackageCompatibilityResult? compatibility = null,
            DCMLDependencyResolutionResult? dependencyResolution = null,
            DCMLModuleRuntimeResult? runtime = null
        )
        {
            var diagnostics =
                new List<DCMLDiagnosticIssue>();

            var statusById =
                new Dictionary<string, DCMLModuleStatus>(
                    StringComparer.OrdinalIgnoreCase
                );

            if (discovery != null)
            {
                AddDiscovery(
                    discovery,
                    statusById,
                    diagnostics
                );
            }

            if (compatibility != null)
            {
                AddCompatibility(
                    compatibility,
                    statusById,
                    diagnostics
                );
            }

            if (dependencyResolution != null)
            {
                AddDependencyResolution(
                    dependencyResolution,
                    statusById,
                    diagnostics
                );
            }

            if (runtime != null)
            {
                AddRuntime(
                    runtime,
                    statusById,
                    diagnostics
                );
            }

            List<DCMLModuleStatus> modules =
                statusById.Values
                    .OrderBy(
                        module => module.ModuleId,
                        StringComparer.OrdinalIgnoreCase
                    )
                    .ToList();

            return new DCMLDiagnosticsSnapshot(
                modules,
                diagnostics
            );
        }

        private static void AddDiscovery(
            DCMLPackageDiscoveryResult discovery,
            IDictionary<string, DCMLModuleStatus> statusById,
            ICollection<DCMLDiagnosticIssue> diagnostics
        )
        {
            foreach (DCMLModulePackage package in discovery.Packages)
            {
                SetStatus(
                    statusById,
                    package,
                    DCMLModuleStatusState.Discovered
                );
            }

            foreach (
                DCMLPackageDiscoveryFailure failure
                in discovery.Failures
            )
            {
                diagnostics.Add(
                    new DCMLDiagnosticIssue(
                        DCMLDiagnosticStage.Discovery,
                        DCMLDiagnosticSeverity.Error,
                        failure.ErrorCode,
                        failure.ErrorMessage,
                        packageDirectory:
                            failure.PackageDirectory,
                        manifestPath:
                            failure.ManifestPath
                    )
                );

                foreach (
                    DCMLValidationIssue issue
                    in failure.ValidationIssues
                )
                {
                    diagnostics.Add(
                        new DCMLDiagnosticIssue(
                            DCMLDiagnosticStage.Validation,
                            DCMLDiagnosticSeverity.Error,
                            issue.Code,
                            issue.Message,
                            packageDirectory:
                                failure.PackageDirectory,
                            manifestPath:
                                failure.ManifestPath
                        )
                    );
                }
            }
        }

        private static void AddCompatibility(
            DCMLPackageCompatibilityResult compatibility,
            IDictionary<string, DCMLModuleStatus> statusById,
            ICollection<DCMLDiagnosticIssue> diagnostics
        )
        {
            foreach (
                DCMLModulePackage package
                in compatibility.CompatiblePackages
            )
            {
                SetStatus(
                    statusById,
                    package,
                    DCMLModuleStatusState.Compatible
                );
            }

            foreach (
                DCMLModulePackage package
                in compatibility.IncompatiblePackages
            )
            {
                SetStatus(
                    statusById,
                    package,
                    DCMLModuleStatusState.Incompatible
                );
            }

            foreach (
                DCMLPackageCompatibilityIssue issue
                in compatibility.Issues
            )
            {
                diagnostics.Add(
                    new DCMLDiagnosticIssue(
                        DCMLDiagnosticStage.Compatibility,
                        DCMLDiagnosticSeverity.Error,
                        issue.Code,
                        issue.Message,
                        moduleId:
                            issue.ModuleId,
                        requirementId:
                            issue.RequirementId
                    )
                );
            }
        }

        private static void AddDependencyResolution(
            DCMLDependencyResolutionResult dependencyResolution,
            IDictionary<string, DCMLModuleStatus> statusById,
            ICollection<DCMLDiagnosticIssue> diagnostics
        )
        {
            foreach (
                DCMLModulePackage package
                in dependencyResolution.LoadOrder
            )
            {
                SetStatus(
                    statusById,
                    package,
                    DCMLModuleStatusState.Pending
                );
            }

            foreach (
                DCMLDependencyResolutionIssue issue
                in dependencyResolution.Issues
            )
            {
                if (
                    !string.IsNullOrWhiteSpace(
                        issue.ModuleId
                    ) &&
                    statusById.TryGetValue(
                        issue.ModuleId,
                        out DCMLModuleStatus? existing
                    )
                )
                {
                    statusById[issue.ModuleId] =
                        new DCMLModuleStatus(
                            existing.ModuleId,
                            existing.Name,
                            existing.Version,
                            DCMLModuleStatusState.Blocked
                        );
                }

                diagnostics.Add(
                    new DCMLDiagnosticIssue(
                        DCMLDiagnosticStage.DependencyResolution,
                        DCMLDiagnosticSeverity.Error,
                        issue.Code,
                        issue.Message,
                        moduleId:
                            issue.ModuleId,
                        dependencyId:
                            issue.DependencyId
                    )
                );
            }
        }

        private static void AddRuntime(
            DCMLModuleRuntimeResult runtime,
            IDictionary<string, DCMLModuleStatus> statusById,
            ICollection<DCMLDiagnosticIssue> diagnostics
        )
        {
            foreach (
                DCMLModuleRuntimeEntry entry
                in runtime.Modules
            )
            {
                SetStatus(
                    statusById,
                    entry.Package,
                    MapRuntimeState(
                        entry.State
                    )
                );
            }

            foreach (
                DCMLModuleRuntimeIssue issue
                in runtime.Issues
            )
            {
                diagnostics.Add(
                    new DCMLDiagnosticIssue(
                        MapRuntimeStage(
                            issue.Code
                        ),
                        DCMLDiagnosticSeverity.Error,
                        issue.Code,
                        issue.Message,
                        moduleId:
                            issue.ModuleId,
                        dependencyId:
                            issue.DependencyId,
                        exceptionType:
                            issue.ExceptionType
                    )
                );
            }
        }

        private static void SetStatus(
            IDictionary<string, DCMLModuleStatus> statusById,
            DCMLModulePackage package,
            DCMLModuleStatusState state
        )
        {
            string moduleId =
                package.Manifest.Id;

            if (
                string.IsNullOrWhiteSpace(
                    moduleId
                )
            )
            {
                return;
            }

            statusById[moduleId] =
                new DCMLModuleStatus(
                    moduleId,
                    package.Manifest.Name,
                    package.Manifest.Version,
                    state
                );
        }

        private static DCMLModuleStatusState MapRuntimeState(
            DCMLModuleRuntimeState state
        )
        {
            switch (state)
            {
                case DCMLModuleRuntimeState.Pending:
                    return DCMLModuleStatusState.Pending;

                case DCMLModuleRuntimeState.Activating:
                    return DCMLModuleStatusState.Activating;

                case DCMLModuleRuntimeState.Initializing:
                    return DCMLModuleStatusState.Initializing;

                case DCMLModuleRuntimeState.Starting:
                    return DCMLModuleStatusState.Starting;

                case DCMLModuleRuntimeState.Running:
                    return DCMLModuleStatusState.Running;

                case DCMLModuleRuntimeState.Blocked:
                    return DCMLModuleStatusState.Blocked;

                case DCMLModuleRuntimeState.Failed:
                    return DCMLModuleStatusState.Failed;

                case DCMLModuleRuntimeState.Stopped:
                    return DCMLModuleStatusState.Stopped;

                default:
                    return DCMLModuleStatusState.Unknown;
            }
        }

        private static DCMLDiagnosticStage MapRuntimeStage(
            string code
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    code
                )
            )
            {
                return DCMLDiagnosticStage.Runtime;
            }

            if (
                code.IndexOf(
                    "ACTIVAT",
                    StringComparison.OrdinalIgnoreCase
                ) >= 0 ||
                code.IndexOf(
                    "MODULE_ID_MISMATCH",
                    StringComparison.OrdinalIgnoreCase
                ) >= 0 ||
                code.IndexOf(
                    "CONTEXT_",
                    StringComparison.OrdinalIgnoreCase
                ) >= 0
            )
            {
                return DCMLDiagnosticStage.Activation;
            }

            if (
                code.IndexOf(
                    "INITIALIZE",
                    StringComparison.OrdinalIgnoreCase
                ) >= 0
            )
            {
                return DCMLDiagnosticStage.Initialization;
            }

            if (
                code.IndexOf(
                    "START",
                    StringComparison.OrdinalIgnoreCase
                ) >= 0
            )
            {
                return DCMLDiagnosticStage.Start;
            }

            if (
                code.IndexOf(
                    "STOP",
                    StringComparison.OrdinalIgnoreCase
                ) >= 0
            )
            {
                return DCMLDiagnosticStage.Stop;
            }

            return DCMLDiagnosticStage.Runtime;
        }
    }
}
