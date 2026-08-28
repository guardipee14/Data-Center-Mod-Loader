using System.Collections.Generic;

namespace DCML.Core.Models
{
    /// <summary>
    /// Contains the result of validating a DCML manifest.
    /// </summary>
    public sealed class DCMLValidationResult
    {
        private readonly List<DCMLValidationIssue> _issues =
            new List<DCMLValidationIssue>();

        /// <summary>
        /// Gets whether validation completed without any issues.
        /// </summary>
        public bool IsValid => _issues.Count == 0;

        /// <summary>
        /// Gets the validation issues that were found.
        /// </summary>
        public IReadOnlyList<DCMLValidationIssue> Issues => _issues;

        /// <summary>
        /// Adds a validation issue.
        /// </summary>
        public void Add(
            string code,
            string message
        )
        {
            _issues.Add(
                new DCMLValidationIssue(
                    code,
                    message
                )
            );
        }
    }
}
