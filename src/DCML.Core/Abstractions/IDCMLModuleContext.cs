using System;

namespace DCML.Core.Abstractions
{
    /// <summary>
    /// Provides a DCML module with information and services
    /// supplied by its current host.
    /// </summary>
    public interface IDCMLModuleContext
    {
        /// <summary>
        /// Gets the directory containing the module package.
        /// </summary>
        string ModuleDirectory { get; }

        /// <summary>
        /// Gets the directory where the module may store
        /// persistent module-specific data.
        /// </summary>
        string DataDirectory { get; }

        /// <summary>
        /// Gets services exposed to the module by DCML.
        /// </summary>
        IServiceProvider Services { get; }
    }
}
