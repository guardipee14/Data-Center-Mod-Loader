using System;

namespace DCML.Core.Models;

public sealed class DCMLGameObjectQuery
{
    public const int DefaultMaxResults =
        256;

    public const int MaximumMaxResults =
        4096;

    public DCMLGameObjectQuery(
        string? nameContains = null,
        string? sceneName = null,
        string? componentTypeName = null,
        bool includeInactive = true,
        int maxResults = DefaultMaxResults)
    {
        if (
            maxResults <= 0 ||
            maxResults > MaximumMaxResults
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxResults),
                maxResults,
                $"Max results must be between 1 and {MaximumMaxResults}.");
        }

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

        MaxResults =
            maxResults;
    }

    public string NameContains { get; }

    public string SceneName { get; }

    public string ComponentTypeName { get; }

    public bool IncludeInactive { get; }

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
