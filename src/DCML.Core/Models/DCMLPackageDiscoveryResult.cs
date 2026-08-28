using System.Collections.Generic;

namespace DCML.Core.Models
{
    /// <summary>
    /// Contains packages and failures produced during DCML discovery.
    /// </summary>
    public sealed class DCMLPackageDiscoveryResult
    {
        private readonly List<DCMLModulePackage> _packages =
            new List<DCMLModulePackage>();

        private readonly List<DCMLPackageDiscoveryFailure> _failures =
            new List<DCMLPackageDiscoveryFailure>();

        /// <summary>
        /// Gets successfully discovered module packages.
        /// </summary>
        public IReadOnlyList<DCMLModulePackage> Packages =>
            _packages;

        /// <summary>
        /// Gets package discovery failures.
        /// </summary>
        public IReadOnlyList<DCMLPackageDiscoveryFailure> Failures =>
            _failures;

        /// <summary>
        /// Gets whether discovery completed without package failures.
        /// </summary>
        public bool Success =>
            _failures.Count == 0;

        internal void AddPackage(
            DCMLModulePackage package
        )
        {
            _packages.Add(
                package
            );
        }

        internal void AddFailure(
            DCMLPackageDiscoveryFailure failure
        )
        {
            _failures.Add(
                failure
            );
        }
    }
}
