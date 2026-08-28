using System;
using DCML.Core.Models;

namespace DCML.DataCenter.Models;

public sealed class DataCenterEntityQuery
{
    public DataCenterEntityQuery(
        string? kind = null,
        string? nameContains = null,
        string? sceneName = null,
        string? componentTypeName = null,
        bool includeInactive = true,
        bool includeUnknown = true,
        int maxResults =
            DCMLGameObjectQuery.DefaultMaxResults)
    {
        if (
            maxResults <= 0 ||
            maxResults >
                DCMLGameObjectQuery.MaximumMaxResults
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxResults),
                maxResults,
                $"Max results must be between 1 and {DCMLGameObjectQuery.MaximumMaxResults}.");
        }

        Kind =
            Normalize(
                kind);

        NameContains =
            Normalize(
                nameContains);

        SceneName =
            Normalize(
                sceneName);

        ComponentTypeName =
            Normalize(
                componentTypeName);

        IncludeInactive =
            includeInactive;

        IncludeUnknown =
            includeUnknown;

        MaxResults =
            maxResults;
    }

    public string Kind { get; }

    public string NameContains { get; }

    public string SceneName { get; }

    public string ComponentTypeName { get; }

    public bool IncludeInactive { get; }

    public bool IncludeUnknown { get; }

    public int MaxResults { get; }

    private static string Normalize(
        string? value)
    {
        return
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
    }
}
