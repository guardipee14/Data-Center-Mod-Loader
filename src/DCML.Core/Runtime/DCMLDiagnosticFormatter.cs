using System;
using System.Collections.Generic;
using DCML.Core.Models;

namespace DCML.Core.Runtime
{
    /// <summary>
    /// Formats structured DCML diagnostics for developer-facing text
    /// output while preserving stable diagnostic codes and context.
    /// </summary>
    public static class DCMLDiagnosticFormatter
    {
        public static string Format(
            DCMLDiagnosticIssue issue
        )
        {
            if (issue == null)
            {
                throw new ArgumentNullException(
                    nameof(issue)
                );
            }

            var context =
                new List<string>();

            if (
                !string.IsNullOrWhiteSpace(
                    issue.ModuleId
                )
            )
            {
                context.Add(
                    "module=" +
                    issue.ModuleId
                );
            }

            if (
                !string.IsNullOrWhiteSpace(
                    issue.DependencyId
                )
            )
            {
                context.Add(
                    "dependency=" +
                    issue.DependencyId
                );
            }

            if (
                !string.IsNullOrWhiteSpace(
                    issue.RequirementId
                )
            )
            {
                context.Add(
                    "requirement=" +
                    issue.RequirementId
                );
            }

            if (
                !string.IsNullOrWhiteSpace(
                    issue.PackageDirectory
                )
            )
            {
                context.Add(
                    "package=" +
                    issue.PackageDirectory
                );
            }

            if (
                !string.IsNullOrWhiteSpace(
                    issue.ManifestPath
                )
            )
            {
                context.Add(
                    "manifest=" +
                    issue.ManifestPath
                );
            }

            if (
                !string.IsNullOrWhiteSpace(
                    issue.ExceptionType
                )
            )
            {
                context.Add(
                    "exception=" +
                    issue.ExceptionType
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

            return
                "[" +
                issue.Severity +
                "] [" +
                issue.Stage +
                "] " +
                issue.Code +
                contextText +
                ": " +
                issue.Message;
        }
    }
}
