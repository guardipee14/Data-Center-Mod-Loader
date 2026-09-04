namespace DCML.Core.Models
{
    /// <summary>
    /// Describes one sanitized diagnostic in an exported report.
    /// Filesystem-specific source fields are intentionally absent.
    /// </summary>
    public sealed class DCMLDiagnosticReportIssue
    {
        public DCMLDiagnosticReportIssue(
            DCMLDiagnosticStage stage,
            DCMLDiagnosticSeverity severity,
            string code,
            string message,
            string? moduleId = null,
            string? dependencyId = null,
            string? requirementId = null,
            string? exceptionType = null
        )
        {
            Stage = stage;
            Severity = severity;
            Code = code;
            Message = message;
            ModuleId = moduleId;
            DependencyId = dependencyId;
            RequirementId = requirementId;
            ExceptionType = exceptionType;
        }

        public DCMLDiagnosticStage Stage { get; }

        public DCMLDiagnosticSeverity Severity { get; }

        public string Code { get; }

        public string Message { get; }

        public string? ModuleId { get; }

        public string? DependencyId { get; }

        public string? RequirementId { get; }

        public string? ExceptionType { get; }
    }
}
