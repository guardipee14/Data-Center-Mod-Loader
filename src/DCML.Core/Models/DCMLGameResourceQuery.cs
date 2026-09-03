using System;

namespace DCML.Core.Models;

public sealed class DCMLGameResourceQuery
{
    public const int DefaultMaxResults =
        256;

    public const int MaximumMaxResults =
        16384;

    public DCMLGameResourceQuery(
        string? nameContains = null,
        string? componentTypeName = null,
        string? componentTypeNamePrefix = null,
        int maxResults = DefaultMaxResults,
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

        ComponentTypeName =
            Normalize(
                componentTypeName);

        ComponentTypeNamePrefix =
            Normalize(
                componentTypeNamePrefix);

        MaxResults =
            maxResults;

        SkipResults =
            skipResults;
    }

    public string NameContains { get; }

    public string ComponentTypeName { get; }

    public string ComponentTypeNamePrefix { get; }

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
