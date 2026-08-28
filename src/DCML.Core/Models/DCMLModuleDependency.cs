namespace DCML.Core.Models
{
    /// <summary>
    /// Describes another DCML module that a module depends on.
    /// </summary>
    public sealed class DCMLModuleDependency
    {
        /// <summary>
        /// Gets or sets the stable identifier of the dependency.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the minimum acceptable dependency version.
        /// A null or empty value means no minimum version is required.
        /// </summary>
        public string? MinimumVersion { get; set; }

        /// <summary>
        /// Gets or sets whether this dependency is optional.
        /// </summary>
        public bool Optional { get; set; }
    }
}
