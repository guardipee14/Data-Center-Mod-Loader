using System;

namespace DCML.Core.Models
{
    /// <summary>
    /// Represents one validated DCML module package discovered on disk.
    /// </summary>
    public sealed class DCMLModulePackage
    {
        public DCMLModulePackage(
            string packageDirectory,
            string manifestPath,
            DCMLModuleManifest manifest
        )
        {
            PackageDirectory =
                packageDirectory ??
                throw new ArgumentNullException(
                    nameof(packageDirectory)
                );

            ManifestPath =
                manifestPath ??
                throw new ArgumentNullException(
                    nameof(manifestPath)
                );

            Manifest =
                manifest ??
                throw new ArgumentNullException(
                    nameof(manifest)
                );
        }

        /// <summary>
        /// Gets the package directory.
        /// </summary>
        public string PackageDirectory { get; }

        /// <summary>
        /// Gets the manifest path.
        /// </summary>
        public string ManifestPath { get; }

        /// <summary>
        /// Gets the validated module manifest.
        /// </summary>
        public DCMLModuleManifest Manifest { get; }
    }
}
