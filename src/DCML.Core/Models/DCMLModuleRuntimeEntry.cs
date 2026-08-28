namespace DCML.Core.Models
{
    /// <summary>
    /// Describes the final lifecycle state of one module for a
    /// runtime operation.
    /// </summary>
    public sealed class DCMLModuleRuntimeEntry
    {
        public DCMLModuleRuntimeEntry(
            DCMLModulePackage package,
            DCMLModuleRuntimeState state
        )
        {
            Package = package;
            State = state;
        }

        /// <summary>
        /// Gets the package associated with this runtime entry.
        /// </summary>
        public DCMLModulePackage Package { get; }

        /// <summary>
        /// Gets the module identifier.
        /// </summary>
        public string ModuleId =>
            Package.Manifest.Id;

        /// <summary>
        /// Gets the lifecycle state captured by this result.
        /// </summary>
        public DCMLModuleRuntimeState State { get; }
    }
}
