using DCML.Core.Abstractions;
using DCML.Core.Models;
using DCML.Core.Runtime;

namespace DCML.Loader.MelonLoader;

/// <summary>
/// MelonLoader host adapter for the host-neutral reflection activator.
/// </summary>
public sealed class MelonModuleActivator :
    IDCMLModuleActivator
{
    private readonly DCMLReflectionModuleActivator _inner =
        new DCMLReflectionModuleActivator();

    public IDCMLModule Create(
        DCMLModulePackage package)
    {
        return
            _inner.Create(
                package);
    }
}
