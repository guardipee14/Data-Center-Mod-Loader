using System;
using DCML.Core.Abstractions;

namespace DCML.Core.Models;

/// <summary>
/// Describes one package source without exposing source-specific runtime
/// dependencies through the core contract.
/// </summary>
public sealed class DCMLPackageSourceDescriptor
{
    public DCMLPackageSourceDescriptor(
        string id,
        string displayName,
        string sourceType,
        DCMLPackageSourceCapabilities capabilities)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException(
                "Package source ID cannot be empty.",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException(
                "Package source display name cannot be empty.",
                nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(sourceType))
        {
            throw new ArgumentException(
                "Package source type cannot be empty.",
                nameof(sourceType));
        }

        if ((capabilities & DCMLPackageSourceCapabilities.Discovery) ==
            DCMLPackageSourceCapabilities.None)
        {
            throw new ArgumentException(
                "A package source must advertise the Discovery capability.",
                nameof(capabilities));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        SourceType = sourceType.Trim();
        Capabilities = capabilities;
    }

    public string Id { get; }

    public string DisplayName { get; }

    /// <summary>
    /// Gets a source-defined type identifier such as "local-directory" or
    /// "workshop". Core does not interpret this value as authorization to
    /// access or mutate the underlying platform.
    /// </summary>
    public string SourceType { get; }

    public DCMLPackageSourceCapabilities Capabilities { get; }

    public bool HasCapability(DCMLPackageSourceCapabilities capability)
    {
        return (Capabilities & capability) == capability;
    }
}
