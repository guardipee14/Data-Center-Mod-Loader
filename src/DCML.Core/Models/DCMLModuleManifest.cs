using System.Collections.Generic;

namespace DCML.Core.Models
{
    /// <summary>
    /// Describes a DCML module package.
    /// </summary>
    public sealed class DCMLModuleManifest
    {
        /// <summary>
        /// Gets or sets the DCML manifest schema version.
        /// </summary>
        public int SchemaVersion { get; set; } =
            DCMLManifestSchema.CurrentVersion;

        /// <summary>
        /// Gets or sets the stable module identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the human-readable module name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the module version.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional module description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the optional module author or publisher.
        /// </summary>
        public string? Author { get; set; }

        /// <summary>
        /// Gets or sets the assembly file containing the module entry point.
        /// </summary>
        public string EntryAssembly { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the fully-qualified type implementing IDCMLModule.
        /// </summary>
        public string EntryType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional minimum DCML version required
        /// by this module.
        /// </summary>
        public string? MinimumDCMLVersion { get; set; }

        /// <summary>
        /// Gets or sets the runtime capabilities that must be present
        /// before this package may be activated.
        /// </summary>
        public IList<DCMLCapabilityRequirement> RequiredCapabilities { get; set; } =
            new List<DCMLCapabilityRequirement>();

        /// <summary>
        /// Gets or sets whether installing or updating this module
        /// requires the host application to restart.
        /// </summary>
        public bool RequiresRestart { get; set; }

        /// <summary>
        /// Gets or sets the dependencies declared by this module.
        /// </summary>
        public IList<DCMLModuleDependency> Dependencies { get; set; } =
            new List<DCMLModuleDependency>();
    }
}
