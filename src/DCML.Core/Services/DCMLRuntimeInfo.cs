using System;
using System.Collections.Generic;
using DCML.Core.Abstractions;
using DCML.Core.Runtime;

namespace DCML.Core.Services;

public sealed class DCMLRuntimeInfo :
    IDCMLRuntimeInfo,
    IDCMLCapabilityCatalog
{
    private readonly HashSet<string> _capabilitySet;

    private readonly IReadOnlyCollection<string> _capabilities;

    private readonly IReadOnlyCollection<DCMLCapabilityDescriptor> _capabilityDescriptors;

    private readonly Dictionary<string, DCMLCapabilityDescriptor> _capabilityMap;

    public DCMLRuntimeInfo(
        string moduleId,
        string dcmlVersion,
        string hostName,
        string hostVersion,
        string gameName,
        string gameRoot,
        IEnumerable<string> capabilities)
        : this(
            moduleId,
            dcmlVersion,
            hostName,
            hostVersion,
            gameName,
            gameRoot,
            CreateVersionedCapabilities(capabilities))
    {
    }

    public DCMLRuntimeInfo(
        string moduleId,
        string dcmlVersion,
        string hostName,
        string hostVersion,
        string gameName,
        string gameRoot,
        IEnumerable<DCMLCapabilityDescriptor> capabilities)
    {
        ModuleId =
            RequireValue(
                moduleId,
                nameof(moduleId));

        DCMLVersion =
            RequireValue(
                dcmlVersion,
                nameof(dcmlVersion));

        HostName =
            RequireValue(
                hostName,
                nameof(hostName));

        HostVersion =
            RequireValue(
                hostVersion,
                nameof(hostVersion));

        GameName =
            RequireValue(
                gameName,
                nameof(gameName));

        GameRoot =
            RequireValue(
                gameRoot,
                nameof(gameRoot));

        if (capabilities is null)
        {
            throw new ArgumentNullException(
                nameof(capabilities));
        }

        _capabilitySet =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        _capabilityMap =
            new Dictionary<string, DCMLCapabilityDescriptor>(
                StringComparer.OrdinalIgnoreCase);

        var orderedCapabilities =
            new List<string>();

        var orderedDescriptors =
            new List<DCMLCapabilityDescriptor>();

        foreach (var descriptor in capabilities)
        {
            if (descriptor is null)
            {
                throw new ArgumentException(
                    "Runtime capabilities cannot contain a null descriptor.",
                    nameof(capabilities));
            }

            if (
                _capabilityMap.TryGetValue(
                    descriptor.Id,
                    out var existing))
            {
                if (
                    !string.Equals(
                        existing.Version,
                        descriptor.Version,
                        StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        $"Capability '{descriptor.Id}' was registered with conflicting versions '{existing.Version}' and '{descriptor.Version}'.",
                        nameof(capabilities));
                }

                continue;
            }

            _capabilitySet.Add(
                descriptor.Id);

            _capabilityMap.Add(
                descriptor.Id,
                descriptor);

            orderedCapabilities.Add(
                descriptor.Id);

            orderedDescriptors.Add(
                descriptor);
        }

        _capabilities =
            orderedCapabilities.AsReadOnly();

        _capabilityDescriptors =
            orderedDescriptors.AsReadOnly();
    }

    public string ModuleId { get; }

    public string DCMLVersion { get; }

    public string HostName { get; }

    public string HostVersion { get; }

    public string GameName { get; }

    public string GameRoot { get; }

    public IReadOnlyCollection<string> Capabilities =>
        _capabilities;

    public IReadOnlyCollection<DCMLCapabilityDescriptor> CapabilityDescriptors =>
        _capabilityDescriptors;

    public bool HasCapability(
        string capability)
    {
        if (string.IsNullOrWhiteSpace(capability))
        {
            return false;
        }

        return
            _capabilitySet.Contains(
                capability.Trim());
    }

    public bool TryGetCapabilityVersion(
        string capability,
        out string? version)
    {
        version =
            null;

        if (string.IsNullOrWhiteSpace(capability))
        {
            return false;
        }

        if (
            !_capabilityMap.TryGetValue(
                capability.Trim(),
                out var descriptor))
        {
            return false;
        }

        version =
            descriptor.Version;

        return true;
    }

    public bool SupportsCapability(
        string capability,
        string minimumVersion)
    {
        if (
            !TryGetCapabilityVersion(
                capability,
                out string? availableVersion))
        {
            return false;
        }

        if (
            !DCMLSemanticVersion.TryCompare(
                availableVersion,
                minimumVersion,
                out int comparison))
        {
            return false;
        }

        return
            comparison >= 0;
    }

    private static IReadOnlyCollection<DCMLCapabilityDescriptor> CreateVersionedCapabilities(
        IEnumerable<string> capabilities)
    {
        if (capabilities is null)
        {
            throw new ArgumentNullException(
                nameof(capabilities));
        }

        var descriptors =
            new List<DCMLCapabilityDescriptor>();

        foreach (string capability in capabilities)
        {
            if (string.IsNullOrWhiteSpace(capability))
            {
                throw new ArgumentException(
                    "Runtime capabilities cannot contain an empty value.",
                    nameof(capabilities));
            }

            descriptors.Add(
                new DCMLCapabilityDescriptor(
                    capability,
                    DCMLCapabilityVersions.V1));
        }

        return
            descriptors.AsReadOnly();
    }

    private static string RequireValue(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{parameterName} cannot be empty.",
                parameterName);
        }

        return value.Trim();
    }
}
