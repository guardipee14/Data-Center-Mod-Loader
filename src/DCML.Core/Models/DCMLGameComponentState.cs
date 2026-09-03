using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DCML.Core.Models;

public sealed class DCMLGameComponentState
{
    private readonly IReadOnlyDictionary<string, DCMLGameValue> _values;

    public DCMLGameComponentState(
        int instanceId,
        string? name,
        string? sceneName,
        string? hierarchyPath,
        bool? activeInHierarchy,
        bool isResource,
        string componentTypeName,
        IEnumerable<KeyValuePair<string, DCMLGameValue>>? values,
        int? componentInstanceId = null)
    {
        if (string.IsNullOrWhiteSpace(componentTypeName))
        {
            throw new ArgumentException(
                "A component type name is required.",
                nameof(componentTypeName));
        }

        InstanceId = instanceId;
        GameObjectInstanceId = instanceId;
        ComponentInstanceId =
            componentInstanceId ??
            instanceId;
        Name = name ?? string.Empty;
        SceneName = sceneName ?? string.Empty;
        HierarchyPath = hierarchyPath ?? string.Empty;
        ActiveInHierarchy = activeInHierarchy;
        IsResource = isResource;
        ComponentTypeName = componentTypeName.Trim();

        var dictionary =
            new Dictionary<string, DCMLGameValue>(
                StringComparer.OrdinalIgnoreCase);

        if (values is not null)
        {
            foreach (KeyValuePair<string, DCMLGameValue> pair in values)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value is null)
                {
                    continue;
                }

                dictionary[pair.Key.Trim()] = pair.Value;
            }
        }

        _values = new ReadOnlyDictionary<string, DCMLGameValue>(dictionary);
    }

    // Backward-compatible GameObject identity.
    public int InstanceId { get; }

    public int GameObjectInstanceId { get; }

    // Identity of the matched component itself.
    public int ComponentInstanceId { get; }

    public string Name { get; }

    public string SceneName { get; }

    public string HierarchyPath { get; }

    public bool? ActiveInHierarchy { get; }

    public bool IsResource { get; }

    public string ComponentTypeName { get; }

    public IReadOnlyDictionary<string, DCMLGameValue> Values => _values;
}
