using System.Collections.Generic;
using DCML.Core.Abstractions;

namespace DCML.Loader.MelonLoader;

internal static class MelonHostCapabilities
{
    public static IReadOnlyCollection<DCMLCapabilityDescriptor> Create()
    {
        return
            new[]
            {
                V1(DCMLRuntimeCapabilities.Logging),
                V1(DCMLRuntimeCapabilities.RuntimeInformation),
                V1(DCMLRuntimeCapabilities.RuntimeCapabilities),
                V1(DCMLRuntimeCapabilities.Configuration),
                V1(DCMLRuntimeCapabilities.Events),
                V1(DCMLRuntimeCapabilities.GameSceneLifecycle),
                V1(DCMLRuntimeCapabilities.GameObjectDiscovery),
                V1(DCMLRuntimeCapabilities.GameTypeCatalog),
                V1(DCMLRuntimeCapabilities.GameResourceDiscovery),
                V1(DCMLRuntimeCapabilities.GameTypeInspection),
                V1(DCMLRuntimeCapabilities.GameMainThread),
                V1(DCMLRuntimeCapabilities.GameComponentState)
            };
    }

    private static DCMLCapabilityDescriptor V1(
        string capability)
    {
        return
            new DCMLCapabilityDescriptor(
                capability,
                DCMLCapabilityVersions.V1);
    }
}
