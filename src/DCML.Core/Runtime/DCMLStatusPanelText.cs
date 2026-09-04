using System;
using System.Collections.Generic;
using System.Linq;
using DCML.Core.Models;

namespace DCML.Core.Runtime
{
    /// <summary>
    /// Produces compact, path-safe text for status surfaces such as
    /// host overlays. Package and manifest paths are intentionally
    /// excluded from this representation.
    /// </summary>
    public static class DCMLStatusPanelText
    {
        public static IReadOnlyList<string> Build(
            DCMLDiagnosticsSnapshot snapshot,
            int maxModules = 8,
            int maxDiagnostics = 6
        )
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(
                    nameof(snapshot)
                );
            }

            if (maxModules < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxModules)
                );
            }

            if (maxDiagnostics < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxDiagnostics)
                );
            }

            var lines =
                new List<string>
                {
                    "Modules: " +
                    snapshot.Modules.Count +
                    " | Running: " +
                    snapshot.RunningModuleCount +
                    " | Errors: " +
                    snapshot.ErrorCount +
                    " | Warnings: " +
                    snapshot.WarningCount
                };

            List<DCMLModuleStatus> modules =
                snapshot.Modules
                    .OrderBy(
                        module =>
                            module.ModuleId,
                        StringComparer.OrdinalIgnoreCase
                    )
                    .ToList();

            foreach (
                DCMLModuleStatus module
                in modules.Take(
                    maxModules
                )
            )
            {
                lines.Add(
                    "[" +
                    module.State +
                    "] " +
                    module.ModuleId +
                    " v" +
                    module.Version
                );
            }

            if (
                modules.Count >
                maxModules
            )
            {
                lines.Add(
                    "... " +
                    (modules.Count - maxModules) +
                    " more module(s)"
                );
            }

            if (
                snapshot.Diagnostics.Count != 0
            )
            {
                lines.Add(
                    "Diagnostics:"
                );
            }

            foreach (
                DCMLDiagnosticIssue diagnostic
                in snapshot.Diagnostics.Take(
                    maxDiagnostics
                )
            )
            {
                lines.Add(
                    FormatDiagnostic(
                        diagnostic
                    )
                );
            }

            if (
                snapshot.Diagnostics.Count >
                maxDiagnostics
            )
            {
                lines.Add(
                    "... " +
                    (
                        snapshot.Diagnostics.Count -
                        maxDiagnostics
                    ) +
                    " more diagnostic(s)"
                );
            }

            return lines;
        }

        private static string FormatDiagnostic(
            DCMLDiagnosticIssue diagnostic
        )
        {
            var context =
                new List<string>();

            if (
                !string.IsNullOrWhiteSpace(
                    diagnostic.ModuleId
                )
            )
            {
                context.Add(
                    "module=" +
                    diagnostic.ModuleId
                );
            }

            if (
                !string.IsNullOrWhiteSpace(
                    diagnostic.DependencyId
                )
            )
            {
                context.Add(
                    "dependency=" +
                    diagnostic.DependencyId
                );
            }

            if (
                !string.IsNullOrWhiteSpace(
                    diagnostic.RequirementId
                )
            )
            {
                context.Add(
                    "requirement=" +
                    diagnostic.RequirementId
                );
            }

            string contextText =
                context.Count == 0
                    ? string.Empty
                    : " [" +
                      string.Join(
                          "; ",
                          context
                      ) +
                      "]";

            string message =
                (
                    diagnostic.Message ??
                    string.Empty
                )
                .Replace(
                    "\r",
                    " "
                )
                .Replace(
                    "\n",
                    " "
                );

            return
                "[" +
                diagnostic.Severity +
                "] " +
                diagnostic.Code +
                " (" +
                diagnostic.Stage +
                ")" +
                contextText +
                ": " +
                message;
        }
    }
}
