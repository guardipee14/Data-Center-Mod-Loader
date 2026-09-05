using System;

namespace DCML.Core.Models;

/// <summary>
/// Describes a package-source discovery problem without requiring callers to
/// expose filesystem paths, platform credentials, or provider-specific
/// exception objects.
/// </summary>
public sealed class DCMLPackageSourceIssue
{
    public DCMLPackageSourceIssue(
        string sourceId,
        string? packageKey,
        string code,
        string message)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            throw new ArgumentException(
                "Package source ID cannot be empty.",
                nameof(sourceId));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Package source issue code cannot be empty.",
                nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "Package source issue message cannot be empty.",
                nameof(message));
        }

        SourceId = sourceId.Trim();
        PackageKey = string.IsNullOrWhiteSpace(packageKey) ? null : packageKey.Trim();
        Code = code.Trim();
        Message = message.Trim();
    }

    public string SourceId { get; }

    public string? PackageKey { get; }

    public string Code { get; }

    public string Message { get; }
}
