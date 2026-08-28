using System.Collections.Generic;

namespace DCML.Core.Models
{
    /// <summary>
    /// Contains the deterministic load order and any modules that
    /// could not be resolved.
    /// </summary>
    public sealed class DCMLDependencyResolutionResult
    {
        private readonly List<DCMLModulePackage> _loadOrder =
            new List<DCMLModulePackage>();

        private readonly List<DCMLDependencyResolutionIssue> _issues =
            new List<DCMLDependencyResolutionIssue>();

        /// <summary>
        /// Gets packages that can load, in dependency-safe order.
        /// </summary>
        public IReadOnlyList<DCMLModulePackage> LoadOrder =>
            _loadOrder;

        /// <summary>
        /// Gets dependency-resolution issues.
        /// </summary>
        public IReadOnlyList<DCMLDependencyResolutionIssue> Issues =>
            _issues;

        /// <summary>
        /// Gets whether every supplied package was resolved.
        /// </summary>
        public bool Success =>
            _issues.Count == 0;

        internal void AddToLoadOrder(
            DCMLModulePackage package
        )
        {
            _loadOrder.Add(
                package
            );
        }

        internal void AddIssue(
            DCMLDependencyResolutionIssue issue
        )
        {
            _issues.Add(
                issue
            );
        }
    }
}
