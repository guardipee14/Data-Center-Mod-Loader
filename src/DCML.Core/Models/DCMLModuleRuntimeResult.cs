using System.Collections.Generic;

namespace DCML.Core.Models
{
    /// <summary>
    /// Contains a snapshot of module states and lifecycle issues.
    /// </summary>
    public sealed class DCMLModuleRuntimeResult
    {
        public DCMLModuleRuntimeResult(
            IReadOnlyList<DCMLModuleRuntimeEntry> modules,
            IReadOnlyList<DCMLModuleRuntimeIssue> issues
        )
        {
            Modules = modules;
            Issues = issues;
        }

        /// <summary>
        /// Gets the module-state snapshot.
        /// </summary>
        public IReadOnlyList<DCMLModuleRuntimeEntry> Modules { get; }

        /// <summary>
        /// Gets lifecycle issues produced by the operation.
        /// </summary>
        public IReadOnlyList<DCMLModuleRuntimeIssue> Issues { get; }

        /// <summary>
        /// Gets whether the operation completed without lifecycle issues.
        /// </summary>
        public bool Success =>
            Issues.Count == 0;
    }
}
