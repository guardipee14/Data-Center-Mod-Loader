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

    public const int DefaultMaxPages =
        16;

    public const int MaximumMaxPages =
        256;

    public DataCenterComponentCatalogQuery(
        string? sceneName = null,
        string? typeNamePrefix = null,
        bool includeInactive = true,
        int maxObjects = DefaultMaxObjects,
        int maxExamplesPerType =
            DefaultMaxExamplesPerType,
        bool scanAllPages = false,
        int maxPages = DefaultMaxPages)
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

        if (
            maxPages <= 0 ||
            maxPages >
                MaximumMaxPages
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxPages),
                maxPages,
                $"Max pages must be between 1 and {MaximumMaxPages}.");
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

        ScanAllPages =
            scanAllPages;

        MaxPages =
            maxPages;
    }

    public string SceneName { get; }

    public string TypeNamePrefix { get; }

    public bool IncludeInactive { get; }

    public int MaxObjects { get; }

    public int MaxExamplesPerType { get; }

    public bool ScanAllPages { get; }

    public int MaxPages { get; }

    private static string Normalize(
        string? value)
    {
        return
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
    }
}
