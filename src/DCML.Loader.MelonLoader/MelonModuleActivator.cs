using System;
using System.IO;
using System.Reflection;
using DCML.Core.Abstractions;
using DCML.Core.Models;

namespace DCML.Loader.MelonLoader
{
    /// <summary>
    /// Creates DCML module instances inside MelonLoader's managed
    /// runtime.
    /// </summary>
    public sealed class MelonModuleActivator :
        IDCMLModuleActivator
    {
        public IDCMLModule Create(
            DCMLModulePackage package
        )
        {
            if (package == null)
            {
                throw new ArgumentNullException(
                    nameof(package)
                );
            }

            string packageRoot =
                Path.GetFullPath(
                    package.PackageDirectory
                );

            string assemblyPath =
                Path.GetFullPath(
                    Path.Combine(
                        packageRoot,
                        package.Manifest.EntryAssembly
                    )
                );

            string packageRootWithSeparator =
                packageRoot.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar
                ) +
                Path.DirectorySeparatorChar;

            if (
                !assemblyPath.StartsWith(
                    packageRootWithSeparator,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                throw new InvalidOperationException(
                    "Entry assembly resolves outside the module package."
                );
            }

            if (
                !File.Exists(
                    assemblyPath
                )
            )
            {
                throw new FileNotFoundException(
                    "DCML module entry assembly was not found.",
                    assemblyPath
                );
            }

            Assembly assembly =
                Assembly.LoadFrom(
                    assemblyPath
                );

            Type entryType =
                assembly.GetType(
                    package.Manifest.EntryType,
                    false,
                    false
                );

            if (entryType == null)
            {
                throw new TypeLoadException(
                    "DCML entry type '" +
                    package.Manifest.EntryType +
                    "' was not found in '" +
                    package.Manifest.EntryAssembly +
                    "'."
                );
            }

            if (
                entryType.IsAbstract ||
                entryType.IsInterface ||
                !typeof(IDCMLModule).IsAssignableFrom(
                    entryType
                )
            )
            {
                throw new InvalidOperationException(
                    "DCML entry type '" +
                    package.Manifest.EntryType +
                    "' does not implement IDCMLModule."
                );
            }

            object instance =
                Activator.CreateInstance(
                    entryType
                );

            IDCMLModule module =
                instance as IDCMLModule;

            if (module == null)
            {
                throw new InvalidOperationException(
                    "DCML could not create module entry type '" +
                    package.Manifest.EntryType +
                    "'."
                );
            }

            return module;
        }
    }
}
