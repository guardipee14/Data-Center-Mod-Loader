using System;
using System.IO;
using DCML.Core;
using DCML.Core.Abstractions;
using DCML.Core.Services;
using MelonLoader;

namespace DCML.Loader.MelonLoader;

public sealed class MelonModuleContext : IDCMLModuleContext
{
    public MelonModuleContext(
        string moduleId,
        string moduleDirectory,
        string dataDirectory,
        string gameRoot,
        IDCMLEventBus eventBus,
        IDCMLGameLifecycle gameLifecycle,
        IDCMLGameObjectDiscovery gameObjectDiscovery,
        IDCMLGameTypeCatalog gameTypeCatalog,
        IDCMLGameResourceDiscovery gameResourceDiscovery,
        IDCMLGameTypeInspector gameTypeInspector,
        IDCMLGameThread gameThread,
        IDCMLGameComponentStateReader gameComponentStateReader)
    {
        if (string.IsNullOrWhiteSpace(moduleId))
        {
            throw new ArgumentException(
                "Module ID cannot be empty.",
                nameof(moduleId));
        }

        if (string.IsNullOrWhiteSpace(moduleDirectory))
        {
            throw new ArgumentException(
                "Module directory cannot be empty.",
                nameof(moduleDirectory));
        }

        if (string.IsNullOrWhiteSpace(dataDirectory))
        {
            throw new ArgumentException(
                "Data directory cannot be empty.",
                nameof(dataDirectory));
        }

        if (string.IsNullOrWhiteSpace(gameRoot))
        {
            throw new ArgumentException(
                "Game root cannot be empty.",
                nameof(gameRoot));
        }

        if (eventBus is null)
        {
            throw new ArgumentNullException(
                nameof(eventBus));
        }

        if (gameLifecycle is null)
        {
            throw new ArgumentNullException(
                nameof(gameLifecycle));
        }

        if (gameObjectDiscovery is null)
        {
            throw new ArgumentNullException(
                nameof(gameObjectDiscovery));
        }

        if (gameTypeCatalog is null)
        {
            throw new ArgumentNullException(
                nameof(gameTypeCatalog));
        }

        if (gameResourceDiscovery is null)
        {
            throw new ArgumentNullException(
                nameof(gameResourceDiscovery));
        }

        if (gameTypeInspector is null)
        {
            throw new ArgumentNullException(
                nameof(gameTypeInspector));
        }

        if (gameThread is null)
        {
            throw new ArgumentNullException(
                nameof(gameThread));
        }

        if (gameComponentStateReader is null)
        {
            throw new ArgumentNullException(
                nameof(gameComponentStateReader));
        }

        ModuleDirectory =
            Path.GetFullPath(
                moduleDirectory);

        DataDirectory =
            Path.GetFullPath(
                dataDirectory);

        var normalizedGameRoot =
            Path.GetFullPath(
                gameRoot)
                .TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);

        var logger =
            new MelonDCMLLogger(
                moduleId);

        var configuration =
            new DCMLJsonConfiguration(
                Path.Combine(
                    DataDirectory,
                    "config.json"));

        var runtimeInfo =
            new DCMLRuntimeInfo(
                moduleId,
                DCMLVersion.Current,
                "MelonLoader",
                GetMelonLoaderVersion(),
                "Data Center",
                normalizedGameRoot,
                MelonHostCapabilities.Create());

        Services =
            new DCMLServiceProvider(
                (
                    typeof(IDCMLLogger),
                    logger
                ),
                (
                    typeof(IDCMLRuntimeInfo),
                    runtimeInfo
                ),
                (
                    typeof(IDCMLCapabilityCatalog),
                    runtimeInfo
                ),
                (
                    typeof(IDCMLConfiguration),
                    configuration
                ),
                (
                    typeof(IDCMLEventBus),
                    eventBus
                ),
                (
                    typeof(IDCMLGameLifecycle),
                    gameLifecycle
                ),
                (
                    typeof(IDCMLGameObjectDiscovery),
                    gameObjectDiscovery
                ),
                (
                    typeof(IDCMLGameTypeCatalog),
                    gameTypeCatalog
                ),
                (
                    typeof(IDCMLGameResourceDiscovery),
                    gameResourceDiscovery
                ),
                (
                    typeof(IDCMLGameTypeInspector),
                    gameTypeInspector
                ),
                (
                    typeof(IDCMLGameThread),
                    gameThread
                ),
                (
                    typeof(IDCMLGameComponentStateReader),
                    gameComponentStateReader
                ));
    }

    public string ModuleDirectory { get; }

    public string DataDirectory { get; }

    public IServiceProvider Services { get; }

    private static string GetMelonLoaderVersion()
    {
        return
            typeof(MelonMod)
                .Assembly
                .GetName()
                .Version?
                .ToString()
            ?? "unknown";
    }
}
