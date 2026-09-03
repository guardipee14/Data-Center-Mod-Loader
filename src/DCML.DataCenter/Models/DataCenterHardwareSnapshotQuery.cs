using System;

namespace DCML.DataCenter.Models;

public sealed class DataCenterHardwareSnapshotQuery
{
    public const int DefaultMaxPerType = 64;
    public const int MaximumMaxPerType = 1024;

    public DataCenterHardwareSnapshotQuery(
        string? sceneName = null,
        bool includeSceneObjects = true,
        bool includeResources = true,
        int maxPerType = DefaultMaxPerType)
    {
        if (!includeSceneObjects && !includeResources)
        {
            throw new ArgumentException(
                "At least one hardware snapshot source must be enabled.");
        }

        if (maxPerType <= 0 || maxPerType > MaximumMaxPerType)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPerType));
        }

        SceneName = string.IsNullOrWhiteSpace(sceneName)
            ? string.Empty
            : sceneName.Trim();

        IncludeSceneObjects = includeSceneObjects;
        IncludeResources = includeResources;
        MaxPerType = maxPerType;
    }

    public string SceneName { get; }

    public bool IncludeSceneObjects { get; }

    public bool IncludeResources { get; }

    public int MaxPerType { get; }
}
