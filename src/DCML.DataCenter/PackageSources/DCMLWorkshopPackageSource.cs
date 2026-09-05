using System;
using System.Collections.Generic;
using System.IO;
using DCML.Core.Abstractions;
using DCML.Core.Models;

namespace DCML.DataCenter.PackageSources;

/// <summary>
/// Discovers and stages Data Center Workshop items that Steam has already
/// materialized on disk. This adapter never subscribes, downloads, launches
/// Steam, or performs network access.
/// </summary>
public sealed class DCMLWorkshopPackageSource :
    IDCMLPackageStagingSource
{
    public const string DataCenterSteamAppId =
        "4170200";

    public const string SourceId =
        "datacenter.steam-workshop";

    private readonly string _workshopContentRoot;

    public DCMLWorkshopPackageSource(
        string workshopContentRoot)
    {
        if (string.IsNullOrWhiteSpace(workshopContentRoot))
        {
            throw new ArgumentException(
                "A Workshop content root is required.",
                nameof(workshopContentRoot));
        }

        _workshopContentRoot =
            Path.GetFullPath(
                workshopContentRoot);

        Descriptor =
            new DCMLPackageSourceDescriptor(
                SourceId,
                "Data Center Steam Workshop",
                "steam-workshop",
                DCMLPackageSourceCapabilities.Discovery |
                DCMLPackageSourceCapabilities.Staging);
    }

    public DCMLPackageSourceDescriptor Descriptor { get; }

    public string WorkshopContentRoot =>
        _workshopContentRoot;

    public DCMLPackageSourceDiscoveryResult DiscoverPackages()
    {
        var entries =
            new List<DCMLPackageSourceEntry>();

        var issues =
            new List<DCMLPackageSourceIssue>();

        if (!Directory.Exists(_workshopContentRoot))
        {
            issues.Add(
                new DCMLPackageSourceIssue(
                    SourceId,
                    null,
                    "DCML_WORKSHOP_ROOT_NOT_FOUND",
                    "The configured Data Center Workshop content root was not found."));

            return
                new DCMLPackageSourceDiscoveryResult(
                    SourceId,
                    entries,
                    issues);
        }

        string[] directories;

        try
        {
            directories =
                Directory.GetDirectories(
                    _workshopContentRoot,
                    "*",
                    SearchOption.TopDirectoryOnly);
        }
        catch (Exception exception)
        {
            issues.Add(
                new DCMLPackageSourceIssue(
                    SourceId,
                    null,
                    "DCML_WORKSHOP_ROOT_READ_FAILED",
                    exception.Message));

            return
                new DCMLPackageSourceDiscoveryResult(
                    SourceId,
                    entries,
                    issues);
        }

        Array.Sort(
            directories,
            StringComparer.OrdinalIgnoreCase);

        foreach (string directory in directories)
        {
            string packageKey =
                Path.GetFileName(directory);

            if (!IsWorkshopItemId(packageKey))
            {
                issues.Add(
                    new DCMLPackageSourceIssue(
                        SourceId,
                        packageKey,
                        "DCML_WORKSHOP_ITEM_ID_INVALID",
                        "The Workshop item directory name is not a numeric item ID."));

                continue;
            }

            if (IsReparsePoint(directory))
            {
                issues.Add(
                    new DCMLPackageSourceIssue(
                        SourceId,
                        packageKey,
                        "DCML_WORKSHOP_ITEM_REPARSE_POINT",
                        "The Workshop item directory is a reparse point and was not accepted."));

                continue;
            }

            entries.Add(
                new DCMLPackageSourceEntry(
                    SourceId,
                    packageKey));
        }

        return
            new DCMLPackageSourceDiscoveryResult(
                SourceId,
                entries,
                issues);
    }

    public DCMLPackageStageResult StagePackage(
        DCMLPackageSourceEntry entry,
        string stagingRoot)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(
                nameof(entry));
        }

        if (string.IsNullOrWhiteSpace(stagingRoot))
        {
            throw new ArgumentException(
                "A staging root is required.",
                nameof(stagingRoot));
        }

        if (
            !string.Equals(
                entry.SourceId,
                SourceId,
                StringComparison.OrdinalIgnoreCase))
        {
            return
                DCMLPackageStageResult.Failed(
                    SourceId,
                    entry.PackageKey,
                    "DCML_WORKSHOP_SOURCE_MISMATCH",
                    "The package entry belongs to a different package source.");
        }

        if (!IsWorkshopItemId(entry.PackageKey))
        {
            return
                DCMLPackageStageResult.Failed(
                    SourceId,
                    entry.PackageKey,
                    "DCML_WORKSHOP_ITEM_ID_INVALID",
                    "The Workshop package key must be a numeric item ID.");
        }

        string sourceDirectory =
            Path.GetFullPath(
                Path.Combine(
                    _workshopContentRoot,
                    entry.PackageKey));

        if (!IsImmediateChild(
            _workshopContentRoot,
            sourceDirectory))
        {
            return
                DCMLPackageStageResult.Failed(
                    SourceId,
                    entry.PackageKey,
                    "DCML_WORKSHOP_SOURCE_PATH_INVALID",
                    "The Workshop item did not resolve beneath the configured content root.");
        }

        if (!Directory.Exists(sourceDirectory))
        {
            return
                DCMLPackageStageResult.Failed(
                    SourceId,
                    entry.PackageKey,
                    "DCML_WORKSHOP_ITEM_NOT_FOUND",
                    "The Workshop item is not currently available on disk.");
        }

        if (IsReparsePoint(sourceDirectory))
        {
            return
                DCMLPackageStageResult.Failed(
                    SourceId,
                    entry.PackageKey,
                    "DCML_WORKSHOP_ITEM_REPARSE_POINT",
                    "The Workshop item directory is a reparse point and cannot be staged.");
        }

        string normalizedStagingRoot =
            Path.GetFullPath(
                stagingRoot);

        Directory.CreateDirectory(
            normalizedStagingRoot);

        string destinationDirectory =
            Path.GetFullPath(
                Path.Combine(
                    normalizedStagingRoot,
                    entry.PackageKey));

        if (!IsImmediateChild(
            normalizedStagingRoot,
            destinationDirectory))
        {
            return
                DCMLPackageStageResult.Failed(
                    SourceId,
                    entry.PackageKey,
                    "DCML_WORKSHOP_STAGE_PATH_INVALID",
                    "The staging destination did not resolve beneath the staging root.");
        }

        if (
            Directory.Exists(destinationDirectory) ||
            File.Exists(destinationDirectory))
        {
            return
                DCMLPackageStageResult.Failed(
                    SourceId,
                    entry.PackageKey,
                    "DCML_WORKSHOP_STAGE_TARGET_EXISTS",
                    "The staging destination already exists and will not be overwritten.");
        }

        string temporaryDirectory =
            destinationDirectory +
            ".staging-" +
            Guid.NewGuid().ToString("N");

        try
        {
            CopyDirectory(
                sourceDirectory,
                temporaryDirectory);

            Directory.Move(
                temporaryDirectory,
                destinationDirectory);

            return
                DCMLPackageStageResult.Succeeded(
                    SourceId,
                    entry.PackageKey,
                    destinationDirectory);
        }
        catch (Exception exception)
        {
            TryDeleteDirectory(
                temporaryDirectory);

            return
                DCMLPackageStageResult.Failed(
                    SourceId,
                    entry.PackageKey,
                    "DCML_WORKSHOP_STAGE_COPY_FAILED",
                    exception.Message);
        }
    }

    private static bool IsWorkshopItemId(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (char character in value)
        {
            if (
                character < '0' ||
                character > '9')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsImmediateChild(
        string parent,
        string child)
    {
        string normalizedParent =
            Path.GetFullPath(parent)
                .TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);

        string? childParent =
            Directory.GetParent(
                Path.GetFullPath(child))
                ?.FullName
                .TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);

        return
            string.Equals(
                normalizedParent,
                childParent,
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsReparsePoint(
        string path)
    {
        return
            (File.GetAttributes(path) &
             FileAttributes.ReparsePoint) != 0;
    }

    private static void CopyDirectory(
        string sourceDirectory,
        string destinationDirectory)
    {
        if (IsReparsePoint(sourceDirectory))
        {
            throw new IOException(
                "Reparse-point directories cannot be staged.");
        }

        Directory.CreateDirectory(
            destinationDirectory);

        foreach (
            string file
            in Directory.GetFiles(
                sourceDirectory,
                "*",
                SearchOption.TopDirectoryOnly))
        {
            if (IsReparsePoint(file))
            {
                throw new IOException(
                    "Reparse-point files cannot be staged.");
            }

            File.Copy(
                file,
                Path.Combine(
                    destinationDirectory,
                    Path.GetFileName(file)),
                false);
        }

        foreach (
            string childDirectory
            in Directory.GetDirectories(
                sourceDirectory,
                "*",
                SearchOption.TopDirectoryOnly))
        {
            if (IsReparsePoint(childDirectory))
            {
                throw new IOException(
                    "Reparse-point directories cannot be staged.");
            }

            CopyDirectory(
                childDirectory,
                Path.Combine(
                    destinationDirectory,
                    Path.GetFileName(childDirectory)));
        }
    }

    private static void TryDeleteDirectory(
        string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(
                    path,
                    true);
            }
        }
        catch
        {
        }
    }
}
