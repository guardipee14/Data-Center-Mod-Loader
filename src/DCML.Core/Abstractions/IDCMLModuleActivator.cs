using DCML.Core.Models;

namespace DCML.Core.Abstractions
{
    /// <summary>
    /// Creates a DCML module instance from a discovered package.
    /// Host adapters implement this contract so DCML.Core does not
    /// depend on a particular assembly-loading mechanism.
    /// </summary>
    public interface IDCMLModuleActivator
    {
        /// <summary>
        /// Creates the module instance represented by a package.
        /// </summary>
        IDCMLModule Create(
            DCMLModulePackage package
        );
    }
}
