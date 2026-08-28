namespace DCML.Core.Models
{
    /// <summary>
    /// Describes one problem that prevents a DCML module from loading.
    /// </summary>
    public sealed class DCMLDependencyResolutionIssue
    {
        public DCMLDependencyResolutionIssue(
            string moduleId,
            string code,
            string message,
            string? dependencyId = null
        )
        {
            ModuleId = moduleId;
            Code = code;
            Message = message;
            DependencyId = dependencyId;
        }

        /// <summary>
        /// Gets the module that is blocked.
        /// </summary>
        public string ModuleId { get; }

        /// <summary>
        /// Gets the stable resolution issue code.
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// Gets the human-readable resolution message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the related dependency identifier, when applicable.
        /// </summary>
        public string? DependencyId { get; }
    }
}
