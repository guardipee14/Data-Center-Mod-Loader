using System;

namespace DCML.Core.Models;

/// <summary>
/// Describes the result of staging one package-source entry.
/// </summary>
public sealed class DCMLPackageStageResult
{
    private DCMLPackageStageResult(
        string sourceId,
        string packageKey,
        bool success,
        string? stagedPackageDirectory,
        string? errorCode,
        string? errorMessage)
    {
        SourceId =
            sourceId;

        PackageKey =
            packageKey;

        Success =
            success;

        StagedPackageDirectory =
            stagedPackageDirectory;

        ErrorCode =
            errorCode;

        ErrorMessage =
            errorMessage;
    }

    public string SourceId { get; }

    public string PackageKey { get; }

    public bool Success { get; }

    public string? StagedPackageDirectory { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public static DCMLPackageStageResult Succeeded(
        string sourceId,
        string packageKey,
        string stagedPackageDirectory)
    {
        if (string.IsNullOrWhiteSpace(stagedPackageDirectory))
        {
            throw new ArgumentException(
                "A staged package directory is required.",
                nameof(stagedPackageDirectory));
        }

        return
            new DCMLPackageStageResult(
                RequireValue(
                    sourceId,
                    nameof(sourceId)),
                RequireValue(
                    packageKey,
                    nameof(packageKey)),
                true,
                stagedPackageDirectory,
                null,
                null);
    }

    public static DCMLPackageStageResult Failed(
        string sourceId,
        string packageKey,
        string errorCode,
        string errorMessage)
    {
        return
            new DCMLPackageStageResult(
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
