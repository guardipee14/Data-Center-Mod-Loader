using System;
using DCML.Core.Models;

namespace DCML.DataCenter.Models;

public sealed class DataCenterComponentCatalogQuery
{
    public const int DefaultMaxObjects =
        DCMLGameObjectQuery.MaximumMaxResults;

    public const int DefaultMaxExamplesPerType =
        8;

    public const int MaximumMaxExamplesPerType =
        64;

    public DataCenterComponentCatalogQuery(
        string? sceneName = null,
        string? typeNamePrefix = null,
        bool includeInactive = true,
        int maxObjects = DefaultMaxObjects,
        int maxExamplesPerType =
            DefaultMaxExamplesPerType)
    {
        if (
            maxObjects <= 0 ||
            maxObjects >
                DCMLGameObjectQuery.MaximumMaxResults
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxObjects),
                maxObjects,
                $"Max objects must be between 1 and {DCMLGameObjectQuery.MaximumMaxResults}.");
        }

        if (
            maxExamplesPerType <= 0 ||
            maxExamplesPerType >
                MaximumMaxExamplesPerType
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxExamplesPerType),
                maxExamplesPerType,
                $"Max examples per type must be between 1 and {MaximumMaxExamplesPerType}.");
        }

        SceneName =
            Normalize(
                sceneName);

        TypeNamePrefix =
            Normalize(
                typeNamePrefix);

        IncludeInactive =
            includeInactive;

        MaxObjects =
            maxObjects;

        MaxExamplesPerType =
            maxExamplesPerType;
    }

    public string SceneName { get; }

    public string TypeNamePrefix { get; }

    public bool IncludeInactive { get; }

    public int MaxObjects { get; }

    public int MaxExamplesPerType { get; }

    private static string Normalize(
        string? value)
    {
        return
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
    }
}
