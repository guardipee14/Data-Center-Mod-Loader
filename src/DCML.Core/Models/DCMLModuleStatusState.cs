namespace DCML.Core.Models
{
    /// <summary>
    /// Describes the overall DCML processing and lifecycle state of a
    /// module or package.
    /// </summary>
    public enum DCMLModuleStatusState
    {
        Unknown = 0,
        Discovered = 1,
        Invalid = 2,
        Compatible = 3,
        Incompatible = 4,
        Blocked = 5,
        Pending = 6,
        Activating = 7,
        Initializing = 8,
        Starting = 9,
        Running = 10,
        Failed = 11,
        Stopped = 12
    }
}
