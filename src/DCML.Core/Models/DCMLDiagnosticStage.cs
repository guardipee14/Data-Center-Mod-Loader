namespace DCML.Core.Models
{
    /// <summary>
    /// Identifies the DCML processing stage that produced a diagnostic.
    /// </summary>
    public enum DCMLDiagnosticStage
    {
        Unknown = 0,
        Discovery = 1,
        Validation = 2,
        Compatibility = 3,
        DependencyResolution = 4,
        Runtime = 5,
        Activation = 6,
        Initialization = 7,
        Start = 8,
        Stop = 9,
        Host = 10
    }
}
