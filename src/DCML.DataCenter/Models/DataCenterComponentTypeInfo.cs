using System;
using System.Collections.Generic;
using System.Linq;

namespace DCML.DataCenter.Models;

public sealed class DataCenterComponentTypeInfo
{
    private readonly IReadOnlyList<string>
        _exampleHierarchyPaths;

    public DataCenterComponentTypeInfo(
        string typeName,
        int objectCount,
        int activeObjectCount,
        int inactiveObjectCount,
        IEnumerable<string>? exampleHierarchyPaths)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            throw new ArgumentException(
                "A component type name is required.",
                nameof(typeName));
        }

        if (objectCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(objectCount));
        }

        if (activeObjectCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(activeObjectCount));
        }

        if (inactiveObjectCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inactiveObjectCount));
        }

        if (
            activeObjectCount +
            inactiveObjectCount !=
            objectCount
        )
        {
            throw new ArgumentException(
                "Active and inactive object counts must add up to the total object count.");
        }

        TypeName =
            typeName.Trim();

        ObjectCount =
            objectCount;

        ActiveObjectCount =
            activeObjectCount;

        InactiveObjectCount =
            inactiveObjectCount;

        _exampleHierarchyPaths =
            exampleHierarchyPaths is null
                ? Array.Empty<string>()
                : exampleHierarchyPaths
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(
                                value))
                    .Select(
                        value =>
                            value.Trim())
                    .Distinct(
                        StringComparer.Ordinal)
                    .ToArray();
    }

    public string TypeName { get; }

    public int ObjectCount { get; }

    public int ActiveObjectCount { get; }

    public int InactiveObjectCount { get; }

    public IReadOnlyList<string> ExampleHierarchyPaths =>
        _exampleHierarchyPaths;

    public bool IsIl2Cpp =>
        TypeName.StartsWith(
            "Il2Cpp.",
            StringComparison.Ordinal);

    public bool IsUnityEngine =>
        TypeName.StartsWith(
            "UnityEngine.",
            StringComparison.Ordinal);
}
