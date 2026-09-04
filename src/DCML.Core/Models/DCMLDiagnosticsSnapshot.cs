using System.Collections.Generic;
using System.Linq;

namespace DCML.Core.Models
{
    /// <summary>
    /// Contains a point-in-time snapshot of DCML module status and
    /// structured diagnostics.
    /// </summary>
    public sealed class DCMLDiagnosticsSnapshot
    {
        public DCMLDiagnosticsSnapshot(
            IReadOnlyList<DCMLModuleStatus>? modules = null,
            IReadOnlyList<DCMLDiagnosticIssue>? diagnostics = null
        )
        {
            Modules =
                modules == null
                    ? new List<DCMLModuleStatus>()
                    : new List<DCMLModuleStatus>(modules);

            Diagnostics =
                diagnostics == null
                    ? new List<DCMLDiagnosticIssue>()
                    : new List<DCMLDiagnosticIssue>(diagnostics);
        }

        public IReadOnlyList<DCMLModuleStatus> Modules { get; }

        public IReadOnlyList<DCMLDiagnosticIssue> Diagnostics { get; }

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
