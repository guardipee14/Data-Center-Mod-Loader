using System;
using System.Collections.Generic;
using System.Linq;

namespace DCML.DataCenter.Models;

public sealed class DataCenterComponentCatalogSnapshot
{
    private readonly IReadOnlyList<DataCenterComponentTypeInfo>
        _componentTypes;

    public DataCenterComponentCatalogSnapshot(
        string? sceneName,
        int scannedObjectCount,
        IEnumerable<DataCenterComponentTypeInfo>? componentTypes)
    {
        if (scannedObjectCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(scannedObjectCount));
        }

        SceneName =
            sceneName?.Trim() ??
            string.Empty;

        ScannedObjectCount =
            scannedObjectCount;

        _componentTypes =
            componentTypes is null
                ? Array.Empty<DataCenterComponentTypeInfo>()
                : componentTypes
                    .Where(
                        value =>
                            value is not null)
                    .OrderBy(
                        value =>
                            value.TypeName,
                        StringComparer.Ordinal)
                    .ToArray();

        Il2CppTypeCount =
            _componentTypes.Count(
                value =>
                    value.IsIl2Cpp);

        UnityEngineTypeCount =
            _componentTypes.Count(
                value =>
                    value.IsUnityEngine);
    }

    public string SceneName { get; }

    public int ScannedObjectCount { get; }

    public IReadOnlyList<DataCenterComponentTypeInfo> ComponentTypes =>
        _componentTypes;

    public int UniqueComponentTypeCount =>
        _componentTypes.Count;

    public int Il2CppTypeCount { get; }

    public int UnityEngineTypeCount { get; }
}
