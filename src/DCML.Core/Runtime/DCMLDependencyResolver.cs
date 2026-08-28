using System;
using System.Collections.Generic;
using System.Linq;
using DCML.Core.Models;

namespace DCML.Core.Runtime
{
    /// <summary>
    /// Resolves required DCML module dependencies and produces a
    /// deterministic dependency-safe load order.
    /// </summary>
    public static class DCMLDependencyResolver
    {
        /// <summary>
        /// Resolves a discovered package set.
        /// </summary>
        public static DCMLDependencyResolutionResult Resolve(
            IReadOnlyList<DCMLModulePackage> packages
        )
        {
            var result =
                new DCMLDependencyResolutionResult();

            if (packages == null)
            {
                result.AddIssue(
                    new DCMLDependencyResolutionIssue(
                        string.Empty,
                        "DCML_RESOLUTION_PACKAGES_REQUIRED",
                        "A package collection is required."
                    )
                );

                return result;
            }

            var packageById =
                new Dictionary<string, DCMLModulePackage>(
                    StringComparer.OrdinalIgnoreCase
                );

            foreach (
                DCMLModulePackage package
                in packages
                    .Where(
                        package =>
                            package != null
                    )
                    .OrderBy(
                        package =>
                            package.Manifest.Id,
                        StringComparer.OrdinalIgnoreCase
                    )
            )
            {
                if (
                    packageById.ContainsKey(
                        package.Manifest.Id
                    )
                )
                {
                    result.AddIssue(
                        new DCMLDependencyResolutionIssue(
                            package.Manifest.Id,
                            "DCML_RESOLUTION_DUPLICATE_MODULE_ID",
                            "More than one package uses module Id '" +
                            package.Manifest.Id +
                            "'."
                        )
                    );

                    continue;
                }

                packageById.Add(
                    package.Manifest.Id,
                    package
                );
            }

            var blocked =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            ValidateDirectDependencies(
                packageById,
                blocked,
                result
            );

            PropagateBlockedDependencies(
                packageById,
                blocked,
                result
            );

            MarkDependencyCycles(
                packageById,
                blocked,
                result
            );

            PropagateBlockedDependencies(
                packageById,
                blocked,
                result
            );

            BuildLoadOrder(
                packageById,
                blocked,
                result
            );

            return result;
        }

        private static void ValidateDirectDependencies(
            IReadOnlyDictionary<string, DCMLModulePackage> packageById,
            HashSet<string> blocked,
            DCMLDependencyResolutionResult result
        )
        {
            foreach (
                DCMLModulePackage package
                in packageById.Values.OrderBy(
                    package =>
                        package.Manifest.Id,
                    StringComparer.OrdinalIgnoreCase
                )
            )
            {
                string moduleId =
                    package.Manifest.Id;

                foreach (
                    DCMLModuleDependency dependency
                    in GetRequiredDependencies(
                        package
                    )
                )
                {
                    if (
                        !packageById.TryGetValue(
                            dependency.Id,
                            out DCMLModulePackage? dependencyPackage
                        )
                    )
                    {
                        result.AddIssue(
                            new DCMLDependencyResolutionIssue(
                                moduleId,
                                "DCML_DEPENDENCY_MISSING",
                                "Required dependency '" +
                                dependency.Id +
                                "' is not installed.",
                                dependency.Id
                            )
                        );

                        blocked.Add(
                            moduleId
                        );

                        continue;
                    }

                    if (
                        string.IsNullOrWhiteSpace(
                            dependency.MinimumVersion
                        )
                    )
                    {
                        continue;
                    }

                    if (
                        !DCMLSemanticVersion.TryCompare(
                            dependencyPackage.Manifest.Version,
                            dependency.MinimumVersion,
                            out int comparison
                        )
                    )
                    {
                        result.AddIssue(
                            new DCMLDependencyResolutionIssue(
                                moduleId,
                                "DCML_DEPENDENCY_VERSION_INVALID",
                                "Dependency version information for '" +
                                dependency.Id +
                                "' could not be compared.",
                                dependency.Id
                            )
                        );

                        blocked.Add(
                            moduleId
                        );

                        continue;
                    }

                    if (comparison < 0)
                    {
                        result.AddIssue(
                            new DCMLDependencyResolutionIssue(
                                moduleId,
                                "DCML_DEPENDENCY_VERSION_UNSATISFIED",
                                "Dependency '" +
                                dependency.Id +
                                "' is version '" +
                                dependencyPackage.Manifest.Version +
                                "', but version '" +
                                dependency.MinimumVersion +
                                "' or newer is required.",
                                dependency.Id
                            )
                        );

                        blocked.Add(
                            moduleId
                        );
                    }
                }
            }
        }

        private static void PropagateBlockedDependencies(
            IReadOnlyDictionary<string, DCMLModulePackage> packageById,
            HashSet<string> blocked,
            DCMLDependencyResolutionResult result
        )
        {
            bool changed;

            do
            {
                changed = false;

                foreach (
                    DCMLModulePackage package
                    in packageById.Values.OrderBy(
                        package =>
                            package.Manifest.Id,
                        StringComparer.OrdinalIgnoreCase
                    )
                )
                {
                    string moduleId =
                        package.Manifest.Id;

                    if (
                        blocked.Contains(
                            moduleId
                        )
                    )
                    {
                        continue;
                    }

                    foreach (
                        DCMLModuleDependency dependency
                        in GetRequiredDependencies(
                            package
                        )
                    )
                    {
                        if (
                            !packageById.ContainsKey(
                                dependency.Id
                            ) ||
                            !blocked.Contains(
                                dependency.Id
                            )
                        )
                        {
                            continue;
                        }

                        result.AddIssue(
                            new DCMLDependencyResolutionIssue(
                                moduleId,
                                "DCML_DEPENDENCY_BLOCKED",
                                "Required dependency '" +
                                dependency.Id +
                                "' cannot be loaded.",
                                dependency.Id
                            )
                        );

                        blocked.Add(
                            moduleId
                        );

                        changed = true;
                        break;
                    }
                }
            }
            while (changed);
        }

        private static void MarkDependencyCycles(
            IReadOnlyDictionary<string, DCMLModulePackage> packageById,
            HashSet<string> blocked,
            DCMLDependencyResolutionResult result
        )
        {
            var indexById =
                new Dictionary<string, int>(
                    StringComparer.OrdinalIgnoreCase
                );

            var lowLinkById =
                new Dictionary<string, int>(
                    StringComparer.OrdinalIgnoreCase
                );

            var stack =
                new Stack<string>();

            var onStack =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            var components =
                new List<List<string>>();

            int nextIndex = 0;

            foreach (
                string moduleId
                in packageById.Keys
                    .Where(
                        moduleId =>
                            !blocked.Contains(
                                moduleId
                            )
                    )
                    .OrderBy(
                        moduleId =>
                            moduleId,
                        StringComparer.OrdinalIgnoreCase
                    )
            )
            {
                if (
                    indexById.ContainsKey(
                        moduleId
                    )
                )
                {
                    continue;
                }

                StrongConnect(
                    moduleId,
                    packageById,
                    blocked,
                    indexById,
                    lowLinkById,
                    stack,
                    onStack,
                    components,
                    ref nextIndex
                );
            }

            foreach (
                List<string> component
                in components
            )
            {
                bool isCycle =
                    component.Count > 1;

                if (
                    component.Count == 1 &&
                    HasRequiredDependency(
                        packageById[component[0]],
                        component[0]
                    )
                )
                {
                    isCycle = true;
                }

                if (!isCycle)
                {
                    continue;
                }

                string[] ordered =
                    component
                        .OrderBy(
                            id =>
                                id,
                            StringComparer.OrdinalIgnoreCase
                        )
                        .ToArray();

                string cycleDescription =
                    string.Join(
                        " -> ",
                        ordered
                    );

                foreach (string moduleId in ordered)
                {
                    if (
                        blocked.Add(
                            moduleId
                        )
                    )
                    {
                        result.AddIssue(
                            new DCMLDependencyResolutionIssue(
                                moduleId,
                                "DCML_DEPENDENCY_CYCLE",
                                "The module participates in a required dependency cycle: " +
                                cycleDescription +
                                "."
                            )
                        );
                    }
                }
            }
        }

        private static void StrongConnect(
            string moduleId,
            IReadOnlyDictionary<string, DCMLModulePackage> packageById,
            HashSet<string> blocked,
            Dictionary<string, int> indexById,
            Dictionary<string, int> lowLinkById,
            Stack<string> stack,
            HashSet<string> onStack,
            List<List<string>> components,
            ref int nextIndex
        )
        {
            indexById[moduleId] =
                nextIndex;

            lowLinkById[moduleId] =
                nextIndex;

            nextIndex++;

            stack.Push(
                moduleId
            );

            onStack.Add(
                moduleId
            );

            foreach (
                DCMLModuleDependency dependency
                in GetRequiredDependencies(
                    packageById[moduleId]
                )
            )
            {
                string dependencyId =
                    dependency.Id;

                if (
                    blocked.Contains(
                        dependencyId
                    ) ||
                    !packageById.ContainsKey(
                        dependencyId
                    )
                )
                {
                    continue;
                }

                if (
                    !indexById.ContainsKey(
                        dependencyId
                    )
                )
                {
                    StrongConnect(
                        dependencyId,
                        packageById,
                        blocked,
                        indexById,
                        lowLinkById,
                        stack,
                        onStack,
                        components,
                        ref nextIndex
                    );

                    lowLinkById[moduleId] =
                        Math.Min(
                            lowLinkById[moduleId],
                            lowLinkById[dependencyId]
                        );
                }
                else if (
                    onStack.Contains(
                        dependencyId
                    )
                )
                {
                    lowLinkById[moduleId] =
                        Math.Min(
                            lowLinkById[moduleId],
                            indexById[dependencyId]
                        );
                }
            }

            if (
                lowLinkById[moduleId] !=
                indexById[moduleId]
            )
            {
                return;
            }

            var component =
                new List<string>();

            string currentId;

            do
            {
                currentId =
                    stack.Pop();

                onStack.Remove(
                    currentId
                );

                component.Add(
                    currentId
                );
            }
            while (
                !string.Equals(
                    currentId,
                    moduleId,
                    StringComparison.OrdinalIgnoreCase
                )
            );

            components.Add(
                component
            );
        }

        private static void BuildLoadOrder(
            IReadOnlyDictionary<string, DCMLModulePackage> packageById,
            HashSet<string> blocked,
            DCMLDependencyResolutionResult result
        )
        {
            string[] eligibleIds =
                packageById.Keys
                    .Where(
                        moduleId =>
                            !blocked.Contains(
                                moduleId
                            )
                    )
                    .OrderBy(
                        moduleId =>
                            moduleId,
                        StringComparer.OrdinalIgnoreCase
                    )
                    .ToArray();

            var indegree =
                new Dictionary<string, int>(
                    StringComparer.OrdinalIgnoreCase
                );

            var dependents =
                new Dictionary<string, List<string>>(
                    StringComparer.OrdinalIgnoreCase
                );

            foreach (string moduleId in eligibleIds)
            {
                indegree[moduleId] = 0;

                dependents[moduleId] =
                    new List<string>();
            }

            foreach (string moduleId in eligibleIds)
            {
                foreach (
                    DCMLModuleDependency dependency
                    in GetRequiredDependencies(
                        packageById[moduleId]
                    )
                )
                {
                    if (
                        blocked.Contains(
                            dependency.Id
                        ) ||
                        !indegree.ContainsKey(
                            dependency.Id
                        )
                    )
                    {
                        continue;
                    }

                    indegree[moduleId]++;

                    dependents[dependency.Id].Add(
                        moduleId
                    );
                }
            }

            var ready =
                new SortedSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            foreach (
                string moduleId
                in eligibleIds
            )
            {
                if (
                    indegree[moduleId] == 0
                )
                {
                    ready.Add(
                        moduleId
                    );
                }
            }

            while (ready.Count > 0)
            {
                string moduleId =
                    ready.Min!;

                ready.Remove(
                    moduleId
                );

                result.AddToLoadOrder(
                    packageById[moduleId]
                );

                foreach (
                    string dependentId
                    in dependents[moduleId]
                        .OrderBy(
                            id =>
                                id,
                            StringComparer.OrdinalIgnoreCase
                        )
                )
                {
                    indegree[dependentId]--;

                    if (
                        indegree[dependentId] == 0
                    )
                    {
                        ready.Add(
                            dependentId
                        );
                    }
                }
            }
        }

        private static IEnumerable<DCMLModuleDependency>
            GetRequiredDependencies(
                DCMLModulePackage package
            )
        {
            if (
                package.Manifest.Dependencies == null
            )
            {
                return Enumerable.Empty<
                    DCMLModuleDependency
                >();
            }

            return package.Manifest.Dependencies
                .Where(
                    dependency =>
                        dependency != null &&
                        !dependency.Optional &&
                        !string.IsNullOrWhiteSpace(
                            dependency.Id
                        )
                )
                .OrderBy(
                    dependency =>
                        dependency.Id,
                    StringComparer.OrdinalIgnoreCase
                );
        }

        private static bool HasRequiredDependency(
            DCMLModulePackage package,
            string dependencyId
        )
        {
            return GetRequiredDependencies(
                package
            ).Any(
                dependency =>
                    string.Equals(
                        dependency.Id,
                        dependencyId,
                        StringComparison.OrdinalIgnoreCase
                    )
            );
        }
    }
}
