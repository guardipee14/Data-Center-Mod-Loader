using System.Collections.Generic;

namespace DCML.Core.Abstractions;

public interface IDCMLCapabilityCatalog
{
    IReadOnlyCollection<DCMLCapabilityDescriptor> CapabilityDescriptors { get; }

    bool HasCapability(string capability);

    bool TryGetCapabilityVersion(
        string capability,
        out string? version);

    bool SupportsCapability(
        string capability,
        string minimumVersion);
}
