using System;
using System.Collections.Generic;
using System.Linq;

namespace DCML.Core.Models;

public sealed class DCMLGameResourceInfo
{
    private readonly IReadOnlyList<string>
        _componentTypeNames;

    public DCMLGameResourceInfo(
        int instanceId,
        string? name,
        IEnumerable<string>? componentTypeNames)
    {
        InstanceId =
            instanceId;

        Name =
            name ??
            string.Empty;

        _componentTypeNames =
            componentTypeNames is null
                ? Array.Empty<string>()
                : componentTypeNames
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(
                                value))
                    .Select(
                        value =>
                            value.Trim())
                    .Distinct(
                        StringComparer.Ordinal)
                    .OrderBy(
                        value =>
                            value,
                        StringComparer.Ordinal)
                    .ToArray();
    }

    public int InstanceId { get; }

    public string Name { get; }

    public IReadOnlyList<string> ComponentTypeNames =>
        _componentTypeNames;
}
