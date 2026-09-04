using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using DCML.Core.Models;

namespace DCML.Core.Runtime
{
    /// <summary>
    /// Creates privacy-safe diagnostic reports from DCML diagnostics
    /// snapshots and serializes them as stable JSON.
    /// </summary>
    public static class DCMLDiagnosticReportExporter
    {
        private const string RedactedPath =
            "[REDACTED_PATH]";

        private const string RedactedUser =
            "[REDACTED_USER]";

        private const string RedactedSave =
            "[REDACTED_SAVE]";

        private static readonly Regex WindowsPathPattern =
            new Regex(
                @"(?i)\b[A-Z]:\\[^,;\r\n]*",
                RegexOptions.Compiled
            );

        private static readonly Regex UncPathPattern =
            new Regex(
                @"\\\\[^\s\r\n]+(?:\\[^\s\r\n]+)*",
                RegexOptions.Compiled
            );

        private static readonly Regex UnixUserPathPattern =
            new Regex(
                @"/(?:home|Users)/[^/\s\r\n]+(?:/[^\s\r\n]*)?",
                RegexOptions.Compiled |
                RegexOptions.IgnoreCase
            );

        private static readonly Regex SaveSelectionPattern =
            new Regex(
                @"(?i)\b(?:selected\s+save|save\s+file|save\s+path)\s*[:=]?\s*[^,;\r\n]+",
                RegexOptions.Compiled
            );

        private static readonly JsonSerializerOptions JsonOptions =
            new JsonSerializerOptions
            {
                PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase,
                WriteIndented =
                    true
            };

        public static DCMLDiagnosticReport CreateReport(
            DCMLDiagnosticsSnapshot snapshot,
            DateTimeOffset? generatedAtUtc = null
        )
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(
                    nameof(snapshot)
                );
            }

            List<DCMLDiagnosticReportModule> modules =
                snapshot.Modules
                    .OrderBy(
                        module =>
                            module.ModuleId,
                        StringComparer.OrdinalIgnoreCase
                    )
                    .Select(
                        module =>
                            new DCMLDiagnosticReportModule(
                                module.ModuleId,
                                SanitizeText(
                                    module.Name
                                ),
                                module.Version,
                                module.State
                            )
                    )
                    .ToList();

            List<DCMLDiagnosticReportIssue> diagnostics =
                snapshot.Diagnostics
                    .Select(
                        diagnostic =>
                            new DCMLDiagnosticReportIssue(
                                diagnostic.Stage,
                                diagnostic.Severity,
                                diagnostic.Code,
                                SanitizeText(
                                    diagnostic.Message
                                ),
                                moduleId:
                                    diagnostic.ModuleId,
                                dependencyId:
                                    diagnostic.DependencyId,
                                requirementId:
                                    diagnostic.RequirementId,
                                exceptionType:
                                    diagnostic.ExceptionType
                            )
                    )
                    .ToList();

            return new DCMLDiagnosticReport(
                DCMLVersion.Current,
                generatedAtUtc ??
                    DateTimeOffset.UtcNow,
                modules,
                diagnostics
            );
        }

        public static string ExportJson(
            DCMLDiagnosticsSnapshot snapshot,
            DateTimeOffset? generatedAtUtc = null
        )
        {
            DCMLDiagnosticReport report =
                CreateReport(
                    snapshot,
                    generatedAtUtc
                );

            return JsonSerializer.Serialize(
                report,
                JsonOptions
            );
        }

        public static void ExportToFile(
            DCMLDiagnosticsSnapshot snapshot,
            string path,
            DateTimeOffset? generatedAtUtc = null
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    path
                )
            )
            {
                throw new ArgumentException(
                    "A diagnostic report path is required.",
                    nameof(path)
                );
            }

            string fullPath =
                Path.GetFullPath(
                    path
                );

            string? directory =
                Path.GetDirectoryName(
                    fullPath
                );

            if (
                !string.IsNullOrWhiteSpace(
                    directory
                )
            )
            {
                Directory.CreateDirectory(
                    directory
                );
            }

            File.WriteAllText(
                fullPath,
                ExportJson(
                    snapshot,
                    generatedAtUtc
                )
            );
        }

        internal static string SanitizeText(
            string? value
        )
        {
            if (
                string.IsNullOrEmpty(
                    value
                )
            )
            {
                return string.Empty;
            }

            string sanitized =
                value;

            string userProfile =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile
                );

            if (
                !string.IsNullOrWhiteSpace(
                    userProfile
                )
            )
            {
                sanitized =
                    ReplaceOrdinalIgnoreCase(
                        sanitized,
                        userProfile,
                        RedactedPath
                    );
            }

            string userName =
                Environment.UserName;

            if (
                !string.IsNullOrWhiteSpace(
                    userName
                ) &&
                userName.Length >= 2
            )
            {
                sanitized =
                    ReplaceOrdinalIgnoreCase(
                        sanitized,
                        userName,
                        RedactedUser
                    );
            }

            sanitized =
                SaveSelectionPattern.Replace(
                    sanitized,
                    RedactedSave
                );

            sanitized =
                WindowsPathPattern.Replace(
                    sanitized,
                    RedactedPath
                );

            sanitized =
                UncPathPattern.Replace(
                    sanitized,
                    RedactedPath
                );

            sanitized =
                UnixUserPathPattern.Replace(
                    sanitized,
                    RedactedPath
                );

            return sanitized;
        }

        private static string ReplaceOrdinalIgnoreCase(
            string value,
            string oldValue,
            string newValue
        )
        {
            int startIndex =
                0;

            while (true)
            {
                int index =
                    value.IndexOf(
                        oldValue,
                        startIndex,
                        StringComparison.OrdinalIgnoreCase
                    );

                if (index < 0)
                {
                    return value;
                }

                value =
                    value.Remove(
                        index,
                        oldValue.Length
                    )
                    .Insert(
                        index,
                        newValue
                    );

                startIndex =
                    index +
                    newValue.Length;
            }
        }
    }
}
