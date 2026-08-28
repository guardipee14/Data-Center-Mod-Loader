namespace DCML.Core.Models
{
    /// <summary>
    /// Describes one module lifecycle problem.
    /// </summary>
    public sealed class DCMLModuleRuntimeIssue
    {
        public DCMLModuleRuntimeIssue(
            string moduleId,
            string code,
            string message,
            string? dependencyId = null,
            string? exceptionType = null
        )
        {
            ModuleId = moduleId;
            Code = code;
            Message = message;
            DependencyId = dependencyId;
            ExceptionType = exceptionType;
        }

        /// <summary>
        /// Gets the affected module identifier.
        /// </summary>
        public string ModuleId { get; }

        /// <summary>
        /// Gets the stable runtime issue code.
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// Gets the human-readable issue message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the related dependency identifier, when applicable.
        /// </summary>
        public string? DependencyId { get; }

        /// <summary>
        /// Gets the exception type name, when the issue came from an exception.
        /// </summary>
        public string? ExceptionType { get; }
    }
}
