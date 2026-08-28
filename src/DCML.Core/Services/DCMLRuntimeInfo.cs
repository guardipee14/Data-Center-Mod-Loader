using System;
using System.Collections.Generic;
using DCML.Core.Abstractions;

namespace DCML.Core.Services;

public sealed class DCMLRuntimeInfo : IDCMLRuntimeInfo
{
    private readonly HashSet<string> _capabilitySet;

    private readonly IReadOnlyCollection<string> _capabilities;

    public DCMLRuntimeInfo(
        string moduleId,
        string dcmlVersion,
        string hostName,
        string hostVersion,
        string gameName,
        string gameRoot,
        IEnumerable<string> capabilities)
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

        var orderedCapabilities =
            new List<string>();

        foreach (var capability in capabilities)
        {
            if (string.IsNullOrWhiteSpace(capability))
            {
                throw new ArgumentException(
                    "Runtime capabilities cannot contain an empty value.",
                    nameof(capabilities));
            }

            var normalized =
                capability.Trim();

            if (_capabilitySet.Add(normalized))
            {
                orderedCapabilities.Add(
                    normalized);
            }
        }

        _capabilities =
            orderedCapabilities.AsReadOnly();
    }

    public string ModuleId { get; }

    public string DCMLVersion { get; }

    public string HostName { get; }

    public string HostVersion { get; }

    public string GameName { get; }

    public string GameRoot { get; }

    public IReadOnlyCollection<string> Capabilities =>
        _capabilities;

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
