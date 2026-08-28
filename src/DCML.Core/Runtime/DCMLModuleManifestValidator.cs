using System;
using System.Collections.Generic;
using System.IO;
using DCML.Core.Models;

namespace DCML.Core.Runtime
{
    /// <summary>
    /// Validates DCML module manifests before a package is loaded.
    /// </summary>
    public static class DCMLModuleManifestValidator
    {
        /// <summary>
        /// Validates a module manifest.
        /// </summary>
        public static DCMLValidationResult Validate(
            DCMLModuleManifest manifest
        )
        {
            if (manifest == null)
            {
                var nullResult =
                    new DCMLValidationResult();

                nullResult.Add(
                    "DCML_MANIFEST_REQUIRED",
                    "A module manifest is required."
                );

                return nullResult;
            }

            var result =
                new DCMLValidationResult();

            ValidateSchemaVersion(
                manifest,
                result
            );

            ValidateRequiredFields(
                manifest,
                result
            );

            ValidateVersions(
                manifest,
                result
            );

            ValidateEntryAssembly(
                manifest.EntryAssembly,
                result
            );

            ValidateDependencies(
                manifest,
                result
            );

            return result;
        }

        private static void ValidateSchemaVersion(
            DCMLModuleManifest manifest,
            DCMLValidationResult result
        )
        {
            if (
                manifest.SchemaVersion !=
                DCMLManifestSchema.CurrentVersion
            )
            {
                result.Add(
                    "DCML_MANIFEST_SCHEMA_UNSUPPORTED",
                    "Manifest schema version '" +
                    manifest.SchemaVersion +
                    "' is not supported. Expected version '" +
                    DCMLManifestSchema.CurrentVersion +
                    "'."
                );
            }
        }

        private static void ValidateRequiredFields(
            DCMLModuleManifest manifest,
            DCMLValidationResult result
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    manifest.Id
                )
            )
            {
                result.Add(
                    "DCML_MANIFEST_ID_REQUIRED",
                    "The module Id is required."
                );
            }

            if (
                string.IsNullOrWhiteSpace(
                    manifest.Name
                )
            )
            {
                result.Add(
                    "DCML_MANIFEST_NAME_REQUIRED",
                    "The module Name is required."
                );
            }

            if (
                string.IsNullOrWhiteSpace(
                    manifest.Version
                )
            )
            {
                result.Add(
                    "DCML_MANIFEST_VERSION_REQUIRED",
                    "The module Version is required."
                );
            }

            if (
                string.IsNullOrWhiteSpace(
                    manifest.EntryAssembly
                )
            )
            {
                result.Add(
                    "DCML_MANIFEST_ENTRY_ASSEMBLY_REQUIRED",
                    "The module EntryAssembly is required."
                );
            }

            if (
                string.IsNullOrWhiteSpace(
                    manifest.EntryType
                )
            )
            {
                result.Add(
                    "DCML_MANIFEST_ENTRY_TYPE_REQUIRED",
                    "The module EntryType is required."
                );
            }
        }

        private static void ValidateVersions(
            DCMLModuleManifest manifest,
            DCMLValidationResult result
        )
        {
            if (
                !string.IsNullOrWhiteSpace(
                    manifest.Version
                ) &&
                !DCMLSemanticVersion.IsValid(
                    manifest.Version
                )
            )
            {
                result.Add(
                    "DCML_MANIFEST_VERSION_INVALID",
                    "The module Version must be a valid Semantic Versioning 2.0.0 version."
                );
            }

            if (
                !string.IsNullOrWhiteSpace(
                    manifest.MinimumDCMLVersion
                ) &&
                !DCMLSemanticVersion.IsValid(
                    manifest.MinimumDCMLVersion
                )
            )
            {
                result.Add(
                    "DCML_MANIFEST_MINIMUM_DCML_VERSION_INVALID",
                    "MinimumDCMLVersion must be a valid Semantic Versioning 2.0.0 version."
                );
            }
        }

        private static void ValidateEntryAssembly(
            string entryAssembly,
            DCMLValidationResult result
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    entryAssembly
                )
            )
            {
                return;
            }

            if (
                Path.IsPathRooted(
                    entryAssembly
                )
            )
            {
                result.Add(
                    "DCML_MANIFEST_ENTRY_ASSEMBLY_ROOTED",
                    "EntryAssembly must be a relative path."
                );
            }

            if (
                !string.Equals(
                    Path.GetExtension(
                        entryAssembly
                    ),
                    ".dll",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                result.Add(
                    "DCML_MANIFEST_ENTRY_ASSEMBLY_EXTENSION",
                    "EntryAssembly must reference a DLL file."
                );
            }

            string normalized =
                entryAssembly.Replace(
                    '\\',
                    '/'
                );

            string[] segments =
                normalized.Split(
                    new[] { '/' },
                    StringSplitOptions.RemoveEmptyEntries
                );

            foreach (string segment in segments)
            {
                if (segment == "..")
                {
                    result.Add(
                        "DCML_MANIFEST_ENTRY_ASSEMBLY_TRAVERSAL",
                        "EntryAssembly may not traverse outside the module directory."
                    );

                    break;
                }
            }
        }

        private static void ValidateDependencies(
            DCMLModuleManifest manifest,
            DCMLValidationResult result
        )
        {
            if (manifest.Dependencies == null)
            {
                result.Add(
                    "DCML_MANIFEST_DEPENDENCIES_INVALID",
                    "Dependencies may not be null."
                );

                return;
            }

            var dependencyIds =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            foreach (
                DCMLModuleDependency dependency
                in manifest.Dependencies
            )
            {
                if (dependency == null)
                {
                    result.Add(
                        "DCML_MANIFEST_DEPENDENCY_INVALID",
                        "A dependency entry may not be null."
                    );

                    continue;
                }

                if (
                    string.IsNullOrWhiteSpace(
                        dependency.Id
                    )
                )
                {
                    result.Add(
                        "DCML_MANIFEST_DEPENDENCY_ID_REQUIRED",
                        "Every dependency must declare an Id."
                    );

                    continue;
                }

                if (
                    !string.IsNullOrWhiteSpace(
                        dependency.MinimumVersion
                    ) &&
                    !DCMLSemanticVersion.IsValid(
                        dependency.MinimumVersion
                    )
                )
                {
                    result.Add(
                        "DCML_MANIFEST_DEPENDENCY_VERSION_INVALID",
                        "Dependency '" +
                        dependency.Id +
                        "' has an invalid MinimumVersion."
                    );
                }

                if (
                    string.Equals(
                        manifest.Id,
                        dependency.Id,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    result.Add(
                        "DCML_MANIFEST_SELF_DEPENDENCY",
                        "A module may not depend on itself."
                    );
                }

                if (
                    !dependencyIds.Add(
                        dependency.Id
                    )
                )
                {
                    result.Add(
                        "DCML_MANIFEST_DUPLICATE_DEPENDENCY",
                        "The dependency '" +
                        dependency.Id +
                        "' is declared more than once."
                    );
                }
            }
        }
    }
}
