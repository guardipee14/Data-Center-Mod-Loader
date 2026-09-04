namespace DCML.DataCenter.Persistence;

/// <summary>
/// User- or module-supplied settings for the optional process-backed
/// Data Center cable persistence source.
/// </summary>
/// <remarks>
/// Defaults are deliberately disabled and path-free. Release packages should
/// not populate machine-specific save, host, or helper paths.
/// </remarks>
public sealed class DataCenterProcessCablePersistenceSettings
{
    /// <summary>
    /// Gets or sets whether the process-backed persistence source may be used.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the exact save file selected by the user or module.
    /// </summary>
    public string SavePath { get; set; } =
        string.Empty;

    /// <summary>
    /// Gets or sets the exact process host used to run the decoder helper.
    /// </summary>
    public string HelperHostPath { get; set; } =
        string.Empty;

    /// <summary>
    /// Gets or sets the exact decoder-helper assembly path.
    /// </summary>
    public string HelperDllPath { get; set; } =
        string.Empty;

    /// <summary>
    /// Gets whether all required paths have been supplied.
    /// </summary>
    public bool HasRequiredPaths =>
        !string.IsNullOrWhiteSpace(
            SavePath) &&
        !string.IsNullOrWhiteSpace(
            HelperHostPath) &&
        !string.IsNullOrWhiteSpace(
            HelperDllPath);
}
