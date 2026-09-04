namespace DCML.Core.Models
{
    /// <summary>
    /// Describes one structured diagnostic produced while discovering,
    /// validating, resolving, or running a DCML module.
    /// </summary>
    public sealed class DCMLDiagnosticIssue
    {
        public DCMLDiagnosticIssue(
            DCMLDiagnosticStage stage,
            DCMLDiagnosticSeverity severity,
            string code,
            string message,
            string? moduleId = null,
            string? packageDirectory = null,
            string? manifestPath = null,
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
            PackageDirectory = packageDirectory;
            ManifestPath = manifestPath;
            DependencyId = dependencyId;
            RequirementId = requirementId;
            ExceptionType = exceptionType;
        }

        public DCMLDiagnosticStage Stage { get; }

        public DCMLDiagnosticSeverity Severity { get; }

        public string Code { get; }

        public string Message { get; }

        public string? ModuleId { get; }

        public string? PackageDirectory { get; }

        public string? ManifestPath { get; }

        public string? DependencyId { get; }

        public string? RequirementId { get; }

        public string? ExceptionType { get; }
    }
}
