using System;

namespace DCML.Core.Models;

/// <summary>
/// Describes success or failure while retrieving source-provided package
/// update metadata.
/// </summary>
public sealed class DCMLPackageUpdateMetadataResult
{
    private DCMLPackageUpdateMetadataResult(
        string sourceId,
        string packageKey,
        bool success,
        DCMLPackageUpdateMetadata? metadata,
        string? errorCode,
        string? errorMessage)
    {
        SourceId =
            sourceId;

        PackageKey =
            packageKey;

        Success =
            success;

        Metadata =
            metadata;

        ErrorCode =
            errorCode;

        ErrorMessage =
            errorMessage;
    }

    public string SourceId { get; }

    public string PackageKey { get; }

    public bool Success { get; }

    public DCMLPackageUpdateMetadata? Metadata { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public static DCMLPackageUpdateMetadataResult Succeeded(
        DCMLPackageUpdateMetadata metadata)
    {
        if (metadata == null)
        {
            throw new ArgumentNullException(
                nameof(metadata));
        }

        return
            new DCMLPackageUpdateMetadataResult(
                metadata.SourceId,
                metadata.PackageKey,
                true,
                metadata,
                null,
                null);
    }

    public static DCMLPackageUpdateMetadataResult Failed(
        string sourceId,
        string packageKey,
        string errorCode,
        string errorMessage)
    {
        return
            new DCMLPackageUpdateMetadataResult(
                RequireValue(
                    sourceId,
                    nameof(sourceId)),
                RequireValue(
                    packageKey,
                    nameof(packageKey)),
                false,
                null,
                RequireValue(
                    errorCode,
                    nameof(errorCode)),
                RequireValue(
                    errorMessage,
                    nameof(errorMessage)));
    }

    private static string RequireValue(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A non-empty value is required.",
                parameterName);
        }

        return
            value.Trim();
    }
}
