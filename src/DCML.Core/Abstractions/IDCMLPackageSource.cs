using DCML.Core.Models;

namespace DCML.Core.Abstractions;

/// <summary>
/// Describes a host-neutral source that can discover package entries without
/// staging, installing, updating, or otherwise mutating package state.
/// </summary>
public interface IDCMLPackageSource
{
    /// <summary>
    /// Gets the stable source identity and advertised capabilities.
    /// </summary>
    DCMLPackageSourceDescriptor Descriptor { get; }

    /// <summary>
    /// Performs read-only discovery of source-specific package entries.
    /// </summary>
    DCMLPackageSourceDiscoveryResult DiscoverPackages();
}
