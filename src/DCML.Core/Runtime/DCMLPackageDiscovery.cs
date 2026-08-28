using System;
using System.Collections.Generic;
using System.IO;
using DCML.Core.Models;

namespace DCML.Core.Runtime
{
    /// <summary>
    /// Discovers and validates DCML module packages from a modules root.
    /// </summary>
    public static class DCMLPackageDiscovery
    {
        private const string ManifestFileName =
            "manifest.json";

        /// <summary>
        /// Discovers immediate child package directories beneath a
        /// modules root. One invalid package does not prevent other
        /// packages from being discovered.
        /// </summary>
        public static DCMLPackageDiscoveryResult Discover(
            string modulesRoot
        )
        {
            var result =
                new DCMLPackageDiscoveryResult();

            if (
                string.IsNullOrWhiteSpace(
                    modulesRoot
                )
            )
            {
                result.AddFailure(
                    new DCMLPackageDiscoveryFailure(
                        string.Empty,
                        null,
                        "DCML_DISCOVERY_ROOT_REQUIRED",
                        "A modules root directory is required."
                    )
                );

                return result;
            }

            string normalizedRoot;

            try
            {
                normalizedRoot =
                    Path.GetFullPath(
                        modulesRoot
                    );
            }
            catch (Exception exception)
            {
                result.AddFailure(
                    new DCMLPackageDiscoveryFailure(
                        modulesRoot,
                        null,
                        "DCML_DISCOVERY_ROOT_INVALID",
                        exception.Message
                    )
                );

                return result;
            }

            if (
                !Directory.Exists(
                    normalizedRoot
                )
            )
            {
                result.AddFailure(
                    new DCMLPackageDiscoveryFailure(
                        normalizedRoot,
                        null,
                        "DCML_DISCOVERY_ROOT_NOT_FOUND",
                        "The modules root directory was not found."
                    )
                );

                return result;
            }

            string[] packageDirectories;

            try
            {
                packageDirectories =
                    Directory.GetDirectories(
                        normalizedRoot,
                        "*",
                        SearchOption.TopDirectoryOnly
                    );
            }
            catch (Exception exception)
            {
                result.AddFailure(
                    new DCMLPackageDiscoveryFailure(
                        normalizedRoot,
                        null,
                        "DCML_DISCOVERY_ROOT_READ_FAILED",
                        exception.Message
                    )
                );

                return result;
            }

            Array.Sort(
                packageDirectories,
                StringComparer.OrdinalIgnoreCase
            );

            var discoveredIds =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            foreach (
                string packageDirectory
                in packageDirectories
            )
            {
                DiscoverPackage(
                    packageDirectory,
                    discoveredIds,
                    result
                );
            }

            return result;
        }

        private static void DiscoverPackage(
            string packageDirectory,
            HashSet<string> discoveredIds,
            DCMLPackageDiscoveryResult result
        )
        {
            string manifestPath =
                Path.Combine(
                    packageDirectory,
                    ManifestFileName
                );

            if (
                !File.Exists(
                    manifestPath
                )
            )
            {
                result.AddFailure(
                    new DCMLPackageDiscoveryFailure(
                        packageDirectory,
                        manifestPath,
                        "DCML_PACKAGE_MANIFEST_NOT_FOUND",
                        "The package does not contain manifest.json."
                    )
                );

                return;
            }

            string json;

            try
            {
                json =
                    File.ReadAllText(
                        manifestPath
                    );
            }
            catch (Exception exception)
            {
                result.AddFailure(
                    new DCMLPackageDiscoveryFailure(
                        packageDirectory,
                        manifestPath,
                        "DCML_PACKAGE_MANIFEST_READ_FAILED",
                        exception.Message
                    )
                );

                return;
            }

            DCMLManifestReadResult readResult =
                DCMLManifestJson.Deserialize(
                    json
                );

            if (
                !readResult.Success ||
                readResult.Manifest == null
            )
            {
                string errorCode =
                    readResult.ErrorCode ??
                    "DCML_PACKAGE_MANIFEST_INVALID";

                string errorMessage =
                    readResult.ErrorMessage ??
                    "The package manifest failed validation.";

                result.AddFailure(
                    new DCMLPackageDiscoveryFailure(
                        packageDirectory,
                        manifestPath,
                        errorCode,
                        errorMessage,
                        readResult.Validation.Issues
                    )
                );

                return;
            }

            DCMLModuleManifest manifest =
                readResult.Manifest;

            if (
                !discoveredIds.Add(
                    manifest.Id
                )
            )
            {
                result.AddFailure(
                    new DCMLPackageDiscoveryFailure(
                        packageDirectory,
                        manifestPath,
                        "DCML_PACKAGE_DUPLICATE_MODULE_ID",
                        "Another discovered package already uses module Id '" +
                        manifest.Id +
                        "'."
                    )
                );

                return;
            }

            result.AddPackage(
                new DCMLModulePackage(
                    packageDirectory,
                    manifestPath,
                    manifest
                )
            );
        }
    }
}
