namespace DCML.Core.Models
{
    /// <summary>
    /// Describes the current lifecycle state of a DCML module.
    /// </summary>
    public enum DCMLModuleRuntimeState
    {
        Pending = 0,
        Activating = 1,
        Initializing = 2,
        Starting = 3,
        Running = 4,
        Blocked = 5,
        Failed = 6,
        Stopped = 7
    }
}
