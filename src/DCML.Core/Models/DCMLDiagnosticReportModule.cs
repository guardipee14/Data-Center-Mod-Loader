namespace DCML.Core.Models
{
    /// <summary>
    /// Describes one module in a privacy-safe diagnostic report.
    /// </summary>
    public sealed class DCMLDiagnosticReportModule
    {
        public DCMLDiagnosticReportModule(
            string moduleId,
            string name,
            string version,
            DCMLModuleStatusState state
        )
        {
            ModuleId = moduleId;
            Name = name;
            Version = version;
            State = state;
        }

        public string ModuleId { get; }

        public string Name { get; }

        public string Version { get; }

        public DCMLModuleStatusState State { get; }
    }
}
