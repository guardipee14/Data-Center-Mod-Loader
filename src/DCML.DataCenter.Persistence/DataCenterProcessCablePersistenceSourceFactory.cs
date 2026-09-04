using DCML.DataCenter.Abstractions;

namespace DCML.DataCenter.Persistence;

/// <summary>
/// Creates the optional process-backed persistence source from typed
/// module-owned settings.
/// </summary>
public static class DataCenterProcessCablePersistenceSourceFactory
{
    /// <summary>
    /// Creates the persistence source only when the supplied settings are
    /// explicitly enabled and fully configured.
    /// </summary>
    /// <remarks>
    /// Disabled or incomplete settings return <see langword="null"/>. The
    /// factory never discovers save files and never chooses a save implicitly.
    /// </remarks>
    public static IDataCenterCablePersistenceSource? Create(
        DataCenterProcessCablePersistenceSettings? settings)
    {
        if (
            settings is null ||
            !settings.Enabled ||
            !settings.HasRequiredPaths
        )
        {
            return null;
        }

        return
            new DataCenterProcessCablePersistenceSource(
                settings.HelperHostPath,
                settings.HelperDllPath,
                settings.SavePath);
    }
}
