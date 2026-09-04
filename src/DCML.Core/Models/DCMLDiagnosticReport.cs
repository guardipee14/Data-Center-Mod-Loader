using System;
using System.Collections.Generic;
using System.Linq;

namespace DCML.Core.Models
{
    /// <summary>
    /// Privacy-safe, serializable DCML diagnostic report contract.
    /// </summary>
    public sealed class DCMLDiagnosticReport
    {
        public const string CurrentSchemaVersion =
            "1.0";

        public DCMLDiagnosticReport(
            string dcmlVersion,
            DateTimeOffset generatedAtUtc,
            IReadOnlyList<DCMLDiagnosticReportModule>? modules = null,
            IReadOnlyList<DCMLDiagnosticReportIssue>? diagnostics = null
        )
        {
            SchemaVersion =
                CurrentSchemaVersion;

            DCMLVersion =
                dcmlVersion;

            GeneratedAtUtc =
                generatedAtUtc.ToUniversalTime();

            Modules =
                modules == null
                    ? new List<DCMLDiagnosticReportModule>()
                    : new List<DCMLDiagnosticReportModule>(
                        modules
                    );

            Diagnostics =
                diagnostics == null
                    ? new List<DCMLDiagnosticReportIssue>()
                    : new List<DCMLDiagnosticReportIssue>(
                        diagnostics
                    );
        }

        public string SchemaVersion { get; }

        public string DCMLVersion { get; }

        public DateTimeOffset GeneratedAtUtc { get; }

        public IReadOnlyList<DCMLDiagnosticReportModule> Modules { get; }

        public IReadOnlyList<DCMLDiagnosticReportIssue> Diagnostics { get; }

        public int RunningModuleCount =>
            Modules.Count(
                module =>
                    module.State ==
                    DCMLModuleStatusState.Running
            );

        public int InfoCount =>
            Diagnostics.Count(
                diagnostic =>
                    diagnostic.Severity ==
                    DCMLDiagnosticSeverity.Info
            );

        public int WarningCount =>
            Diagnostics.Count(
                diagnostic =>
                    diagnostic.Severity ==
                    DCMLDiagnosticSeverity.Warning
            );

        public int ErrorCount =>
            Diagnostics.Count(
                diagnostic =>
                    diagnostic.Severity ==
                    DCMLDiagnosticSeverity.Error
            );

        public bool HasErrors =>
            ErrorCount != 0;
    }
}
