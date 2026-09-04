namespace DCML.Core.Models
{
    /// <summary>
    /// Describes the current diagnostic status of one DCML module.
    /// </summary>
    public sealed class DCMLModuleStatus
    {
        public DCMLModuleStatus(
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

        public bool IsRunning =>
            State == DCMLModuleStatusState.Running;
    }
}
