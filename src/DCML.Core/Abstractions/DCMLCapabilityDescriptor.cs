using System;
using DCML.Core.Runtime;

namespace DCML.Core.Abstractions;

public sealed class DCMLCapabilityDescriptor
{
    public DCMLCapabilityDescriptor(
        string id,
        string version)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException(
                "Capability ID cannot be empty.",
                nameof(id));
        }

        string normalizedVersion =
            version?.Trim()
            ?? string.Empty;

        if (!DCMLSemanticVersion.IsValid(normalizedVersion))
        {
            throw new ArgumentException(
                "Capability version must be a valid Semantic Versioning 2.0.0 value.",
                nameof(version));
        }

        Id =
            id.Trim();

        Version =
            normalizedVersion;
    }

    public string Id { get; }

    public string Version { get; }
}
