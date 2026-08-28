using System.Collections.Generic;

namespace DCML.Core.Models
{
    /// <summary>
    /// Describes one package that DCML could not discover safely.
    /// </summary>
    public sealed class DCMLPackageDiscoveryFailure
    {
        public DCMLPackageDiscoveryFailure(
            string packageDirectory,
            string? manifestPath,
            string errorCode,
            string errorMessage,
            IReadOnlyList<DCMLValidationIssue>? validationIssues = null
        )
        {
            PackageDirectory =
                packageDirectory;

            ManifestPath =
                manifestPath;

            ErrorCode =
                errorCode;

            ErrorMessage =
                errorMessage;

            ValidationIssues =
                validationIssues ??
                new List<DCMLValidationIssue>();
        }

        /// <summary>
        /// Gets the package directory associated with the failure.
        /// </summary>
        public string PackageDirectory { get; }

        /// <summary>
        /// Gets the manifest path, when one was available.
        /// </summary>
        public string? ManifestPath { get; }

        /// <summary>
        /// Gets a stable discovery failure code.
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// Gets a human-readable failure message.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Gets manifest validation issues associated with the failure.
        /// </summary>
        public IReadOnlyList<DCMLValidationIssue> ValidationIssues { get; }
    }
}
