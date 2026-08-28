using System;
using System.IO;
using System.Linq;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using DCML.DataCenter;
using DCML.DataCenter.Models;

namespace DCML.TestModule;

public sealed class TestModule : IDCMLModule
{
    private IDCMLModuleContext? _context;

    private IDCMLLogger? _logger;

    private IDCMLRuntimeInfo? _runtimeInfo;

    private IDCMLConfiguration? _configuration;

    private IDCMLEventBus? _eventBus;

    private IDCMLGameLifecycle? _gameLifecycle;

    private IDCMLGameObjectDiscovery? _gameObjectDiscovery;

    private DataCenterApi? _dataCenterApi;

    private IDisposable? _probeSubscription;

    private IDisposable? _sceneSubscription;

    private ProbeSettings? _settings;

    private int _eventsReceived;

    private int _sceneEventsReceived;

    private DCMLSceneLifecycleEvent? _lastSceneEvent;

    private int _objectDiscoveryRuns;

    private int _lastObjectDiscoveryCount;

    private string _lastObjectDiscoveryScene =
        string.Empty;

    private string _lastObjectDiscoveryError =
        string.Empty;

    private string _lastObjectDiscoverySample =
        string.Empty;

    private int _recommendedApiRuns;

    private int _lastRecommendedEntityCount;

    private string _lastRecommendedScene =
        string.Empty;

    private string _lastRecommendedKinds =
        string.Empty;

    private string _lastRecommendedError =
        string.Empty;

    private string _lastRecommendedSample =
        string.Empty;

    private int _componentInventoryRuns;

    private int _lastComponentInventoryObjectCount;

    private int _lastComponentInventoryTypeCount;

    private int _lastComponentInventoryIl2CppTypeCount;

    private int _lastComponentInventoryUnityTypeCount;

    private string _lastComponentInventoryScene =
        string.Empty;

    private string _lastComponentInventoryPath =
        string.Empty;

    private string _lastComponentInventoryError =
        string.Empty;

    private string _lastComponentInventoryIl2CppSample =
        string.Empty;

    private int _lastComponentInventoryPagesScanned;

    private bool _lastComponentInventoryComplete;

    public string Id =>
        "dcml.test.lifecycle";

    public string Name =>
        "DCML Lifecycle Test Module";

    public string Version =>
        "0.0.1";

    public void Initialize(
        IDCMLModuleContext context)
    {
        _context =
            context ??
            throw new ArgumentNullException(
                nameof(context));

        _logger =
            context.Services.GetService(
                typeof(IDCMLLogger))
            as IDCMLLogger;

        _runtimeInfo =
            context.Services.GetService(
                typeof(IDCMLRuntimeInfo))
            as IDCMLRuntimeInfo;

        _configuration =
            context.Services.GetService(
                typeof(IDCMLConfiguration))
            as IDCMLConfiguration;

        _eventBus =
            context.Services.GetService(
                typeof(IDCMLEventBus))
            as IDCMLEventBus;

        _gameLifecycle =
            context.Services.GetService(
                typeof(IDCMLGameLifecycle))
            as IDCMLGameLifecycle;

        _gameObjectDiscovery =
            context.Services.GetService(
                typeof(IDCMLGameObjectDiscovery))
            as IDCMLGameObjectDiscovery;

        if (_logger is null)
        {
            throw new InvalidOperationException(
                "DCML did not provide IDCMLLogger through the module service provider.");
        }

        if (_runtimeInfo is null)
        {
            throw new InvalidOperationException(
                "DCML did not provide IDCMLRuntimeInfo through the module service provider.");
        }

        if (_configuration is null)
        {
            throw new InvalidOperationException(
                "DCML did not provide IDCMLConfiguration through the module service provider.");
        }

        if (_eventBus is null)
        {
            throw new InvalidOperationException(
                "DCML did not provide IDCMLEventBus through the module service provider.");
        }

        if (_gameLifecycle is null)
        {
            throw new InvalidOperationException(
                "DCML did not provide IDCMLGameLifecycle through the module service provider.");
        }

        if (_gameObjectDiscovery is null)
        {
            throw new InvalidOperationException(
                "DCML did not provide IDCMLGameObjectDiscovery through the module service provider.");
        }

        _dataCenterApi =
            DataCenterApi.Create(
                context);

        if (!string.Equals(
                _runtimeInfo.ModuleId,
                Id,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Runtime module ID '{_runtimeInfo.ModuleId}' does not match module ID '{Id}'.");
        }

        foreach (string capability in new[]
        {
            DCMLRuntimeCapabilities.Logging,
            DCMLRuntimeCapabilities.RuntimeInformation,
            DCMLRuntimeCapabilities.Configuration,
            DCMLRuntimeCapabilities.Events,
            DCMLRuntimeCapabilities.GameSceneLifecycle,
            DCMLRuntimeCapabilities.GameObjectDiscovery
        })
        {
            if (!_runtimeInfo.HasCapability(capability))
            {
                throw new InvalidOperationException(
                    $"DCML runtime information did not advertise '{capability}'.");
            }
        }

        _settings =
            _configuration.Load(
                new ProbeSettings());

        _settings.LaunchCount++;
        _settings.LastLifecycleStage =
            "Initialize";
        _settings.LastDCMLVersion =
            _runtimeInfo.DCMLVersion;

        _configuration.Save(
            _settings);

        _probeSubscription =
            _eventBus.Subscribe<ProbeEvent>(
                OnProbeEvent);

        _sceneSubscription =
            _eventBus.Subscribe<DCMLSceneLifecycleEvent>(
                OnSceneLifecycleEvent);

        AppendProof(
            "Initialize");

        _logger.Info(
            "Initialize completed.");

        _logger.Info(
            $"Runtime info: DCML {_runtimeInfo.DCMLVersion}; " +
            $"Host {_runtimeInfo.HostName} {_runtimeInfo.HostVersion}; " +
            $"Module {_runtimeInfo.ModuleId}; " +
            $"Game {_runtimeInfo.GameName}.");

        _logger.Info(
            $"Configuration loaded from '{_configuration.ConfigurationPath}'. " +
            $"Launch count: {_settings.LaunchCount}.");

        _logger.Info(
            "Event subscription registered.");

        _logger.Info(
            "Game scene lifecycle subscription registered.");

        _logger.Info(
            "Game object discovery service registered.");

        _logger.Info(
            "Optional DCML.DataCenter recommended API enabled by this module.");
    }

    public void Start()
    {
        EnsureInitialized();

        _settings!.LastLifecycleStage =
            "Start";

        _configuration!.Save(
            _settings);

        AppendProof(
            "Start");

        _logger!.Info(
            "Start completed.");

        _eventBus!.Publish(
            new ProbeEvent(
                Id,
                "Start"));
    }

    public void Stop()
    {
        if (_context is null)
        {
            return;
        }

        if (
            _settings is not null &&
            _configuration is not null
        )
        {
            _settings.LastLifecycleStage =
                "Stop";

            _configuration.Save(
                _settings);
        }

        _probeSubscription?.Dispose();
        _probeSubscription =
            null;

        _sceneSubscription?.Dispose();
        _sceneSubscription =
            null;

        AppendProof(
            "Stop");

        _logger?.Info(
            "Stop completed.");
    }

    private void OnProbeEvent(
        ProbeEvent eventData)
    {
        _eventsReceived++;

        AppendProof(
            "EventReceived");

        _logger?.Info(
            $"Event received from '{eventData.SourceModuleId}' at stage '{eventData.Stage}'.");
    }

    private void OnSceneLifecycleEvent(
        DCMLSceneLifecycleEvent eventData)
    {
        _sceneEventsReceived++;
        _lastSceneEvent =
            eventData;

        AppendProof(
            "SceneEvent");

        _logger?.Info(
            $"Scene event received: {eventData.Stage}; " +
            $"BuildIndex {eventData.BuildIndex}; " +
            $"Scene '{eventData.SceneName}'; " +
            $"Sequence {eventData.Sequence}.");

        if (
            eventData.Stage ==
                DCMLSceneLifecycleStage.Initialized &&
            !string.IsNullOrWhiteSpace(
                eventData.SceneName)
        )
        {
            RunObjectDiscovery(
                eventData.SceneName);

            RunRecommendedDataCenterApi(
                eventData.SceneName);

            RunComponentInventory(
                eventData.SceneName);
        }
    }

    private void RunObjectDiscovery(
        string sceneName)
    {
        if (_gameObjectDiscovery is null)
        {
            return;
        }

        try
        {
            var results =
                _gameObjectDiscovery.Find(
                    new DCMLGameObjectQuery(
                        sceneName: sceneName,
                        includeInactive: true,
                        maxResults: 64));

            _objectDiscoveryRuns++;
            _lastObjectDiscoveryCount =
                results.Count;
            _lastObjectDiscoveryScene =
                sceneName;
            _lastObjectDiscoveryError =
                string.Empty;

            _lastObjectDiscoverySample =
                string.Join(
                    " || ",
                    results
                        .Take(8)
                        .Select(
                            value =>
                                value.HierarchyPath +
                                " [" +
                                string.Join(
                                    ",",
                                    value.ComponentTypeNames.Take(4)) +
                                "]"));

            AppendProof(
                "ObjectDiscovery");

            _logger?.Info(
                $"Game object discovery returned {results.Count} object(s) for scene '{sceneName}'.");
        }
        catch (Exception exception)
        {
            _objectDiscoveryRuns++;
            _lastObjectDiscoveryCount =
                0;
            _lastObjectDiscoveryScene =
                sceneName;
            _lastObjectDiscoveryError =
                exception.GetType().FullName +
                ": " +
                exception.Message;
            _lastObjectDiscoverySample =
                string.Empty;

            AppendProof(
                "ObjectDiscoveryError");

            _logger?.Error(
                $"Game object discovery failed for scene '{sceneName}'.");

            _logger?.Error(
                exception.ToString());
        }
    }


    private void RunRecommendedDataCenterApi(
        string sceneName)
    {
        if (_dataCenterApi is null)
        {
            return;
        }

        try
        {
            var entities =
                _dataCenterApi.Entities.Find(
                    new DataCenterEntityQuery(
                        kind:
                            DataCenterEntityKinds.UserInterface,
                        sceneName:
                            sceneName,
                        includeInactive:
                            true,
                        includeUnknown:
                            false,
                        maxResults:
                            32));

            _recommendedApiRuns++;
            _lastRecommendedEntityCount =
                entities.Count;
            _lastRecommendedScene =
                sceneName;
            _lastRecommendedError =
                string.Empty;

            _lastRecommendedKinds =
                string.Join(
                    ", ",
                    entities
                        .GroupBy(
                            value =>
                                value.Kind,
                            StringComparer.OrdinalIgnoreCase)
                        .OrderBy(
                            group =>
                                group.Key,
                            StringComparer.OrdinalIgnoreCase)
                        .Select(
                            group =>
                                group.Key +
                                "=" +
                                group.Count()));

            _lastRecommendedSample =
                string.Join(
                    " || ",
                    entities
                        .Take(8)
                        .Select(
                            value =>
                                value.Kind +
                                ":" +
                                value.HierarchyPath +
                                " [" +
                                value.ClassificationRuleId +
                                "]"));

            AppendProof(
                "RecommendedDataCenterApi");

            _logger?.Info(
                $"Optional DCML.DataCenter API returned {entities.Count} recommended semantic entity/entities for scene '{sceneName}'.");
        }
        catch (Exception exception)
        {
            _recommendedApiRuns++;
            _lastRecommendedEntityCount =
                0;
            _lastRecommendedScene =
                sceneName;
            _lastRecommendedKinds =
                string.Empty;
            _lastRecommendedError =
                exception.GetType().FullName +
                ": " +
                exception.Message;
            _lastRecommendedSample =
                string.Empty;

            AppendProof(
                "RecommendedDataCenterApiError");

            _logger?.Error(
                $"Optional DCML.DataCenter API failed for scene '{sceneName}'.");

            _logger?.Error(
                exception.ToString());
        }
    }

    private void RunComponentInventory(
        string sceneName)
    {
        if (
            _context is null ||
            _dataCenterApi is null
        )
        {
            return;
        }

        try
        {
            DataCenterComponentCatalogSnapshot snapshot =
                _dataCenterApi.Components.Scan(
                    new DataCenterComponentCatalogQuery(
                        sceneName:
                            sceneName,
                        typeNamePrefix:
                            "Il2Cpp.",
                        includeInactive:
                            true,
                        maxObjects:
                            DataCenterComponentCatalogQuery.DefaultMaxObjects,
                        maxExamplesPerType:
                            DataCenterComponentCatalogQuery.DefaultMaxExamplesPerType,
                        scanAllPages:
                            true,
                        maxPages:
                            DataCenterComponentCatalogQuery.DefaultMaxPages));

            _componentInventoryRuns++;
            _lastComponentInventoryObjectCount =
                snapshot.ScannedObjectCount;
            _lastComponentInventoryTypeCount =
                snapshot.UniqueComponentTypeCount;
            _lastComponentInventoryIl2CppTypeCount =
                snapshot.Il2CppTypeCount;
            _lastComponentInventoryUnityTypeCount =
                snapshot.UnityEngineTypeCount;
            _lastComponentInventoryScene =
                sceneName;
            _lastComponentInventoryPagesScanned =
                snapshot.PagesScanned;
            _lastComponentInventoryComplete =
                snapshot.IsComplete;
            _lastComponentInventoryError =
                string.Empty;

            _lastComponentInventoryIl2CppSample =
                string.Join(
                    " || ",
                    snapshot.ComponentTypes
                        .Where(
                            value =>
                                value.IsIl2Cpp)
                        .OrderByDescending(
                            value =>
                                value.ObjectCount)
                        .ThenBy(
                            value =>
                                value.TypeName,
                            StringComparer.Ordinal)
                        .Take(12)
                        .Select(
                            value =>
                                value.TypeName +
                                "=" +
                                value.ObjectCount));

            _lastComponentInventoryPath =
                WriteComponentInventory(
                    snapshot);

            AppendProof(
                "ComponentInventory");

            _logger?.Info(
                $"Focused IL2CPP component inventory scanned {snapshot.ScannedObjectCount} object(s), " +
                $"{snapshot.UniqueComponentTypeCount} unique component type(s), and " +
                $"{snapshot.Il2CppTypeCount} Il2Cpp type(s) for scene '{sceneName}'.");
        }
        catch (Exception exception)
        {
            _componentInventoryRuns++;
            _lastComponentInventoryObjectCount =
                0;
            _lastComponentInventoryTypeCount =
                0;
            _lastComponentInventoryIl2CppTypeCount =
                0;
            _lastComponentInventoryUnityTypeCount =
                0;
            _lastComponentInventoryScene =
                sceneName;
            _lastComponentInventoryPagesScanned =
                0;
            _lastComponentInventoryComplete =
                false;
            _lastComponentInventoryPath =
                string.Empty;
            _lastComponentInventoryIl2CppSample =
                string.Empty;
            _lastComponentInventoryError =
                exception.GetType().FullName +
                ": " +
                exception.Message;

            AppendProof(
                "ComponentInventoryError");

            _logger?.Error(
                $"Focused IL2CPP component inventory failed for scene '{sceneName}'.");

            _logger?.Error(
                exception.ToString());
        }
    }

    private string WriteComponentInventory(
        DataCenterComponentCatalogSnapshot snapshot)
    {
        if (_context is null)
        {
            throw new InvalidOperationException(
                "The module context is unavailable.");
        }

        Directory.CreateDirectory(
            _context.DataDirectory);

        string safeSceneName =
            MakeSafeFileName(
                string.IsNullOrWhiteSpace(
                    snapshot.SceneName)
                    ? "unnamed-scene"
                    : snapshot.SceneName);

        string inventoryPath =
            Path.Combine(
                _context.DataDirectory,
                "DCML.ComponentInventory.Il2Cpp." +
                safeSceneName +
                ".log");

        string il2CppSection =
            FormatComponentSection(
                "Il2Cpp component types",
                snapshot.ComponentTypes
                    .Where(
                        value =>
                            value.IsIl2Cpp));

        string unitySection =
            FormatComponentSection(
                "UnityEngine component types",
                snapshot.ComponentTypes
                    .Where(
                        value =>
                            value.IsUnityEngine));

        string otherSection =
            FormatComponentSection(
                "Other component types",
                snapshot.ComponentTypes
                    .Where(
                        value =>
                            !value.IsIl2Cpp &&
                            !value.IsUnityEngine));

        string content =
            string.Join(
                Environment.NewLine,
                "DCML Data Center Component Inventory",
                $"UTC: {DateTime.UtcNow:O}",
                $"Scene: {snapshot.SceneName}",
                $"ScannedObjectCount: {snapshot.ScannedObjectCount}",
                $"PagesScanned: {snapshot.PagesScanned}",
                $"IsComplete: {snapshot.IsComplete}",
                $"UniqueComponentTypeCount: {snapshot.UniqueComponentTypeCount}",
                $"Il2CppTypeCount: {snapshot.Il2CppTypeCount}",
                $"UnityEngineTypeCount: {snapshot.UnityEngineTypeCount}",
                string.Empty,
                il2CppSection,
                string.Empty,
                unitySection,
                string.Empty,
                otherSection,
                string.Empty);

        File.WriteAllText(
            inventoryPath,
            content);

        return
            inventoryPath;
    }

    private static string FormatComponentSection(
        string title,
        System.Collections.Generic.IEnumerable<DataCenterComponentTypeInfo> types)
    {
        string[] lines =
            types
                .OrderByDescending(
                    value =>
                        value.ObjectCount)
                .ThenBy(
                    value =>
                        value.TypeName,
                    StringComparer.Ordinal)
                .Select(
                    value =>
                        value.TypeName +
                        " | Objects=" +
                        value.ObjectCount +
                        " | Active=" +
                        value.ActiveObjectCount +
                        " | Inactive=" +
                        value.InactiveObjectCount +
                        " | Examples=" +
                        string.Join(
                            " || ",
                            value.ExampleHierarchyPaths))
                .ToArray();

        if (lines.Length == 0)
        {
            return
                title +
                Environment.NewLine +
                "(none)";
        }

        return
            title +
            Environment.NewLine +
            string.Join(
                Environment.NewLine,
                lines);
    }

    private static string MakeSafeFileName(
        string value)
    {
        char[] invalid =
            Path.GetInvalidFileNameChars();

        var characters =
            value
                .Select(
                    character =>
                        invalid.Contains(
                            character)
                            ? '_'
                            : character)
                .ToArray();

        string normalized =
            new string(
                characters)
                .Trim();

        return
            normalized.Length == 0
                ? "unnamed-scene"
                : normalized;
    }

    private void EnsureInitialized()
    {
        if (_context is null)
        {
            throw new InvalidOperationException(
                "The module must be initialized before it can be started.");
        }

        if (_logger is null)
        {
            throw new InvalidOperationException(
                "The module logger is unavailable.");
        }

        if (_runtimeInfo is null)
        {
            throw new InvalidOperationException(
                "The module runtime information is unavailable.");
        }

        if (_configuration is null)
        {
            throw new InvalidOperationException(
                "The module configuration service is unavailable.");
        }

        if (_eventBus is null)
        {
            throw new InvalidOperationException(
                "The module event bus is unavailable.");
        }

        if (_gameLifecycle is null)
        {
            throw new InvalidOperationException(
                "The game lifecycle service is unavailable.");
        }

        if (_gameObjectDiscovery is null)
        {
            throw new InvalidOperationException(
                "The game object discovery service is unavailable.");
        }

        if (_dataCenterApi is null)
        {
            throw new InvalidOperationException(
                "The optional DCML.DataCenter API was not initialized.");
        }

        if (_settings is null)
        {
            throw new InvalidOperationException(
                "The module configuration was not loaded.");
        }

        if (_probeSubscription is null)
        {
            throw new InvalidOperationException(
                "The module event subscription is unavailable.");
        }

        if (_sceneSubscription is null)
        {
            throw new InvalidOperationException(
                "The module scene lifecycle subscription is unavailable.");
        }
    }

    private void AppendProof(
        string stage)
    {
        if (_context is null)
        {
            throw new InvalidOperationException(
                "The module context is unavailable.");
        }

        Directory.CreateDirectory(
            _context.DataDirectory);

        var proofPath =
            Path.Combine(
                _context.DataDirectory,
                "DCML.TestModule.lifecycle.log");

        var runtimeLines =
            _runtimeInfo is null
                ? string.Empty
                : string.Join(
                    Environment.NewLine,
                    $"RuntimeModuleId: {_runtimeInfo.ModuleId}",
                    $"DCMLVersion: {_runtimeInfo.DCMLVersion}",
                    $"Host: {_runtimeInfo.HostName} {_runtimeInfo.HostVersion}",
                    $"Game: {_runtimeInfo.GameName}",
                    $"GameRoot: {_runtimeInfo.GameRoot}",
                    $"Capabilities: {string.Join(", ", _runtimeInfo.Capabilities)}");

        var configurationLines =
            _configuration is null ||
            _settings is null
                ? string.Empty
                : string.Join(
                    Environment.NewLine,
                    $"ConfigurationPath: {_configuration.ConfigurationPath}",
                    $"ConfigurationExists: {_configuration.Exists}",
                    $"LaunchCount: {_settings.LaunchCount}",
                    $"LastLifecycleStage: {_settings.LastLifecycleStage}");

        var eventLines =
            string.Join(
                Environment.NewLine,
                $"EventsReceived: {_eventsReceived}");

        var gameLines =
            _gameLifecycle is null
                ? string.Empty
                : string.Join(
                    Environment.NewLine,
                    $"SceneEventCount: {_gameLifecycle.SceneEventCount}",
                    $"HasCurrentScene: {_gameLifecycle.HasCurrentScene}",
                    $"CurrentSceneBuildIndex: {_gameLifecycle.CurrentSceneBuildIndex}",
                    $"CurrentSceneName: {_gameLifecycle.CurrentSceneName}",
                    $"CurrentSceneStage: {_gameLifecycle.CurrentSceneStage}",
                    $"SceneEventsReceived: {_sceneEventsReceived}",
                    $"LastSceneEvent: {FormatLastSceneEvent()}");

        var discoveryLines =
            string.Join(
                Environment.NewLine,
                $"ObjectDiscoveryRuns: {_objectDiscoveryRuns}",
                $"LastObjectDiscoveryCount: {_lastObjectDiscoveryCount}",
                $"LastObjectDiscoveryScene: {_lastObjectDiscoveryScene}",
                $"LastObjectDiscoveryError: {_lastObjectDiscoveryError}",
                $"LastObjectDiscoverySample: {_lastObjectDiscoverySample}");

        var recommendedApiLines =
            string.Join(
                Environment.NewLine,
                $"RecommendedApiRuns: {_recommendedApiRuns}",
                $"LastRecommendedEntityCount: {_lastRecommendedEntityCount}",
                $"LastRecommendedScene: {_lastRecommendedScene}",
                $"LastRecommendedKinds: {_lastRecommendedKinds}",
                $"LastRecommendedError: {_lastRecommendedError}",
                $"LastRecommendedSample: {_lastRecommendedSample}");

        var componentInventoryLines =
            string.Join(
                Environment.NewLine,
                $"ComponentInventoryRuns: {_componentInventoryRuns}",
                $"LastComponentInventoryObjectCount: {_lastComponentInventoryObjectCount}",
                $"LastComponentInventoryTypeCount: {_lastComponentInventoryTypeCount}",
                $"LastComponentInventoryIl2CppTypeCount: {_lastComponentInventoryIl2CppTypeCount}",
                $"LastComponentInventoryUnityTypeCount: {_lastComponentInventoryUnityTypeCount}",
                $"LastComponentInventoryScene: {_lastComponentInventoryScene}",
                $"LastComponentInventoryPagesScanned: {_lastComponentInventoryPagesScanned}",
                $"LastComponentInventoryComplete: {_lastComponentInventoryComplete}",
                $"LastComponentInventoryPath: {_lastComponentInventoryPath}",
                $"LastComponentInventoryError: {_lastComponentInventoryError}",
                $"LastComponentInventoryIl2CppSample: {_lastComponentInventoryIl2CppSample}");

        var entry =
            string.Join(
                Environment.NewLine,
                stage,
                $"UTC: {DateTime.UtcNow:O}",
                $"ModuleDirectory: {_context.ModuleDirectory}",
                $"DataDirectory: {_context.DataDirectory}",
                runtimeLines,
                configurationLines,
                eventLines,
                gameLines,
                discoveryLines,
                recommendedApiLines,
                componentInventoryLines,
                string.Empty,
                string.Empty);

        File.AppendAllText(
            proofPath,
            entry);
    }

    private string FormatLastSceneEvent()
    {
        if (_lastSceneEvent is null)
        {
            return "None";
        }

        return
            $"{_lastSceneEvent.Stage}|" +
            $"{_lastSceneEvent.BuildIndex}|" +
            $"{_lastSceneEvent.SceneName}|" +
            $"{_lastSceneEvent.Sequence}";
    }

    private sealed class ProbeEvent
    {
        public ProbeEvent(
            string sourceModuleId,
            string stage)
        {
            SourceModuleId =
                sourceModuleId;

            Stage =
                stage;
        }

        public string SourceModuleId { get; }

        public string Stage { get; }
    }

    private sealed class ProbeSettings
    {
        public int LaunchCount { get; set; }

        public string LastLifecycleStage { get; set; } =
            "Never";

        public string LastDCMLVersion { get; set; } =
            string.Empty;
    }
}
