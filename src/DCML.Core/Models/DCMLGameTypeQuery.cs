using System;

namespace DCML.Core.Models;

public sealed class DCMLGameTypeQuery
{
    public const int DefaultMaxResults =
        1024;

    public const int MaximumMaxResults =
        16384;

    public DCMLGameTypeQuery(
        string? fullNameStartsWith = null,
        string? nameContains = null,
        string? assemblyName = null,
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

        FullNameStartsWith =
            Normalize(
                fullNameStartsWith);

        NameContains =
            Normalize(
                nameContains);

        AssemblyName =
            Normalize(
                assemblyName);

        MaxResults =
            maxResults;
    }

    public string FullNameStartsWith { get; }

    public string NameContains { get; }

    public string AssemblyName { get; }

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
