using System;
using System.Collections.Generic;
using System.Linq;
using DCML.Core.Abstractions;
using DCML.Core.Models;

namespace DCML.Core.Runtime
{
    /// <summary>
    /// Coordinates DCML module activation and lifecycle independently
    /// of the host that creates module instances.
    /// </summary>
    public sealed class DCMLModuleRuntime
    {
        private readonly IDCMLModuleActivator _activator;
        private readonly IDCMLModuleContextFactory _contextFactory;

        private readonly Dictionary<string, RuntimeModule> _modules =
            new Dictionary<string, RuntimeModule>(
                StringComparer.OrdinalIgnoreCase
            );

        private readonly List<RuntimeModule> _startOrder =
            new List<RuntimeModule>();

        public DCMLModuleRuntime(
            IDCMLModuleActivator activator,
            IDCMLModuleContextFactory contextFactory
        )
        {
            _activator =
                activator ??
                throw new ArgumentNullException(
                    nameof(activator)
                );

            _contextFactory =
                contextFactory ??
                throw new ArgumentNullException(
                    nameof(contextFactory)
                );
        }

        /// <summary>
        /// Gets whether at least one module is currently running.
        /// </summary>
        public bool IsRunning =>
            _startOrder.Any(
                module =>
                    module.State ==
                    DCMLModuleRuntimeState.Running
            );

        /// <summary>
        /// Activates, initializes, and starts modules in the supplied
        /// dependency-safe order. Failures are isolated so independent
        /// modules may continue starting.
        /// </summary>
        public DCMLModuleRuntimeResult Start(
            IReadOnlyList<DCMLModulePackage> loadOrder
        )
        {
            var issues =
                new List<DCMLModuleRuntimeIssue>();

            if (loadOrder == null)
            {
                issues.Add(
                    new DCMLModuleRuntimeIssue(
                        string.Empty,
                        "DCML_RUNTIME_LOAD_ORDER_REQUIRED",
                        "A module load order is required."
                    )
                );

                return CreateResult(
                    issues
                );
            }

            if (_modules.Count != 0)
            {
                issues.Add(
                    new DCMLModuleRuntimeIssue(
                        string.Empty,
                        "DCML_RUNTIME_ALREADY_STARTED",
                        "This DCML module runtime has already been started."
                    )
                );

                return CreateResult(
                    issues
                );
            }

            foreach (
                DCMLModulePackage package
                in loadOrder
            )
            {
                if (package == null)
                {
                    issues.Add(
                        new DCMLModuleRuntimeIssue(
                            string.Empty,
                            "DCML_RUNTIME_PACKAGE_INVALID",
                            "The load order contains a null package."
                        )
                    );

                    continue;
                }

                string moduleId =
                    package.Manifest.Id;

                if (
                    _modules.ContainsKey(
                        moduleId
                    )
                )
                {
                    issues.Add(
                        new DCMLModuleRuntimeIssue(
                            moduleId,
                            "DCML_RUNTIME_DUPLICATE_MODULE_ID",
                            "The runtime load order contains module Id '" +
                            moduleId +
                            "' more than once."
                        )
                    );

                    continue;
                }

                _modules.Add(
                    moduleId,
                    new RuntimeModule(
                        package
                    )
                );
            }

            foreach (
                DCMLModulePackage package
                in loadOrder
            )
            {
                if (package == null)
                {
                    continue;
                }

                if (
                    !_modules.TryGetValue(
                        package.Manifest.Id,
                        out RuntimeModule? runtimeModule
                    )
                )
                {
                    continue;
                }

                if (
                    runtimeModule.State !=
                    DCMLModuleRuntimeState.Pending
                )
                {
                    continue;
                }

                if (
                    TryGetUnavailableRequiredDependency(
                        package,
                        out string? dependencyId
                    )
                )
                {
                    runtimeModule.State =
                        DCMLModuleRuntimeState.Blocked;

                    issues.Add(
                        new DCMLModuleRuntimeIssue(
                            package.Manifest.Id,
                            "DCML_RUNTIME_DEPENDENCY_NOT_RUNNING",
                            "Required dependency '" +
                            dependencyId +
                            "' is not running.",
                            dependencyId
                        )
                    );

                    continue;
                }

                StartModule(
                    runtimeModule,
                    issues
                );
            }

            return CreateResult(
                issues
            );
        }

        /// <summary>
        /// Stops all running modules in reverse start order. One stop
        /// failure does not prevent other modules from stopping.
        /// </summary>
        public DCMLModuleRuntimeResult Stop()
        {
            var issues =
                new List<DCMLModuleRuntimeIssue>();

            for (
                int index =
                    _startOrder.Count - 1;
                index >= 0;
                index--
            )
            {
                RuntimeModule runtimeModule =
                    _startOrder[index];

                if (
                    runtimeModule.State !=
                    DCMLModuleRuntimeState.Running ||
                    runtimeModule.Module == null
                )
                {
                    continue;
                }

                try
                {
                    runtimeModule.Module.Stop();

                    runtimeModule.State =
                        DCMLModuleRuntimeState.Stopped;
                }
                catch (Exception exception)
                {
                    runtimeModule.State =
                        DCMLModuleRuntimeState.Failed;

                    issues.Add(
                        CreateExceptionIssue(
                            runtimeModule.Package.Manifest.Id,
                            "DCML_RUNTIME_STOP_FAILED",
                            "The module threw an exception while stopping.",
                            exception
                        )
                    );
                }
            }

            return CreateResult(
                issues
            );
        }

        private void StartModule(
            RuntimeModule runtimeModule,
            List<DCMLModuleRuntimeIssue> issues
        )
        {
            string moduleId =
                runtimeModule.Package.Manifest.Id;

            try
            {
                runtimeModule.State =
                    DCMLModuleRuntimeState.Activating;

                runtimeModule.Module =
                    _activator.Create(
                        runtimeModule.Package
                    );

                if (runtimeModule.Module == null)
                {
                    runtimeModule.State =
                        DCMLModuleRuntimeState.Failed;

                    issues.Add(
                        new DCMLModuleRuntimeIssue(
                            moduleId,
                            "DCML_RUNTIME_ACTIVATOR_RETURNED_NULL",
                            "The module activator returned null."
                        )
                    );

                    return;
                }
            }
            catch (Exception exception)
            {
                runtimeModule.State =
                    DCMLModuleRuntimeState.Failed;

                issues.Add(
                    CreateExceptionIssue(
                        moduleId,
                        "DCML_RUNTIME_ACTIVATION_FAILED",
                        "The module could not be activated.",
                        exception
                    )
                );

                return;
            }

            if (
                !string.Equals(
                    runtimeModule.Module.Id,
                    moduleId,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                runtimeModule.State =
                    DCMLModuleRuntimeState.Failed;

                issues.Add(
                    new DCMLModuleRuntimeIssue(
                        moduleId,
                        "DCML_RUNTIME_MODULE_ID_MISMATCH",
                        "The activated module reports Id '" +
                        runtimeModule.Module.Id +
                        "', but its manifest declares '" +
                        moduleId +
                        "'."
                    )
                );

                return;
            }

            IDCMLModuleContext context;

            try
            {
                context =
                    _contextFactory.CreateContext(
                        runtimeModule.Package
                    );

                if (context == null)
                {
                    runtimeModule.State =
                        DCMLModuleRuntimeState.Failed;

                    issues.Add(
                        new DCMLModuleRuntimeIssue(
                            moduleId,
                            "DCML_RUNTIME_CONTEXT_FACTORY_RETURNED_NULL",
                            "The module context factory returned null."
                        )
                    );

                    return;
                }
            }
            catch (Exception exception)
            {
                runtimeModule.State =
                    DCMLModuleRuntimeState.Failed;

                issues.Add(
                    CreateExceptionIssue(
                        moduleId,
                        "DCML_RUNTIME_CONTEXT_CREATION_FAILED",
                        "The module context could not be created.",
                        exception
                    )
                );

                return;
            }

            try
            {
                runtimeModule.State =
                    DCMLModuleRuntimeState.Initializing;

                runtimeModule.Module.Initialize(
                    context
                );
            }
            catch (Exception exception)
            {
                runtimeModule.State =
                    DCMLModuleRuntimeState.Failed;

                issues.Add(
                    CreateExceptionIssue(
                        moduleId,
                        "DCML_RUNTIME_INITIALIZE_FAILED",
                        "The module threw an exception while initializing.",
                        exception
                    )
                );

                return;
            }

            try
            {
                runtimeModule.State =
                    DCMLModuleRuntimeState.Starting;

                runtimeModule.Module.Start();

                runtimeModule.State =
                    DCMLModuleRuntimeState.Running;

                _startOrder.Add(
                    runtimeModule
                );
            }
            catch (Exception exception)
            {
                runtimeModule.State =
                    DCMLModuleRuntimeState.Failed;

                issues.Add(
                    CreateExceptionIssue(
                        moduleId,
                        "DCML_RUNTIME_START_FAILED",
                        "The module threw an exception while starting.",
                        exception
                    )
                );

                TryCleanupFailedStart(
                    runtimeModule,
                    issues
                );
            }
        }

        private void TryCleanupFailedStart(
            RuntimeModule runtimeModule,
            List<DCMLModuleRuntimeIssue> issues
        )
        {
            if (runtimeModule.Module == null)
            {
                return;
            }

            try
            {
                runtimeModule.Module.Stop();
            }
            catch (Exception exception)
            {
                issues.Add(
                    CreateExceptionIssue(
                        runtimeModule.Package.Manifest.Id,
                        "DCML_RUNTIME_START_CLEANUP_FAILED",
                        "The module also threw an exception while cleaning up after a failed start.",
                        exception
                    )
                );
            }
        }

        private bool TryGetUnavailableRequiredDependency(
            DCMLModulePackage package,
            out string? dependencyId
        )
        {
            dependencyId = null;

            if (
                package.Manifest.Dependencies == null
            )
            {
                return false;
            }

            foreach (
                DCMLModuleDependency dependency
                in package.Manifest.Dependencies
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
                    )
            )
            {
                if (
                    !_modules.TryGetValue(
                        dependency.Id,
                        out RuntimeModule? dependencyModule
                    ) ||
                    dependencyModule.State !=
                        DCMLModuleRuntimeState.Running
                )
                {
                    dependencyId =
                        dependency.Id;

                    return true;
                }
            }

            return false;
        }

        private DCMLModuleRuntimeResult CreateResult(
            IReadOnlyList<DCMLModuleRuntimeIssue> issues
        )
        {
            var entries =
                _modules.Values
                    .OrderBy(
                        module =>
                            module.Package.Manifest.Id,
                        StringComparer.OrdinalIgnoreCase
                    )
                    .Select(
                        module =>
                            new DCMLModuleRuntimeEntry(
                                module.Package,
                                module.State
                            )
                    )
                    .ToList();

            return new DCMLModuleRuntimeResult(
                entries,
                new List<DCMLModuleRuntimeIssue>(
                    issues
                )
            );
        }

        private static DCMLModuleRuntimeIssue
            CreateExceptionIssue(
                string moduleId,
                string code,
                string message,
                Exception exception
            )
        {
            return new DCMLModuleRuntimeIssue(
                moduleId,
                code,
                message + " " + exception.Message,
                null,
                exception.GetType().FullName
            );
        }

        private sealed class RuntimeModule
        {
            public RuntimeModule(
                DCMLModulePackage package
            )
            {
                Package = package;
                State =
                    DCMLModuleRuntimeState.Pending;
            }

            public DCMLModulePackage Package { get; }

            public IDCMLModule? Module { get; set; }

            public DCMLModuleRuntimeState State { get; set; }
        }
    }
}
