using DCML.Core.Models;

namespace DCML.Core.Abstractions
{
    /// <summary>
    /// Creates the host-independent context supplied to a DCML module.
    /// </summary>
    public interface IDCMLModuleContextFactory
    {
        /// <summary>
        /// Creates the context for a module package.
        /// </summary>
        IDCMLModuleContext CreateContext(
            DCMLModulePackage package
        );
    }
}
