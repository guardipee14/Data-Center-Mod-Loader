using System;
using System.Collections.Generic;
using System.Linq;

namespace DCML.Core.Models;

public sealed class DCMLGameObjectInfo
{
    private readonly IReadOnlyList<string>
        _componentTypeNames;

    public DCMLGameObjectInfo(
        int instanceId,
        string? name,
        string? sceneName,
        string? hierarchyPath,
        bool activeInHierarchy,
        IEnumerable<string>? componentTypeNames)
    {
        InstanceId =
            instanceId;

        Name =
            name ??
            string.Empty;

        SceneName =
            sceneName ??
            string.Empty;

        HierarchyPath =
            hierarchyPath ??
            string.Empty;

        ActiveInHierarchy =
            activeInHierarchy;

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

    public string SceneName { get; }

    public string HierarchyPath { get; }

    public bool ActiveInHierarchy { get; }

    public IReadOnlyList<string> ComponentTypeNames =>
        _componentTypeNames;
}
