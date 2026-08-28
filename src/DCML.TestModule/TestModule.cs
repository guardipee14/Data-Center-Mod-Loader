using System;
using System.IO;
using DCML.Core.Abstractions;
using DCML.Core.Models;

namespace DCML.TestModule;

public sealed class TestModule : IDCMLModule
{
    private IDCMLModuleContext? _context;

    private IDCMLLogger? _logger;

    private IDCMLRuntimeInfo? _runtimeInfo;

    private IDCMLConfiguration? _configuration;

    private IDCMLEventBus? _eventBus;

    private IDCMLGameLifecycle? _gameLifecycle;

    private IDisposable? _probeSubscription;

    private IDisposable? _sceneSubscription;

    private ProbeSettings? _settings;

    private int _eventsReceived;

    private int _sceneEventsReceived;

    private DCMLSceneLifecycleEvent? _lastSceneEvent;

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
            DCMLRuntimeCapabilities.GameSceneLifecycle
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
