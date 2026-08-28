using System;

namespace DCML.Core.Models;

public sealed class DCMLGameObjectQuery
{
    public const int DefaultMaxResults =
        256;

    public const int MaximumMaxResults =
        16384;

    public DCMLGameObjectQuery(
        string? nameContains = null,
        string? sceneName = null,
        string? componentTypeName = null,
        bool includeInactive = true,
        int maxResults = DefaultMaxResults,
        string? componentTypeNamePrefix = null,
        int skipResults = 0)
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

        if (skipResults < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skipResults),
                skipResults,
                "Skip results cannot be negative.");
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

        ComponentTypeNamePrefix =
            Normalize(
                componentTypeNamePrefix);

        IncludeInactive =
            includeInactive;

        MaxResults =
            maxResults;

        SkipResults =
            skipResults;
    }

    public string NameContains { get; }

    public string SceneName { get; }

    public string ComponentTypeName { get; }

    public string ComponentTypeNamePrefix { get; }

    public bool IncludeInactive { get; }

    public int MaxResults { get; }

    public int SkipResults { get; }

    private static string Normalize(
        string? value)
    {
        return
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
    }
}
