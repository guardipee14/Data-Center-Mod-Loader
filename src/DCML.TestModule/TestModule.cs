using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using DCML.DataCenter;
using DCML.DataCenter.Abstractions;
using DCML.DataCenter.Models;

namespace DCML.TestModule;

public sealed class TestModule : IDCMLModule
{
    private static readonly string[] GameTypeKeywords =
    {
        "Server",
        "Rack",
        "Switch",
        "Router",
        "Firewall",
        "Device",
        "Port",
        "SFP",
        "QSFP",
        "Cable",
        "Factory",
        "Machine",
        "Hacking",
        "Coding",
        "Packet"
    };

    private static readonly string[] InspectedGameTypeNames =
    {
        "Il2Cpp.Server",
        "Il2Cpp.Rack",
        "Il2Cpp.NetworkSwitch",
        "Il2Cpp.Router",
        "Il2Cpp.Firewall",
        "Il2Cpp.SFPModule",
        "Il2Cpp.CableLink",
        "Il2Cpp.CustomerBase",
        "Il2Cpp.CustomerItem",
        "Il2Cpp.CustomerBaseSaveData",
        "Il2Cpp.INetworkEndpoint",
        "Il2Cpp.ITimedDevice"
    };

    private const int DefaultAutomaticSceneDiagnosticDelayFrames =
        600;

    private const int SafeAutomaticSceneDiagnosticStageCount =
        3;

    private const int HeavyAutomaticSceneDiagnosticStageCount =
        8;

    private IDCMLModuleContext? _context;

    private IDCMLLogger? _logger;

    private IDCMLRuntimeInfo? _runtimeInfo;

    private IDCMLConfiguration? _configuration;

    private IDCMLEventBus? _eventBus;

    private IDCMLGameLifecycle? _gameLifecycle;

    private IDCMLGameObjectDiscovery? _gameObjectDiscovery;

    private IDCMLGameTypeCatalog? _gameTypeCatalog;

    private IDCMLGameResourceDiscovery? _gameResourceDiscovery;

    private IDCMLGameTypeInspector? _gameTypeInspector;

    private IDCMLGameThread? _gameThread;

    private IDCMLGameComponentStateReader? _gameComponentStateReader;

    private DataCenterApi? _dataCenterApi;

    private IDataCenterCablePersistenceSource?
        _cablePersistenceSource;

    private IDisposable? _probeSubscription;

    private IDisposable? _sceneSubscription;

    private ProbeSettings? _settings;

    private int _automaticSceneDiagnosticGeneration;

    private bool _automaticSceneDiagnosticPending;

    private int _automaticSceneDiagnosticFramesRemaining;

    private int _automaticSceneDiagnosticStage;

    private int _automaticSceneDiagnosticSchedules;

    private int _automaticSceneDiagnosticCompletions;

    private int _automaticSceneDiagnosticCancellations;

    private string _automaticSceneDiagnosticScene =
        string.Empty;

    private string _automaticSceneDiagnosticLastError =
        string.Empty;

    private int _cablePersistenceMetadataProbeGeneration;

    private bool _cablePersistenceMetadataProbePending;

    private int _cablePersistenceMetadataProbeFramesRemaining;

    private string _cablePersistenceMetadataProbeScene =
        string.Empty;

    private int _physicalCablePersistenceSourceProbeRuns;
    private bool _physicalCablePersistenceSourceProbeRunning;
    private int _lastPhysicalCablePersistenceCableCount;
    private int _lastPhysicalCablePersistenceEndpointCount;
    private int _lastPhysicalCablePersistenceResolvedEndpointCount;
    private int _lastPhysicalCablePersistenceUnresolvedEndpointCount;
    private int _lastPhysicalCablePersistenceNetworkEdgeCount;
    private string _lastPhysicalCablePersistenceSourcePath =
        string.Empty;
    private string _lastPhysicalCablePersistenceError =
        string.Empty;

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

    private int _targetedSemanticRuns;

    private string _lastTargetedSemanticScene =
        string.Empty;

    private string _lastTargetedSemanticCounts =
        string.Empty;

    private string _lastTargetedSemanticAtLimit =
        string.Empty;

    private string _lastTargetedSemanticError =
        string.Empty;

    private string _lastTargetedSemanticSample =
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

    private int _gameTypeCatalogRuns;

    private int _lastGameTypeCatalogTypeCount;

    private bool _lastGameTypeCatalogAtResultLimit;

    private string _lastGameTypeCatalogScene =
        string.Empty;

    private string _lastGameTypeCatalogPath =
        string.Empty;

    private string _lastGameTypeCatalogKeywordCounts =
        string.Empty;

    private string _lastGameTypeCatalogError =
        string.Empty;

    private string _lastGameTypeCatalogSample =
        string.Empty;

    private int _gameResourceDiscoveryRuns;

    private string _lastGameResourceDiscoveryScene =
        string.Empty;

    private int _lastGameResourceDiscoveryServerCount;

    private int _lastGameResourceDiscoveryRackCount;

    private int _lastGameResourceDiscoveryNetworkDeviceCount;

    private int _lastGameResourceDiscoveryCableCount;

    private string _lastGameResourceDiscoveryError =
        string.Empty;

    private string _lastGameResourceDiscoverySample =
        string.Empty;

    private int _gameTypeInspectionRuns;

    private string _lastGameTypeInspectionScene =
        string.Empty;

    private int _lastGameTypeInspectionTypeCount;

    private int _lastGameTypeInspectionMemberCount;

    private string _lastGameTypeInspectionAtLimit =
        string.Empty;

    private string _lastGameTypeInspectionPath =
        string.Empty;

    private string _lastGameTypeInspectionError =
        string.Empty;

    private string _lastGameTypeInspectionSummary =
        string.Empty;

    private int _cablePersistenceMetadataProbeRuns;

    private string _lastCablePersistenceMetadataProbeScene =
        string.Empty;

    private int _lastCablePersistenceMetadataCandidateTypeCount;

    private int _lastCablePersistenceMetadataInspectedTypeCount;

    private int _lastCablePersistenceMetadataRelevantMemberCount;

    private string _lastCablePersistenceMetadataPath =
        string.Empty;

    private string _lastCablePersistenceMetadataCandidateTypes =
        string.Empty;

    private string _lastCablePersistenceMetadataRelevantMembers =
        string.Empty;

    private string _lastCablePersistenceMetadataError =
        string.Empty;

    private int _gameThreadProbeRuns;

    private bool _lastGameThreadInitializeWasMainThread;

    private bool _lastGameThreadBackgroundWasMainThread;

    private bool _lastGameThreadPostWasMainThread;

    private bool _lastGameThreadInvokeWasMainThread;

    private int _lastGameThreadPostCount;

    private int _lastGameThreadInvokeCount;

    private string _lastGameThreadError =
        string.Empty;

    private int _hardwareSnapshotRuns;

    private string _lastHardwareSnapshotScene =
        string.Empty;

    private int _lastHardwareSnapshotServerCount;

    private int _lastHardwareSnapshotRackCount;

    private int _lastHardwareSnapshotNetworkDeviceCount;

    private int _lastHardwareSnapshotSfpCount;

    private int _lastHardwareSnapshotCableCount;

    private int _lastHardwareSnapshotServerDefinitionCount;
    private int _lastHardwareSnapshotServerInstanceCount;
    private int _lastHardwareSnapshotRackDefinitionCount;
    private int _lastHardwareSnapshotRackInstanceCount;
    private int _lastHardwareSnapshotNetworkDeviceDefinitionCount;
    private int _lastHardwareSnapshotNetworkDeviceInstanceCount;
    private int _lastHardwareSnapshotSfpDefinitionCount;
    private int _lastHardwareSnapshotSfpInstanceCount;
    private int _lastHardwareSnapshotCableDefinitionCount;
    private int _lastHardwareSnapshotCableInstanceCount;

    private int _lastHardwareSnapshotSfpLinkedCount;
    private int _lastHardwareSnapshotCableParentServerCount;
    private int _lastHardwareSnapshotCableParentSwitchCount;
    private int _lastHardwareSnapshotCableParentPatchPanelCount;
    private int _lastHardwareSnapshotCableParentInternetCount;
    private int _lastHardwareSnapshotCableInsertedSfpCount;

    private string _lastHardwareRelationshipSample =
        string.Empty;

    private int _lastHardwareTopologyNodeCount;
    private int _lastHardwareTopologyEdgeCount;
    private int _lastHardwareTopologyResolvedEdgeCount;
    private int _lastHardwareTopologyUnresolvedEdgeCount;
    private int _lastHardwareTopologyCableSearchPages;
    private int _lastHardwareTopologyCableCandidatesScanned;
    private bool _lastHardwareTopologyCableSearchExhausted;
    private int _lastHardwareTopologyNonSceneSearchPages;
    private int _lastHardwareTopologyNonSceneCandidatesScanned;
    private int _lastHardwareTopologyNonSceneTargetMatchCount;
    private bool _lastHardwareTopologyNonSceneSearchExhausted;
    private int _lastHardwareTopologyTargetCableDetailRequestedCount;
    private int _lastHardwareTopologyTargetCableDetailFoundCount;
    private int _lastHardwareTopologyTargetCableParentServerCount;
    private int _lastHardwareTopologyTargetCableParentSwitchCount;
    private int _lastHardwareTopologyTargetCableParentPatchPanelCount;
    private int _lastHardwareTopologyTargetCableParentInternetCount;
    private int _lastHardwareTopologyTargetCableInsertedSfpCount;
    private int _lastHardwareTopologyTargetCableSfpPortCount;
    private int _lastHardwareTopologyTargetCableEndpointCount;

    private int _lastHardwareTopologyTargetHierarchyTargetCount;
    private int _lastHardwareTopologyTargetHierarchyMatchedTargetCount;
    private int _lastHardwareTopologyTargetHierarchyObjectCount;
    private int _lastHardwareTopologyTargetHierarchyServerAncestorCount;
    private int _lastHardwareTopologyTargetHierarchyNetworkDeviceAncestorCount;
    private int _lastHardwareTopologyTargetHierarchyPatchPanelAncestorCount;
    private int _lastHardwareTopologyTargetHierarchyInternetAncestorCount;
    private int _lastHardwareTopologyTargetHierarchyRackAncestorCount;

    private string _lastHardwareTopologyTargetHierarchySample =
        string.Empty;

    private string _lastHardwareTopologyTargetHierarchyError =
        string.Empty;

    private int _lastHardwareTopologyCustomerBaseRootCount;
    private int _lastHardwareTopologyCustomerBaseSubtreeObjectCount;
    private int _lastHardwareTopologyCustomerBaseSubtreeIl2CppTypeCount;
    private int _lastHardwareTopologyCustomerBaseSubtreeNetworkSwitchCount;
    private int _lastHardwareTopologyCustomerBaseSubtreeRouterCount;
    private int _lastHardwareTopologyCustomerBaseSubtreeFirewallCount;
    private int _lastHardwareTopologyCustomerBaseSubtreeServerCount;
    private int _lastHardwareTopologyCustomerBaseSubtreePatchPanelCount;
    private int _lastHardwareTopologyCustomerBaseSubtreeInternetCount;
    private int _lastHardwareTopologyCustomerBaseSubtreeRackCount;
    private int _lastHardwareTopologyCustomerBaseSubtreeCableLinkCount;
    private bool _lastHardwareTopologyCustomerBaseSubtreeAtResultLimit;

    private string _lastHardwareTopologyCustomerBaseSubtreeTypes =
        string.Empty;

    private string _lastHardwareTopologyCustomerBaseSubtreeSample =
        string.Empty;

    private string _lastHardwareTopologyCustomerBaseSubtreeError =
        string.Empty;

    private int _lastCustomerBaseStateProbeComponentCount;
    private int _lastCustomerBaseStateProbeFieldCount;
    private int _lastCustomerBaseStateProbePropertyCount;
    private int _lastCustomerBaseStateProbeValueCount;
    private int _lastCustomerBaseStateProbeReferenceCount;
    private int _lastCustomerBaseStateProbeScalarCount;
    private int _lastCustomerBaseStateProbeNullCount;
    private int _lastCustomerBaseStateProbeUnsupportedCount;
    private int _lastCustomerBaseStateProbeUnavailableCount;

    private string _lastCustomerBaseStateProbeFields =
        string.Empty;

    private string _lastCustomerBaseStateProbeProperties =
        string.Empty;

    private string _lastCustomerBaseRelatedTypeSummary =
        string.Empty;

    private string _lastCustomerBaseStateProbeReferenceTypes =
        string.Empty;

    private string _lastCustomerBaseStateProbeUnsupportedTypes =
        string.Empty;

    private string _lastCustomerBaseStateProbeSample =
        string.Empty;

    private string _lastCustomerBaseStateProbeError =
        string.Empty;

    private int _lastCustomerBaseCableLinkCollectionBaseCount;
    private int _lastCustomerBaseCableLinkCollectionDeclaredCount;
    private int _lastCustomerBaseCableLinkCollectionReferenceCount;
    private int _lastCustomerBaseCableLinkCollectionUniqueReferenceCount;
    private int _lastCustomerBaseCableLinkCollectionTopologyTargetCount;
    private int _lastCustomerBaseCableLinkCollectionTopologyTargetMatchCount;
    private int _lastCustomerBaseCableLinkCollectionNonTargetReferenceCount;

    private string _lastCustomerBaseCableLinkCollectionSample =
        string.Empty;

    private string _lastHardwareTopologySample =
        string.Empty;

    private string _lastHardwareTopologyError =
        string.Empty;

    private string _lastHardwareSnapshotError =
        string.Empty;

    private string _lastHardwareSnapshotSample =
        string.Empty;

    public string Id =>
        "dcml.test.lifecycle";

    public string Name =>
        "DCML Lifecycle Test Module";

    public string Version =>
        DCML.Core.DCMLVersion.Current;

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

        _gameTypeCatalog =
            context.Services.GetService(
                typeof(IDCMLGameTypeCatalog))
            as IDCMLGameTypeCatalog;

        _gameResourceDiscovery =
            context.Services.GetService(
                typeof(IDCMLGameResourceDiscovery))
            as IDCMLGameResourceDiscovery;

        _gameTypeInspector =
            context.Services.GetService(
                typeof(IDCMLGameTypeInspector))
            as IDCMLGameTypeInspector;

        _gameThread =
            context.Services.GetService(
                typeof(IDCMLGameThread))
            as IDCMLGameThread;

        _gameComponentStateReader =
            context.Services.GetService(
                typeof(IDCMLGameComponentStateReader))
            as IDCMLGameComponentStateReader;

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

        if (_gameTypeCatalog is null)
        {
            throw new InvalidOperationException(
                "DCML did not provide IDCMLGameTypeCatalog through the module service provider.");
        }

        if (_gameResourceDiscovery is null)
        {
            throw new InvalidOperationException(
                "DCML did not provide IDCMLGameResourceDiscovery through the module service provider.");
        }

        if (_gameTypeInspector is null)
        {
            throw new InvalidOperationException(
                "DCML did not provide IDCMLGameTypeInspector through the module service provider.");
        }

        if (_gameThread is null)
        {
            throw new InvalidOperationException(
                "DCML did not provide IDCMLGameThread through the module service provider.");
        }

        if (_gameComponentStateReader is null)
        {
            throw new InvalidOperationException(
                "DCML did not provide IDCMLGameComponentStateReader through the module service provider.");
        }

        _lastGameThreadInitializeWasMainThread =
            _gameThread.IsMainThread;

        _settings =
            _configuration.Load(
                new ProbeSettings());

        _cablePersistenceSource =
            CreateCablePersistenceSource(
                _settings);

        _dataCenterApi =
            DataCenterApi.Create(
                context,
                _cablePersistenceSource);

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
            DCMLRuntimeCapabilities.GameObjectDiscovery,
            DCMLRuntimeCapabilities.GameTypeCatalog,
            DCMLRuntimeCapabilities.GameResourceDiscovery,
            DCMLRuntimeCapabilities.GameTypeInspection,
            DCMLRuntimeCapabilities.GameMainThread,
            DCMLRuntimeCapabilities.GameComponentState
        })
        {
            if (!_runtimeInfo.HasCapability(capability))
            {
                throw new InvalidOperationException(
                    $"DCML runtime information did not advertise '{capability}'.");
            }
        }

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
            "Game type catalog service registered.");

        _logger.Info(
            "Game resource discovery service registered.");

        _logger.Info(
            "Game type inspection service registered.");

        _logger.Info(
            "Game main-thread scheduler service registered.");

        _logger.Info(
            "Game component-state reader service registered.");

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

        RunGameThreadProbe();

        RunPhysicalCablePersistenceSourceProbe();

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

        CancelAutomaticSceneDiagnostics(
            "Module stopping.");

        CancelCablePersistenceMetadataProbe(
            "Module stopping.");

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

    private IDataCenterCablePersistenceSource?
        CreateCablePersistenceSource(
            ProbeSettings settings)
    {
        if (
            !settings.EnablePhysicalCablePersistenceSource ||
            string.IsNullOrWhiteSpace(
                settings.PhysicalCableSavePath) ||
            string.IsNullOrWhiteSpace(
                settings.PhysicalCableHelperHostPath) ||
            string.IsNullOrWhiteSpace(
                settings.PhysicalCableHelperDllPath)
        )
        {
            return null;
        }

        return new ProcessCablePersistenceSource(
            settings.PhysicalCableHelperHostPath,
            settings.PhysicalCableHelperDllPath,
            settings.PhysicalCableSavePath);
    }

    private void RunPhysicalCablePersistenceSourceProbe()
    {
        if (
            _settings?.EnablePhysicalCablePersistenceSourceProbe != true ||
            _cablePersistenceSource is null ||
            _context is null ||
            _physicalCablePersistenceSourceProbeRunning
        )
        {
            return;
        }

        _physicalCablePersistenceSourceProbeRunning = true;
        _physicalCablePersistenceSourceProbeRuns++;
        _lastPhysicalCablePersistenceError = string.Empty;
        _lastPhysicalCablePersistenceSourcePath =
            _cablePersistenceSource.SourcePath;

        _ = Task.Run(
            async () =>
            {
                try
                {
                    DataCenterCablePersistenceSnapshot persistence =
                        await _cablePersistenceSource
                            .ReadAsync()
                            .ConfigureAwait(false);

                    DataCenterHardwareTopologyGraph graph =
                        DataCenterPhysicalCableTopology.Build(
                            persistence.Cables,
                            persistence.Index);

                    _lastPhysicalCablePersistenceCableCount =
                        persistence.CableCount;
                    _lastPhysicalCablePersistenceEndpointCount =
                        persistence.EndpointCount;
                    _lastPhysicalCablePersistenceResolvedEndpointCount =
                        persistence.ResolvedEndpointCount;
                    _lastPhysicalCablePersistenceUnresolvedEndpointCount =
                        persistence.UnresolvedEndpointCount;
                    _lastPhysicalCablePersistenceNetworkEdgeCount =
                        graph.NetworkConnectionEdges.Count;

                    WritePhysicalCablePersistenceReport(
                        "Complete",
                        persistence,
                        graph);
                }
                catch (Exception exception)
                {
                    _lastPhysicalCablePersistenceError =
                        exception.GetType().FullName +
                        ": " +
                        exception.Message;

                    WritePhysicalCablePersistenceReport(
                        "Failed",
                        null,
                        null);
                }
                finally
                {
                    _physicalCablePersistenceSourceProbeRunning = false;

                    if (
                        _gameThread is not null &&
                        _settings is not null &&
                        _configuration is not null
                    )
                    {
                        _gameThread.Post(
                            () =>
                            {
                                _settings.EnablePhysicalCablePersistenceSourceProbe =
                                    false;

                                _configuration.Save(
                                    _settings);
                            });
                    }
                }
            });
    }

    private void WritePhysicalCablePersistenceReport(
        string status,
        DataCenterCablePersistenceSnapshot? persistence,
        DataCenterHardwareTopologyGraph? graph)
    {
        if (_context is null)
        {
            return;
        }

        Directory.CreateDirectory(
            _context.DataDirectory);

        string report =
            Path.Combine(
                _context.DataDirectory,
                "DCML.PhysicalCablePersistenceSource.log");

        File.WriteAllLines(
            report,
            new[]
            {
                "DCML Physical Cable Persistence Source",
                "Status: " + status,
                "UTC: " + DateTime.UtcNow.ToString("O"),
                "SourceMode: OutOfProcessJson",
                "SourcePath: " + _lastPhysicalCablePersistenceSourcePath,
                "CableCount: " + _lastPhysicalCablePersistenceCableCount,
                "EndpointCount: " + _lastPhysicalCablePersistenceEndpointCount,
                "ResolvedEndpointCount: " + _lastPhysicalCablePersistenceResolvedEndpointCount,
                "UnresolvedEndpointCount: " + _lastPhysicalCablePersistenceUnresolvedEndpointCount,
                "NetworkConnectionEdgeCount: " + _lastPhysicalCablePersistenceNetworkEdgeCount,
                "PhysicalCableEdgeCount: " +
                    (graph?.PhysicalCableEdges.Count.ToString() ?? string.Empty),
                "AllEdgesBidirectional: " +
                    (
                        graph is null
                            ? string.Empty
                            : graph.PhysicalCableEdges.All(
                                value => value.IsBidirectional)
                                .ToString()
                    ),
                "Error: " + _lastPhysicalCablePersistenceError
            });
    }

    private sealed class ProcessCablePersistenceSource :
        IDataCenterCablePersistenceSource
    {
        private readonly string _hostPath;
        private readonly string _helperDllPath;

        public ProcessCablePersistenceSource(
            string hostPath,
            string helperDllPath,
            string savePath)
        {
            _hostPath =
                Path.GetFullPath(hostPath);
            _helperDllPath =
                Path.GetFullPath(helperDllPath);
            SourcePath =
                Path.GetFullPath(savePath);
        }

        public string SourcePath { get; }

        public async Task<DataCenterCablePersistenceSnapshot> ReadAsync()
        {
            var startInfo =
                new ProcessStartInfo
                {
                    FileName = _hostPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory =
                        Path.GetDirectoryName(
                            _helperDllPath) ??
                        string.Empty
                };

            startInfo.ArgumentList.Add(
                _helperDllPath);

            startInfo.ArgumentList.Add(
                SourcePath);

            using Process process =
                new();

            process.StartInfo =
                startInfo;

            if (!process.Start())
            {
                throw new InvalidOperationException(
                    "The persistence helper process did not start.");
            }

            string stdout =
                await process.StandardOutput
                    .ReadToEndAsync()
                    .ConfigureAwait(false);

            string stderr =
                await process.StandardError
                    .ReadToEndAsync()
                    .ConfigureAwait(false);

            await process.WaitForExitAsync()
                .ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    "Persistence helper failed with exit code " +
                    process.ExitCode +
                    ": " +
                    stderr);
            }

            HelperSnapshot? raw =
                JsonSerializer.Deserialize<HelperSnapshot>(
                    stdout);

            if (raw is null)
            {
                throw new InvalidDataException(
                    "Persistence helper returned no JSON snapshot.");
            }

            DataCenterCablePersistenceRecord[] cables =
                raw.Cables
                    .Select(
                        cable =>
                            new DataCenterCablePersistenceRecord(
                                cable.CableID,
                                ToEndpoint(
                                    DataCenterPhysicalCableEndpointSide.Start,
                                    cable.Start),
                                ToEndpoint(
                                    DataCenterPhysicalCableEndpointSide.End,
                                    cable.End)))
                    .ToArray();

            return new DataCenterCablePersistenceSnapshot(
                raw.SourcePath,
                raw.SourceLength,
                raw.SourceLastWriteTimeUtc,
                cables,
                raw.ServerIDs,
                raw.SwitchIDs,
                raw.RouterIDs,
                raw.FirewallIDs,
                raw.PatchPanelIDs,
                raw.CustomerIDs);
        }

        private static DataCenterCablePersistenceEndpoint ToEndpoint(
            DataCenterPhysicalCableEndpointSide side,
            HelperEndpoint endpoint)
        {
            return new DataCenterCablePersistenceEndpoint(
                side,
                endpoint.LinkType,
                endpoint.ServerID,
                endpoint.SwitchID,
                endpoint.CustomerID,
                endpoint.Position);
        }

        private sealed class HelperSnapshot
        {
            public string SourcePath { get; set; } =
                string.Empty;
            public long SourceLength { get; set; }
            public DateTime SourceLastWriteTimeUtc { get; set; }
            public int NetworkSaveDataCount { get; set; }
            public HelperCable[] Cables { get; set; } =
                Array.Empty<HelperCable>();
            public string[] ServerIDs { get; set; } =
                Array.Empty<string>();
            public string[] SwitchIDs { get; set; } =
                Array.Empty<string>();
            public string[] RouterIDs { get; set; } =
                Array.Empty<string>();
            public string[] FirewallIDs { get; set; } =
                Array.Empty<string>();
            public string[] PatchPanelIDs { get; set; } =
                Array.Empty<string>();
            public int[] CustomerIDs { get; set; } =
                Array.Empty<int>();
        }

        private sealed class HelperCable
        {
            public int CableID { get; set; }
            public HelperEndpoint Start { get; set; } =
                new();
            public HelperEndpoint End { get; set; } =
                new();
        }

        private sealed class HelperEndpoint
        {
            public int LinkType { get; set; }
            public string ServerID { get; set; } =
                string.Empty;
            public string SwitchID { get; set; } =
                string.Empty;
            public int? CustomerID { get; set; }
            public string Position { get; set; } =
                string.Empty;
        }
    }

    private void RunGameThreadProbe()
    {
        if (_gameThread is null)
        {
            return;
        }

        _gameThreadProbeRuns++;
        _lastGameThreadError =
            string.Empty;

        try
        {
            _gameThread.Post(
                () =>
                {
                    _lastGameThreadPostCount++;
                    _lastGameThreadPostWasMainThread =
                        _gameThread.IsMainThread;

                    AppendProof(
                        "GameThreadPost");
                });

            _ = Task.Run(
                async () =>
                {
                    try
                    {
                        _lastGameThreadBackgroundWasMainThread =
                            _gameThread.IsMainThread;

                        await _gameThread.InvokeAsync(
                            () =>
                            {
                                _lastGameThreadInvokeCount++;
                                _lastGameThreadInvokeWasMainThread =
                                    _gameThread.IsMainThread;

                                AppendProof(
                                    "GameThreadInvoke");
                            })
                            .ConfigureAwait(
                                false);
                    }
                    catch (Exception exception)
                    {
                        _lastGameThreadError =
                            exception.GetType().FullName +
                            ": " +
                            exception.Message;

                        try
                        {
                            await _gameThread.InvokeAsync(
                                () =>
                                    AppendProof(
                                        "GameThreadError"))
                                .ConfigureAwait(
                                    false);
                        }
                        catch
                        {
                            // The diagnostic must not affect the module.
                        }
                    }
                });
        }
        catch (Exception exception)
        {
            _lastGameThreadError =
                exception.GetType().FullName +
                ": " +
                exception.Message;

            AppendProof(
                "GameThreadError");
        }
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
                DCMLSceneLifecycleStage.Unloaded
        )
        {
            CancelAutomaticSceneDiagnostics(
                "Scene unloaded.");

            CancelCablePersistenceMetadataProbe(
                "Scene unloaded.");

            return;
        }

        if (
            eventData.Stage !=
                DCMLSceneLifecycleStage.Initialized ||
            string.IsNullOrWhiteSpace(
                eventData.SceneName)
        )
        {
            return;
        }

        if (
            _settings?.EnableCablePersistenceMetadataProbe ==
                true
        )
        {
            ScheduleCablePersistenceMetadataProbe(
                eventData.SceneName);
        }

        if (
            _settings?.EnableAutomaticSceneDiagnostics !=
                true
        )
        {
            _logger?.Info(
                "Automatic scene diagnostics are disabled. " +
                "The scene callback returned without running discovery or hardware scans.");

            return;
        }

        ScheduleAutomaticSceneDiagnostics(
            eventData.SceneName);
    }

    private void ScheduleAutomaticSceneDiagnostics(
        string sceneName)
    {
        if (
            _gameThread is null ||
            _settings is null
        )
        {
            return;
        }

        int generation =
            ++_automaticSceneDiagnosticGeneration;

        _automaticSceneDiagnosticPending =
            true;

        _automaticSceneDiagnosticScene =
            sceneName;

        _automaticSceneDiagnosticFramesRemaining =
            Math.Max(
                1,
                _settings.SceneDiagnosticDelayFrames);

        _automaticSceneDiagnosticStage =
            0;

        _automaticSceneDiagnosticSchedules++;

        _automaticSceneDiagnosticLastError =
            string.Empty;

        _logger?.Info(
            $"Automatic scene diagnostics scheduled for '{sceneName}' " +
            $"after {_automaticSceneDiagnosticFramesRemaining} update frame(s). " +
            $"Heavy scans enabled: {_settings.EnableHeavyAutomaticSceneDiagnostics}.");

        _gameThread.Post(
            () =>
                AdvanceAutomaticSceneDiagnostics(
                    generation));
    }

    private void AdvanceAutomaticSceneDiagnostics(
        int generation)
    {
        if (
            !_automaticSceneDiagnosticPending ||
            generation !=
                _automaticSceneDiagnosticGeneration ||
            _gameThread is null ||
            _settings is null
        )
        {
            return;
        }

        if (
            _gameLifecycle is null ||
            !_gameLifecycle.HasCurrentScene ||
            !string.Equals(
                _gameLifecycle.CurrentSceneName,
                _automaticSceneDiagnosticScene,
                StringComparison.OrdinalIgnoreCase)
        )
        {
            CancelAutomaticSceneDiagnostics(
                "The active scene changed before diagnostics completed.");

            return;
        }

        if (
            _automaticSceneDiagnosticFramesRemaining >
                0
        )
        {
            _automaticSceneDiagnosticFramesRemaining--;

            _gameThread.Post(
                () =>
                    AdvanceAutomaticSceneDiagnostics(
                        generation));

            return;
        }

        try
        {
            RunAutomaticSceneDiagnosticStage(
                _automaticSceneDiagnosticScene,
                _automaticSceneDiagnosticStage);

            _automaticSceneDiagnosticStage++;

            int stageCount =
                _settings.EnableHeavyAutomaticSceneDiagnostics
                    ? HeavyAutomaticSceneDiagnosticStageCount
                    : SafeAutomaticSceneDiagnosticStageCount;

            if (
                _automaticSceneDiagnosticStage >=
                    stageCount
            )
            {
                _automaticSceneDiagnosticPending =
                    false;

                _automaticSceneDiagnosticCompletions++;

                _logger?.Info(
                    $"Automatic scene diagnostics completed for '{_automaticSceneDiagnosticScene}'.");

                AppendProof(
                    "AutomaticSceneDiagnosticsCompleted");

                return;
            }

            _gameThread.Post(
                () =>
                    AdvanceAutomaticSceneDiagnostics(
                        generation));
        }
        catch (Exception exception)
        {
            _automaticSceneDiagnosticLastError =
                exception.GetType().FullName +
                ": " +
                exception.Message;

            CancelAutomaticSceneDiagnostics(
                "A diagnostic stage failed.");

            _logger?.Error(
                "Automatic scene diagnostics failed: " +
                _automaticSceneDiagnosticLastError);
        }
    }

    private void RunAutomaticSceneDiagnosticStage(
        string sceneName,
        int stage)
    {
        switch (stage)
        {
            case 0:
                RunObjectDiscovery(
                    sceneName);

                break;

            case 1:
                RunGameTypeCatalog(
                    sceneName);

                break;

            case 2:
                RunGameResourceDiscovery(
                    sceneName);

                break;

            case 3:
                RunRecommendedDataCenterApi(
                    sceneName);

                break;

            case 4:
                RunTargetedSemanticDiscovery(
                    sceneName);

                break;

            case 5:
                RunComponentInventory(
                    sceneName);

                break;

            case 6:
                RunGameTypeInspection(
                    sceneName);

                break;

            case 7:
                _ =
                    RunHardwareSnapshotsAsync(
                        sceneName);

                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported automatic scene diagnostic stage {stage}.");
        }
    }

    private void CancelAutomaticSceneDiagnostics(
        string reason)
    {
        if (!_automaticSceneDiagnosticPending)
        {
            _automaticSceneDiagnosticGeneration++;

            return;
        }

        _automaticSceneDiagnosticPending =
            false;

        _automaticSceneDiagnosticGeneration++;

        _automaticSceneDiagnosticCancellations++;

        _logger?.Info(
            "Automatic scene diagnostics canceled. " +
            reason);
    }

    private void ScheduleCablePersistenceMetadataProbe(
        string sceneName)
    {
        if (
            _gameThread is null ||
            _settings is null
        )
        {
            return;
        }

        int generation =
            ++_cablePersistenceMetadataProbeGeneration;

        _cablePersistenceMetadataProbePending =
            true;

        _cablePersistenceMetadataProbeScene =
            sceneName;

        _cablePersistenceMetadataProbeFramesRemaining =
            Math.Max(
                1,
                _settings.CablePersistenceProbeDelayFrames);

        _logger?.Info(
            $"Cable persistence metadata probe scheduled for '{sceneName}' " +
            $"after {_cablePersistenceMetadataProbeFramesRemaining} update frame(s). " +
            "The probe uses type metadata only.");

        _gameThread.Post(
            () =>
                AdvanceCablePersistenceMetadataProbe(
                    generation));
    }

    private void AdvanceCablePersistenceMetadataProbe(
        int generation)
    {
        if (
            !_cablePersistenceMetadataProbePending ||
            generation !=
                _cablePersistenceMetadataProbeGeneration ||
            _gameThread is null ||
            _settings is null
        )
        {
            return;
        }

        if (
            _gameLifecycle is null ||
            !_gameLifecycle.HasCurrentScene ||
            !string.Equals(
                _gameLifecycle.CurrentSceneName,
                _cablePersistenceMetadataProbeScene,
                StringComparison.OrdinalIgnoreCase)
        )
        {
            CancelCablePersistenceMetadataProbe(
                "The active scene changed before the metadata probe ran.");

            return;
        }

        if (
            _cablePersistenceMetadataProbeFramesRemaining >
                0
        )
        {
            _cablePersistenceMetadataProbeFramesRemaining--;

            _gameThread.Post(
                () =>
                    AdvanceCablePersistenceMetadataProbe(
                        generation));

            return;
        }

        _cablePersistenceMetadataProbePending =
            false;

        RunCablePersistenceMetadataProbe(
            _cablePersistenceMetadataProbeScene);

        if (
            _configuration is not null &&
            _settings is not null
        )
        {
            _settings.EnableCablePersistenceMetadataProbe =
                false;

            _configuration.Save(
                _settings);
        }
    }

    private void CancelCablePersistenceMetadataProbe(
        string reason)
    {
        if (!_cablePersistenceMetadataProbePending)
        {
            _cablePersistenceMetadataProbeGeneration++;

            return;
        }

        _cablePersistenceMetadataProbePending =
            false;

        _cablePersistenceMetadataProbeGeneration++;

        _logger?.Info(
            "Cable persistence metadata probe canceled. " +
            reason);
    }

    private void RunCablePersistenceMetadataProbe(
        string sceneName)
    {
        _cablePersistenceMetadataProbeRuns++;
        _lastCablePersistenceMetadataProbeScene =
            sceneName;
        _lastCablePersistenceMetadataCandidateTypeCount =
            0;
        _lastCablePersistenceMetadataInspectedTypeCount =
            0;
        _lastCablePersistenceMetadataRelevantMemberCount =
            0;
        _lastCablePersistenceMetadataPath =
            string.Empty;
        _lastCablePersistenceMetadataCandidateTypes =
            string.Empty;
        _lastCablePersistenceMetadataRelevantMembers =
            string.Empty;
        _lastCablePersistenceMetadataError =
            string.Empty;

        if (
            _context is null ||
            _gameTypeCatalog is null ||
            _gameTypeInspector is null
        )
        {
            _lastCablePersistenceMetadataError =
                "The metadata services required by the cable persistence probe are unavailable.";

            AppendProof(
                "CablePersistenceMetadataProbeError");

            return;
        }

        try
        {
            IReadOnlyList<DCMLGameTypeInfo> cableTypes =
                _gameTypeCatalog.Find(
                    new DCMLGameTypeQuery(
                        fullNameStartsWith:
                            "Il2Cpp.",
                        nameContains:
                            "Cable",
                        maxResults:
                            512));

            IReadOnlyList<DCMLGameTypeInfo> saveTypes =
                _gameTypeCatalog.Find(
                    new DCMLGameTypeQuery(
                        fullNameStartsWith:
                            "Il2Cpp.",
                        nameContains:
                            "Save",
                        maxResults:
                            2048));

            string[] candidateTypeNames =
                cableTypes
                    .Select(
                        value =>
                            value.FullName)
                    .Concat(
                        saveTypes
                            .Where(
                                value =>
                                    IsRelevantCablePersistenceSaveType(
                                        value.FullName))
                            .Select(
                                value =>
                                    value.FullName))
                    .Concat(
                        new[]
                        {
                            "Il2Cpp.NetworkSaveData",
                            "Il2Cpp.CableLink"
                        })
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(
                                value))
                    .Distinct(
                        StringComparer.Ordinal)
                    .OrderBy(
                        value =>
                            value,
                        StringComparer.Ordinal)
                    .Take(
                        256)
                    .ToArray();

            _lastCablePersistenceMetadataCandidateTypeCount =
                candidateTypeNames.Length;

            _lastCablePersistenceMetadataCandidateTypes =
                string.Join(
                    ", ",
                    candidateTypeNames);

            var inspections =
                new List<DCMLGameTypeInspection>();

            foreach (
                string typeName in
                candidateTypeNames)
            {
                DCMLGameTypeInspection? inspection =
                    _gameTypeInspector.Inspect(
                        new DCMLGameTypeInspectionQuery(
                            typeName,
                            includeInheritedMembers:
                                false,
                            maxMembers:
                                4096));

                if (inspection is not null)
                {
                    inspections.Add(
                        inspection);
                }
            }

            _lastCablePersistenceMetadataInspectedTypeCount =
                inspections.Count;

            DCMLGameTypeMemberInfo[] relevantMembers =
                inspections
                    .SelectMany(
                        value =>
                            value.Members)
                    .Where(
                        value =>
                            !value.IsInherited &&
                            IsRelevantCablePersistenceMemberName(
                                value.Name))
                    .OrderBy(
                        value =>
                            value.DeclaringTypeFullName,
                        StringComparer.Ordinal)
                    .ThenBy(
                        value =>
                            value.Kind,
                        StringComparer.Ordinal)
                    .ThenBy(
                        value =>
                            value.Name,
                        StringComparer.Ordinal)
                    .ToArray();

            _lastCablePersistenceMetadataRelevantMemberCount =
                relevantMembers.Length;

            _lastCablePersistenceMetadataRelevantMembers =
                string.Join(
                    " || ",
                    relevantMembers
                        .Take(96)
                        .Select(
                            value =>
                                value.DeclaringTypeFullName +
                                "::" +
                                value.Signature));

            _lastCablePersistenceMetadataPath =
                WriteCablePersistenceMetadata(
                    sceneName,
                    candidateTypeNames,
                    inspections);

            AppendProof(
                "CablePersistenceMetadataProbe");

            _logger?.Info(
                "Cable persistence metadata probe for scene '" +
                sceneName +
                "' inspected " +
                inspections.Count +
                " of " +
                candidateTypeNames.Length +
                " candidate type(s); relevant member count=" +
                relevantMembers.Length +
                ".");
        }
        catch (Exception exception)
        {
            _lastCablePersistenceMetadataError =
                exception.GetType().FullName +
                ": " +
                exception.Message;

            AppendProof(
                "CablePersistenceMetadataProbeError");

            _logger?.Error(
                "Cable persistence metadata probe failed for scene '" +
                sceneName +
                "'.");

            _logger?.Error(
                exception.ToString());
        }
    }

    private string WriteCablePersistenceMetadata(
        string sceneName,
        IReadOnlyList<string> candidateTypeNames,
        IReadOnlyList<DCMLGameTypeInspection> inspections)
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
                    sceneName)
                    ? "unnamed-scene"
                    : sceneName);

        string path =
            Path.Combine(
                _context.DataDirectory,
                "DCML.CablePersistenceMetadata." +
                safeSceneName +
                ".log");

        var byType =
            inspections
                .ToDictionary(
                    value =>
                        value.TypeFullName,
                    StringComparer.Ordinal);

        var lines =
            new List<string>
            {
                "DCML Cable Persistence Metadata Probe",
                $"UTC: {DateTime.UtcNow:O}",
                $"Scene: {sceneName}",
                $"CandidateTypeCount: {candidateTypeNames.Count}",
                $"InspectedTypeCount: {inspections.Count}",
                "Mode: metadata-only",
                "PhysicalNetworkEdgesEmitted: False",
                string.Empty
            };

        foreach (
            string typeName in
            candidateTypeNames)
        {
            lines.Add(
                "TYPE: " +
                typeName);

            if (
                !byType.TryGetValue(
                    typeName,
                    out DCMLGameTypeInspection? inspection)
            )
            {
                lines.Add(
                    "Status: NOT FOUND");

                lines.Add(
                    string.Empty);

                continue;
            }

            lines.Add(
                "Status: FOUND");

            lines.Add(
                "Assembly: " +
                inspection.AssemblyName);

            lines.Add(
                "BaseTypes: " +
                FormatNameList(
                    inspection.BaseTypeFullNames));

            lines.Add(
                "Interfaces: " +
                FormatNameList(
                    inspection.InterfaceFullNames));

            lines.Add(
                "TotalMemberCount: " +
                inspection.TotalMemberCount);

            lines.Add(
                "AtMemberLimit: " +
                inspection.AtMemberLimit);

            lines.Add(
                string.Empty);

            AppendMemberSection(
                lines,
                "DIRECT INSTANCE FIELDS",
                inspection.Fields
                    .Where(
                        value =>
                            !value.IsInherited &&
                            !value.IsStatic)
                    .ToArray());

            AppendMemberSection(
                lines,
                "DIRECT PROPERTIES",
                inspection.Properties
                    .Where(
                        value =>
                            !value.IsInherited &&
                            !value.IsStatic)
                    .ToArray());

            AppendMemberSection(
                lines,
                "PERSISTENCE / ENDPOINT METHODS",
                inspection.Methods
                    .Where(
                        value =>
                            !value.IsInherited &&
                            IsRelevantCablePersistenceMemberName(
                                value.Name))
                    .ToArray());
        }

        File.WriteAllLines(
            path,
            lines);

        return path;
    }

    private static bool IsRelevantCablePersistenceSaveType(
        string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return false;
        }

        foreach (
            string keyword in
            new[]
            {
                "Network",
                "Cable",
                "Server",
                "Switch",
                "Router",
                "Firewall",
                "Patch",
                "SFP",
                "Internet",
                "Port",
                "Connection",
                "Link"
            })
        {
            if (
                typeName.IndexOf(
                    keyword,
                    StringComparison.OrdinalIgnoreCase) >= 0
            )
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsRelevantCablePersistenceMemberName(
        string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        if (
            string.Equals(
                name,
                "id",
                StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith(
                "Id",
                StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith(
                "IDs",
                StringComparison.OrdinalIgnoreCase)
        )
        {
            return true;
        }

        foreach (
            string keyword in
            new[]
            {
                "Cable",
                "Start",
                "End",
                "Source",
                "Target",
                "Parent",
                "Port",
                "Server",
                "Switch",
                "Router",
                "Firewall",
                "Patch",
                "Internet",
                "SFP",
                "Link",
                "Connection",
                "Network",
                "Device",
                "Save",
                "Load",
                "Serialize",
                "Deserialize"
            })
        {
            if (
                name.IndexOf(
                    keyword,
                    StringComparison.OrdinalIgnoreCase) >= 0
            )
            {
                return true;
            }
        }

        return false;
    }

    private async Task RunHardwareSnapshotsAsync(
        string sceneName)
    {
        if (_dataCenterApi?.Hardware is null)
        {
            return;
        }

        try
        {
            DataCenterHardwareSnapshotSet snapshot =
                await _dataCenterApi.Hardware
                    .CaptureAsync(
                        new DataCenterHardwareSnapshotQuery(
                            sceneName:
                                sceneName,
                            includeSceneObjects:
                                true,
                            includeResources:
                                true,
                            maxPerType:
                                64))
                    .ConfigureAwait(
                        false);

            _hardwareSnapshotRuns++;
            _lastHardwareSnapshotScene = sceneName;
            _lastHardwareSnapshotServerCount = snapshot.Servers.Count;
            _lastHardwareSnapshotRackCount = snapshot.Racks.Count;
            _lastHardwareSnapshotNetworkDeviceCount = snapshot.NetworkDevices.Count;
            _lastHardwareSnapshotSfpCount = snapshot.SfpModules.Count;
            _lastHardwareSnapshotCableCount = snapshot.Cables.Count;

            _lastHardwareSnapshotServerDefinitionCount =
                snapshot.ServerDefinitions.Count;
            _lastHardwareSnapshotServerInstanceCount =
                snapshot.ServerInstances.Count;
            _lastHardwareSnapshotRackDefinitionCount =
                snapshot.RackDefinitions.Count;
            _lastHardwareSnapshotRackInstanceCount =
                snapshot.RackInstances.Count;
            _lastHardwareSnapshotNetworkDeviceDefinitionCount =
                snapshot.NetworkDeviceDefinitions.Count;
            _lastHardwareSnapshotNetworkDeviceInstanceCount =
                snapshot.NetworkDeviceInstances.Count;
            _lastHardwareSnapshotSfpDefinitionCount =
                snapshot.SfpModuleDefinitions.Count;
            _lastHardwareSnapshotSfpInstanceCount =
                snapshot.SfpModuleInstances.Count;
            _lastHardwareSnapshotCableDefinitionCount =
                snapshot.CableDefinitions.Count;
            _lastHardwareSnapshotCableInstanceCount =
                snapshot.CableInstances.Count;

            _lastHardwareSnapshotSfpLinkedCount =
                snapshot.SfpModuleInstances.Count(
                    value =>
                        value.Link is not null);
            _lastHardwareSnapshotCableParentServerCount =
                snapshot.CableInstances.Count(
                    value =>
                        value.ParentServer is not null);
            _lastHardwareSnapshotCableParentSwitchCount =
                snapshot.CableInstances.Count(
                    value =>
                        value.ParentSwitch is not null);
            _lastHardwareSnapshotCableParentPatchPanelCount =
                snapshot.CableInstances.Count(
                    value =>
                        value.ParentPatchPanel is not null);
            _lastHardwareSnapshotCableParentInternetCount =
                snapshot.CableInstances.Count(
                    value =>
                        value.ParentInternet is not null);
            _lastHardwareSnapshotCableInsertedSfpCount =
                snapshot.CableInstances.Count(
                    value =>
                        value.InsertedSfp is not null);

            _lastHardwareSnapshotError = string.Empty;

            _lastHardwareSnapshotSample =
                string.Join(
                    " || ",
                    snapshot.Servers
                        .Take(3)
                        .Select(
                            value =>
                                "server-" +
                                (value.IsResource ? "definition:" : "instance:") +
                                value.Name +
                                "|ip=" + (value.IP ?? "(null)") +
                                "|on=" + FormatNullable(value.IsOn) +
                                "|processing=" +
                                FormatNullable(value.CurrentProcessingSpeed) +
                                "/" +
                                FormatNullable(value.MaxProcessingSpeed))
                        .Concat(
                            snapshot.Racks
                                .Take(2)
                                .Select(
                                    value =>
                                        "rack-" +
                                        (value.IsResource ? "definition:" : "instance:") +
                                        value.Name +
                                        "|positionsOff=" +
                                        FormatNullable(value.ArePositionsTurnedOff)))
                        .Concat(
                            snapshot.NetworkDevices
                                .Take(6)
                                .Select(
                                    value =>
                                        value.Kind + "-" +
                                        (value.IsResource ? "definition:" : "instance:") +
                                        value.Name +
                                        "|ports=" + FormatNullable(value.PortCount) +
                                        "|on=" + FormatNullable(value.IsOn) +
                                        "|asn=" + FormatNullable(value.ASN) +
                                        "|clusterIp=" + (value.ClusterIP ?? "(null)")))
                        .Concat(
                            snapshot.SfpModules
                                .Take(2)
                                .Select(
                                    value =>
                                        "sfp-" +
                                        (value.IsResource ? "definition:" : "instance:") +
                                        value.Name +
                                        "|speed=" + FormatNullable(value.Speed) +
                                        "|type=" + FormatNullable(value.SfpType)))
                        .Concat(
                            snapshot.Cables
                                .Take(3)
                                .Select(
                                    value =>
                                        "cable-" +
                                        (value.IsResource ? "definition:" : "instance:") +
                                        value.Name +
                                        "|speed=" + FormatNullable(value.ConnectionSpeed) +
                                        "|fibre=" + FormatNullable(value.IsFibrePort) +
                                        "|sfp=" + FormatNullable(value.IsSfpPort) +
                                        "|type=" + (value.TypeOfLink ?? "(null)"))));

            _lastHardwareRelationshipSample =
                string.Join(
                    " || ",
                    snapshot.SfpModuleInstances
                        .Where(
                            value =>
                                value.Link is not null)
                        .Take(4)
                        .Select(
                            value =>
                                "sfp:" +
                                value.Name +
                                "->" +
                                FormatHardwareReference(
                                    value.Link))
                        .Concat(
                            snapshot.CableInstances
                                .Where(
                                    value =>
                                        value.ParentServer is not null ||
                                        value.ParentSwitch is not null ||
                                        value.ParentPatchPanel is not null ||
                                        value.ParentInternet is not null ||
                                        value.InsertedSfp is not null)
                                .Take(8)
                                .Select(
                                    value =>
                                        "cable:" +
                                        value.Name +
                                        "|server=" +
                                        FormatHardwareReference(
                                            value.ParentServer) +
                                        "|switch=" +
                                        FormatHardwareReference(
                                            value.ParentSwitch) +
                                        "|patchPanel=" +
                                        FormatHardwareReference(
                                            value.ParentPatchPanel) +
                                        "|internet=" +
                                        FormatHardwareReference(
                                            value.ParentInternet) +
                                        "|sfp=" +
                                        FormatHardwareReference(
                                            value.InsertedSfp))));

            if (_dataCenterApi?.Topology is not null)
            {
                try
                {
                    DataCenterHardwareTopologyGraph topology =
                        await _dataCenterApi.Topology
                            .CaptureAsync(
                                new DataCenterHardwareSnapshotQuery(
                                    sceneName:
                                        sceneName,
                                    includeSceneObjects:
                                        true,
                                    includeResources:
                                        true,
                                    maxPerType:
                                        64))
                            .ConfigureAwait(
                                false);

                    _lastHardwareTopologyNodeCount =
                        topology.Nodes.Count;
                    _lastHardwareTopologyEdgeCount =
                        topology.Edges.Count;
                    _lastHardwareTopologyResolvedEdgeCount =
                        topology.ResolvedEdges.Count;
                    _lastHardwareTopologyUnresolvedEdgeCount =
                        topology.UnresolvedEdges.Count;
                    _lastHardwareTopologyCableSearchPages =
                        topology.CableSearchPages;
                    _lastHardwareTopologyCableCandidatesScanned =
                        topology.CableCandidatesScanned;
                    _lastHardwareTopologyCableSearchExhausted =
                        topology.CableSearchExhausted;
                    _lastHardwareTopologyNonSceneSearchPages =
                        topology.NonSceneCableSearchPages;
                    _lastHardwareTopologyNonSceneCandidatesScanned =
                        topology.NonSceneCableCandidatesScanned;
                    _lastHardwareTopologyNonSceneTargetMatchCount =
                        topology.NonSceneTargetMatchCount;
                    _lastHardwareTopologyNonSceneSearchExhausted =
                        topology.NonSceneCableSearchExhausted;
                    _lastHardwareTopologyTargetCableDetailRequestedCount =
                        topology.TargetedCableDetailRequestedCount;
                    _lastHardwareTopologyTargetCableDetailFoundCount =
                        topology.TargetedCableDetailFoundCount;
                    _lastHardwareTopologyTargetCableParentServerCount =
                        topology.Edges.Count(
                            edge =>
                                edge.TargetCable?.ParentServer is not null);
                    _lastHardwareTopologyTargetCableParentSwitchCount =
                        topology.Edges.Count(
                            edge =>
                                edge.TargetCable?.ParentSwitch is not null);
                    _lastHardwareTopologyTargetCableParentPatchPanelCount =
                        topology.Edges.Count(
                            edge =>
                                edge.TargetCable?.ParentPatchPanel is not null);
                    _lastHardwareTopologyTargetCableParentInternetCount =
                        topology.Edges.Count(
                            edge =>
                                edge.TargetCable?.ParentInternet is not null);
                    _lastHardwareTopologyTargetCableInsertedSfpCount =
                        topology.Edges.Count(
                            edge =>
                                edge.TargetCable?.InsertedSfp is not null);
                    _lastHardwareTopologyTargetCableSfpPortCount =
                        topology.Edges.Count(
                            edge =>
                                edge.TargetCable?.IsSfpPort == true);
                    _lastHardwareTopologyTargetCableEndpointCount =
                        topology.Edges.Count(
                            edge =>
                                edge.TargetCable?.IsEndPoint == true);
                    _lastHardwareTopologyError =
                        string.Empty;

                    _lastHardwareTopologySample =
                        string.Join(
                            " || ",
                            topology.Edges
                                .Take(8)
                                .Select(
                                    edge =>
                                        edge.Relationship +
                                        ":" +
                                        FormatHardwareReference(
                                            edge.Source) +
                                        "->" +
                                        FormatHardwareReference(
                                            edge.Target) +
                                        "|resolved=" +
                                        edge.TargetResolved +
                                        "|location=" +
                                        edge.TargetLocation +
                                        "|observed=" +
                                        edge.TargetObserved +
                                        "|resolvedName=" +
                                        (
                                            edge.ResolvedTargetName.Length == 0
                                                ? "(null)"
                                                : edge.ResolvedTargetName
                                        ) +
                                        "|targetSfpPort=" +
                                        FormatNullable(
                                            edge.TargetCable?.IsSfpPort) +
                                        "|targetEndpoint=" +
                                        FormatNullable(
                                            edge.TargetCable?.IsEndPoint) +
                                        "|targetStartOrEnd=" +
                                        FormatNullable(
                                            edge.TargetCable?.IsStartOrEnd) +
                                        "|targetSwitchId=" +
                                        (
                                            string.IsNullOrEmpty(
                                                edge.TargetCable?.SwitchID)
                                                ? "(null)"
                                                : edge.TargetCable!.SwitchID
                                        ) +
                                        "|targetType=" +
                                        (
                                            string.IsNullOrEmpty(
                                                edge.TargetCable?.TypeOfLink)
                                                ? "(null)"
                                                : edge.TargetCable!.TypeOfLink
                                        ) +
                                        "|parentServer=" +
                                        FormatHardwareReference(
                                            edge.TargetCable?.ParentServer) +
                                        "|parentSwitch=" +
                                        FormatHardwareReference(
                                            edge.TargetCable?.ParentSwitch) +
                                        "|parentPatchPanel=" +
                                        FormatHardwareReference(
                                            edge.TargetCable?.ParentPatchPanel) +
                                        "|parentInternet=" +
                                        FormatHardwareReference(
                                            edge.TargetCable?.ParentInternet) +
                                        "|insertedSfp=" +
                                        FormatHardwareReference(
                                            edge.TargetCable?.InsertedSfp)));

                    await RunTargetCableHierarchyProbeAsync(
                            sceneName,
                            topology)
                        .ConfigureAwait(
                            false);

                    AppendProof(
                        "HardwareTopology");
                }
                catch (Exception topologyException)
                {
                    _lastHardwareTopologyNodeCount = 0;
                    _lastHardwareTopologyEdgeCount = 0;
                    _lastHardwareTopologyResolvedEdgeCount = 0;
                    _lastHardwareTopologyUnresolvedEdgeCount = 0;
                    _lastHardwareTopologyCableSearchPages = 0;
                    _lastHardwareTopologyCableCandidatesScanned = 0;
                    _lastHardwareTopologyCableSearchExhausted = false;
                    _lastHardwareTopologyNonSceneSearchPages = 0;
                    _lastHardwareTopologyNonSceneCandidatesScanned = 0;
                    _lastHardwareTopologyNonSceneTargetMatchCount = 0;
                    _lastHardwareTopologyNonSceneSearchExhausted = false;
                    _lastHardwareTopologyTargetCableDetailRequestedCount = 0;
                    _lastHardwareTopologyTargetCableDetailFoundCount = 0;
                    _lastHardwareTopologyTargetCableParentServerCount = 0;
                    _lastHardwareTopologyTargetCableParentSwitchCount = 0;
                    _lastHardwareTopologyTargetCableParentPatchPanelCount = 0;
                    _lastHardwareTopologyTargetCableParentInternetCount = 0;
                    _lastHardwareTopologyTargetCableInsertedSfpCount = 0;
                    _lastHardwareTopologyTargetCableSfpPortCount = 0;
                    _lastHardwareTopologyTargetCableEndpointCount = 0;
                    ResetTargetCableHierarchyProbe();
                    _lastHardwareTopologySample = string.Empty;
                    _lastHardwareTopologyError =
                        topologyException.GetType().FullName +
                        ": " +
                        topologyException.Message;

                    AppendProof(
                        "HardwareTopologyError");
                }
            }

            AppendProof(
                "HardwareSnapshots");

            _logger?.Info(
                "Hardware snapshots for scene '" +
                sceneName +
                "' returned server=" +
                snapshot.Servers.Count +
                ", rack=" +
                snapshot.Racks.Count +
                ", network-device=" +
                snapshot.NetworkDevices.Count +
                ", sfp=" +
                snapshot.SfpModules.Count +
                ", cable=" +
                snapshot.Cables.Count +
                "; definitions/instances: server=" +
                snapshot.ServerDefinitions.Count +
                "/" +
                snapshot.ServerInstances.Count +
                ", rack=" +
                snapshot.RackDefinitions.Count +
                "/" +
                snapshot.RackInstances.Count +
                ", network=" +
                snapshot.NetworkDeviceDefinitions.Count +
                "/" +
                snapshot.NetworkDeviceInstances.Count +
                ", sfp=" +
                snapshot.SfpModuleDefinitions.Count +
                "/" +
                snapshot.SfpModuleInstances.Count +
                ", cable=" +
                snapshot.CableDefinitions.Count +
                "/" +
                snapshot.CableInstances.Count +
                ".");
        }
        catch (Exception exception)
        {
            _hardwareSnapshotRuns++;
            _lastHardwareSnapshotScene = sceneName;
            _lastHardwareSnapshotServerCount = 0;
            _lastHardwareSnapshotRackCount = 0;
            _lastHardwareSnapshotNetworkDeviceCount = 0;
            _lastHardwareSnapshotSfpCount = 0;
            _lastHardwareSnapshotCableCount = 0;
            _lastHardwareSnapshotServerDefinitionCount = 0;
            _lastHardwareSnapshotServerInstanceCount = 0;
            _lastHardwareSnapshotRackDefinitionCount = 0;
            _lastHardwareSnapshotRackInstanceCount = 0;
            _lastHardwareSnapshotNetworkDeviceDefinitionCount = 0;
            _lastHardwareSnapshotNetworkDeviceInstanceCount = 0;
            _lastHardwareSnapshotSfpDefinitionCount = 0;
            _lastHardwareSnapshotSfpInstanceCount = 0;
            _lastHardwareSnapshotCableDefinitionCount = 0;
            _lastHardwareSnapshotCableInstanceCount = 0;
            _lastHardwareSnapshotSfpLinkedCount = 0;
            _lastHardwareSnapshotCableParentServerCount = 0;
            _lastHardwareSnapshotCableParentSwitchCount = 0;
            _lastHardwareSnapshotCableParentPatchPanelCount = 0;
            _lastHardwareSnapshotCableParentInternetCount = 0;
            _lastHardwareSnapshotCableInsertedSfpCount = 0;
            _lastHardwareRelationshipSample = string.Empty;
            _lastHardwareTopologyNodeCount = 0;
            _lastHardwareTopologyEdgeCount = 0;
            _lastHardwareTopologyResolvedEdgeCount = 0;
            _lastHardwareTopologyUnresolvedEdgeCount = 0;
            _lastHardwareTopologyCableSearchPages = 0;
            _lastHardwareTopologyCableCandidatesScanned = 0;
            _lastHardwareTopologyCableSearchExhausted = false;
            _lastHardwareTopologyNonSceneSearchPages = 0;
            _lastHardwareTopologyNonSceneCandidatesScanned = 0;
            _lastHardwareTopologyNonSceneTargetMatchCount = 0;
            _lastHardwareTopologyNonSceneSearchExhausted = false;
            _lastHardwareTopologyTargetCableDetailRequestedCount = 0;
            _lastHardwareTopologyTargetCableDetailFoundCount = 0;
            _lastHardwareTopologyTargetCableParentServerCount = 0;
            _lastHardwareTopologyTargetCableParentSwitchCount = 0;
            _lastHardwareTopologyTargetCableParentPatchPanelCount = 0;
            _lastHardwareTopologyTargetCableParentInternetCount = 0;
            _lastHardwareTopologyTargetCableInsertedSfpCount = 0;
            _lastHardwareTopologyTargetCableSfpPortCount = 0;
            _lastHardwareTopologyTargetCableEndpointCount = 0;
            ResetTargetCableHierarchyProbe();
            _lastHardwareTopologySample = string.Empty;
            _lastHardwareTopologyError = string.Empty;
            _lastHardwareSnapshotError =
                exception.GetType().FullName +
                ": " +
                exception.Message;
            _lastHardwareSnapshotSample = string.Empty;

            AppendProof(
                "HardwareSnapshotsError");

            _logger?.Error(
                "Hardware snapshots failed for scene '" +
                sceneName +
                "'.");

            _logger?.Error(
                exception.ToString());
        }
    }

    private async Task RunTargetCableHierarchyProbeAsync(
        string sceneName,
        DataCenterHardwareTopologyGraph topology)
    {
        ResetTargetCableHierarchyProbe();

        if (
            _gameObjectDiscovery is null ||
            _gameThread is null
        )
        {
            return;
        }

        try
        {
            int[] targetGameObjectIds =
                topology.Edges
                    .Where(
                        edge =>
                            edge.TargetResolved &&
                            edge.TargetCable is not null)
                    .Select(
                        edge =>
                            edge.TargetCable!.GameObjectInstanceId)
                    .Distinct()
                    .ToArray();

            _lastHardwareTopologyTargetHierarchyTargetCount =
                targetGameObjectIds.Length;

            if (targetGameObjectIds.Length == 0)
            {
                return;
            }

            var infoById =
                new Dictionary<int, DCMLGameObjectInfo>();

            var frontier =
                new HashSet<int>(
                    targetGameObjectIds);

            const int maximumDepth = 8;

            for (
                int depth = 0;
                depth < maximumDepth &&
                frontier.Count > 0;
                depth++)
            {
                int[] requestedIds =
                    frontier.ToArray();

                IReadOnlyList<DCMLGameObjectInfo> infos =
                    await _gameThread
                        .InvokeAsync(
                            () =>
                                _gameObjectDiscovery.Find(
                                    new DCMLGameObjectQuery(
                                        sceneName:
                                            sceneName,
                                        includeInactive:
                                            true,
                                        maxResults:
                                            Math.Min(
                                                requestedIds.Length,
                                                DCMLGameObjectQuery.MaximumMaxResults),
                                        instanceIds:
                                            requestedIds)))
                        .ConfigureAwait(
                            false);

                frontier.Clear();

                foreach (
                    DCMLGameObjectInfo info in
                    infos)
                {
                    infoById[info.InstanceId] =
                        info;

                    if (
                        info.ParentInstanceId.HasValue &&
                        !infoById.ContainsKey(
                            info.ParentInstanceId.Value)
                    )
                    {
                        frontier.Add(
                            info.ParentInstanceId.Value);
                    }
                }
            }

            _lastHardwareTopologyTargetHierarchyMatchedTargetCount =
                targetGameObjectIds.Count(
                    value =>
                        infoById.ContainsKey(
                            value));

            _lastHardwareTopologyTargetHierarchyObjectCount =
                infoById.Count;

            var samples =
                new List<string>();

            var customerBaseRootIds =
                new HashSet<int>();

            foreach (
                int targetId in
                targetGameObjectIds)
            {
                var chain =
                    new List<DCMLGameObjectInfo>();

                int? currentId =
                    targetId;

                var visited =
                    new HashSet<int>();

                while (
                    currentId.HasValue &&
                    chain.Count < maximumDepth &&
                    visited.Add(
                        currentId.Value) &&
                    infoById.TryGetValue(
                        currentId.Value,
                        out DCMLGameObjectInfo? current))
                {
                    chain.Add(
                        current);

                    currentId =
                        current.ParentInstanceId;
                }

                IReadOnlyList<DCMLGameObjectInfo> ancestors =
                    chain
                        .Skip(1)
                        .ToArray();

                DCMLGameObjectInfo? customerBase =
                    ancestors.FirstOrDefault(
                        value =>
                            HasExactComponentType(
                                value,
                                "Il2Cpp.CustomerBase"));

                if (customerBase is not null)
                {
                    customerBaseRootIds.Add(
                        customerBase.InstanceId);
                }

                if (
                    ancestors.Any(
                        value =>
                            HasExactComponentType(
                                value,
                                "Il2Cpp.Server"))
                )
                {
                    _lastHardwareTopologyTargetHierarchyServerAncestorCount++;
                }

                if (
                    ancestors.Any(
                        value =>
                            HasExactComponentType(
                                value,
                                "Il2Cpp.NetworkSwitch",
                                "Il2Cpp.Router",
                                "Il2Cpp.Firewall"))
                )
                {
                    _lastHardwareTopologyTargetHierarchyNetworkDeviceAncestorCount++;
                }

                if (
                    ancestors.Any(
                        value =>
                            HasExactComponentType(
                                value,
                                "Il2Cpp.PatchPanel"))
                )
                {
                    _lastHardwareTopologyTargetHierarchyPatchPanelAncestorCount++;
                }

                if (
                    ancestors.Any(
                        value =>
                            HasExactComponentType(
                                value,
                                "Il2Cpp.Internet"))
                )
                {
                    _lastHardwareTopologyTargetHierarchyInternetAncestorCount++;
                }

                if (
                    ancestors.Any(
                        value =>
                            HasExactComponentType(
                                value,
                                "Il2Cpp.Rack"))
                )
                {
                    _lastHardwareTopologyTargetHierarchyRackAncestorCount++;
                }

                if (
                    samples.Count < 8 &&
                    chain.Count > 0)
                {
                    samples.Add(
                        string.Join(
                            " > ",
                            chain.Select(
                                (value, depth) =>
                                    "d" +
                                    depth +
                                    ":go#" +
                                    value.InstanceId +
                                    ":" +
                                    value.Name +
                                    "[" +
                                    string.Join(
                                        ",",
                                        value.ComponentTypeNames
                                            .Where(
                                                typeName =>
                                                    typeName.StartsWith(
                                                        "Il2Cpp.",
                                                        StringComparison.Ordinal))
                                            .Take(8)) +
                                    "]")));
                }
            }

            _lastHardwareTopologyTargetHierarchySample =
                string.Join(
                    " || ",
                    samples);

            _lastHardwareTopologyTargetHierarchyError =
                string.Empty;

            await RunCustomerBaseSubtreeProbeAsync(
                    sceneName,
                    customerBaseRootIds,
                    topology)
                .ConfigureAwait(
                    false);
        }
        catch (Exception exception)
        {
            ResetTargetCableHierarchyProbe();

            _lastHardwareTopologyTargetHierarchyError =
                exception.GetType().FullName +
                ": " +
                exception.Message;
        }
    }

    private static bool HasExactComponentType(
        DCMLGameObjectInfo info,
        params string[] typeNames)
    {
        foreach (
            string componentTypeName in
            info.ComponentTypeNames)
        {
            foreach (
                string requestedTypeName in
                typeNames)
            {
                if (
                    string.Equals(
                        componentTypeName,
                        requestedTypeName,
                        StringComparison.Ordinal)
                )
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void ResetTargetCableHierarchyProbe()
    {
        _lastHardwareTopologyTargetHierarchyTargetCount = 0;
        _lastHardwareTopologyTargetHierarchyMatchedTargetCount = 0;
        _lastHardwareTopologyTargetHierarchyObjectCount = 0;
        _lastHardwareTopologyTargetHierarchyServerAncestorCount = 0;
        _lastHardwareTopologyTargetHierarchyNetworkDeviceAncestorCount = 0;
        _lastHardwareTopologyTargetHierarchyPatchPanelAncestorCount = 0;
        _lastHardwareTopologyTargetHierarchyInternetAncestorCount = 0;
        _lastHardwareTopologyTargetHierarchyRackAncestorCount = 0;
        _lastHardwareTopologyTargetHierarchySample = string.Empty;
        _lastHardwareTopologyTargetHierarchyError = string.Empty;

        ResetCustomerBaseSubtreeProbe();
    }

    private async Task RunCustomerBaseSubtreeProbeAsync(
        string sceneName,
        IReadOnlyCollection<int> rootInstanceIds,
        DataCenterHardwareTopologyGraph topology)
    {
        ResetCustomerBaseSubtreeProbe();

        if (
            _gameObjectDiscovery is null ||
            _gameThread is null ||
            rootInstanceIds.Count == 0
        )
        {
            return;
        }

        try
        {
            _lastHardwareTopologyCustomerBaseRootCount =
                rootInstanceIds.Count;

            var infoById =
                new Dictionary<int, DCMLGameObjectInfo>();

            int[] rootIds =
                rootInstanceIds
                    .Distinct()
                    .ToArray();

            IReadOnlyList<DCMLGameObjectInfo> rootInfos =
                await _gameThread
                    .InvokeAsync(
                        () =>
                            _gameObjectDiscovery.Find(
                                new DCMLGameObjectQuery(
                                    sceneName:
                                        sceneName,
                                    includeInactive:
                                        true,
                                    maxResults:
                                        Math.Min(
                                            rootIds.Length,
                                            DCMLGameObjectQuery.MaximumMaxResults),
                                    instanceIds:
                                        rootIds)))
                    .ConfigureAwait(
                        false);

            foreach (
                DCMLGameObjectInfo info in
                rootInfos)
            {
                infoById[info.InstanceId] =
                    info;
            }

            var frontier =
                new HashSet<int>(
                    rootIds);

            const int maximumDepth =
                6;

            for (
                int depth = 0;
                depth < maximumDepth &&
                frontier.Count > 0;
                depth++)
            {
                int[] parentIds =
                    frontier.ToArray();

                IReadOnlyList<DCMLGameObjectInfo> children =
                    await _gameThread
                        .InvokeAsync(
                            () =>
                                _gameObjectDiscovery.Find(
                                    new DCMLGameObjectQuery(
                                        sceneName:
                                            sceneName,
                                        includeInactive:
                                            true,
                                        maxResults:
                                            DCMLGameObjectQuery.MaximumMaxResults,
                                        parentInstanceIds:
                                            parentIds)))
                        .ConfigureAwait(
                            false);

                if (
                    children.Count ==
                        DCMLGameObjectQuery.MaximumMaxResults
                )
                {
                    _lastHardwareTopologyCustomerBaseSubtreeAtResultLimit =
                        true;
                }

                frontier.Clear();

                foreach (
                    DCMLGameObjectInfo child in
                    children)
                {
                    if (
                        !infoById.ContainsKey(
                            child.InstanceId)
                    )
                    {
                        infoById[child.InstanceId] =
                            child;

                        frontier.Add(
                            child.InstanceId);
                    }
                }
            }

            IReadOnlyList<DCMLGameObjectInfo> objects =
                infoById.Values
                    .OrderBy(
                        value =>
                            value.HierarchyPath,
                        StringComparer.OrdinalIgnoreCase)
                    .ThenBy(
                        value =>
                            value.InstanceId)
                    .ToArray();

            _lastHardwareTopologyCustomerBaseSubtreeObjectCount =
                objects.Count;

            _lastHardwareTopologyCustomerBaseSubtreeNetworkSwitchCount =
                CountObjectsWithExactComponentType(
                    objects,
                    "Il2Cpp.NetworkSwitch");

            _lastHardwareTopologyCustomerBaseSubtreeRouterCount =
                CountObjectsWithExactComponentType(
                    objects,
                    "Il2Cpp.Router");

            _lastHardwareTopologyCustomerBaseSubtreeFirewallCount =
                CountObjectsWithExactComponentType(
                    objects,
                    "Il2Cpp.Firewall");

            _lastHardwareTopologyCustomerBaseSubtreeServerCount =
                CountObjectsWithExactComponentType(
                    objects,
                    "Il2Cpp.Server");

            _lastHardwareTopologyCustomerBaseSubtreePatchPanelCount =
                CountObjectsWithExactComponentType(
                    objects,
                    "Il2Cpp.PatchPanel");

            _lastHardwareTopologyCustomerBaseSubtreeInternetCount =
                CountObjectsWithExactComponentType(
                    objects,
                    "Il2Cpp.Internet");

            _lastHardwareTopologyCustomerBaseSubtreeRackCount =
                CountObjectsWithExactComponentType(
                    objects,
                    "Il2Cpp.Rack");

            _lastHardwareTopologyCustomerBaseSubtreeCableLinkCount =
                CountObjectsWithExactComponentType(
                    objects,
                    "Il2Cpp.CableLink");

            string[] il2CppTypes =
                objects
                    .SelectMany(
                        value =>
                            value.ComponentTypeNames)
                    .Where(
                        value =>
                            value.StartsWith(
                                "Il2Cpp.",
                                StringComparison.Ordinal))
                    .Distinct(
                        StringComparer.Ordinal)
                    .OrderBy(
                        value =>
                            value,
                        StringComparer.Ordinal)
                    .ToArray();

            _lastHardwareTopologyCustomerBaseSubtreeIl2CppTypeCount =
                il2CppTypes.Length;

            _lastHardwareTopologyCustomerBaseSubtreeTypes =
                string.Join(
                    ", ",
                    il2CppTypes);

            _lastHardwareTopologyCustomerBaseSubtreeSample =
                string.Join(
                    " || ",
                    objects
                        .Where(
                            value =>
                                value.ComponentTypeNames.Any(
                                    typeName =>
                                        typeName.StartsWith(
                                            "Il2Cpp.",
                                            StringComparison.Ordinal)))
                        .Take(24)
                        .Select(
                            value =>
                                "go#" +
                                value.InstanceId +
                                ":" +
                                value.HierarchyPath +
                                "[" +
                                string.Join(
                                    ",",
                                    value.ComponentTypeNames
                                        .Where(
                                            typeName =>
                                                typeName.StartsWith(
                                                    "Il2Cpp.",
                                                    StringComparison.Ordinal))
                                        .Take(12)) +
                                "]"));

            _lastHardwareTopologyCustomerBaseSubtreeError =
                string.Empty;

            await RunCustomerBaseStateProbeAsync(
                    sceneName,
                    rootIds,
                    topology)
                .ConfigureAwait(
                    false);
        }
        catch (Exception exception)
        {
            ResetCustomerBaseSubtreeProbe();

            _lastHardwareTopologyCustomerBaseSubtreeError =
                exception.GetType().FullName +
                ": " +
                exception.Message;
        }
    }

    private static int CountObjectsWithExactComponentType(
        IEnumerable<DCMLGameObjectInfo> objects,
        string typeName)
    {
        return
            objects.Count(
                value =>
                    HasExactComponentType(
                        value,
                        typeName));
    }

    private void ResetCustomerBaseSubtreeProbe()
    {
        _lastHardwareTopologyCustomerBaseRootCount = 0;
        _lastHardwareTopologyCustomerBaseSubtreeObjectCount = 0;
        _lastHardwareTopologyCustomerBaseSubtreeIl2CppTypeCount = 0;
        _lastHardwareTopologyCustomerBaseSubtreeNetworkSwitchCount = 0;
        _lastHardwareTopologyCustomerBaseSubtreeRouterCount = 0;
        _lastHardwareTopologyCustomerBaseSubtreeFirewallCount = 0;
        _lastHardwareTopologyCustomerBaseSubtreeServerCount = 0;
        _lastHardwareTopologyCustomerBaseSubtreePatchPanelCount = 0;
        _lastHardwareTopologyCustomerBaseSubtreeInternetCount = 0;
        _lastHardwareTopologyCustomerBaseSubtreeRackCount = 0;
        _lastHardwareTopologyCustomerBaseSubtreeCableLinkCount = 0;
        _lastHardwareTopologyCustomerBaseSubtreeAtResultLimit = false;
        _lastHardwareTopologyCustomerBaseSubtreeTypes = string.Empty;
        _lastHardwareTopologyCustomerBaseSubtreeSample = string.Empty;
        _lastHardwareTopologyCustomerBaseSubtreeError = string.Empty;

        ResetCustomerBaseStateProbe();
    }

    private async Task RunCustomerBaseStateProbeAsync(
        string sceneName,
        IReadOnlyCollection<int> customerBaseGameObjectIds,
        DataCenterHardwareTopologyGraph topology)
    {
        ResetCustomerBaseStateProbe();

        if (
            _gameTypeInspector is null ||
            _gameComponentStateReader is null ||
            customerBaseGameObjectIds.Count == 0
        )
        {
            return;
        }

        try
        {
            DCMLGameTypeInspection? inspection =
                _gameTypeInspector.Inspect(
                    new DCMLGameTypeInspectionQuery(
                        "Il2Cpp.CustomerBase",
                        includeInheritedMembers:
                            false,
                        maxMembers:
                            4096));

            if (inspection is null)
            {
                _lastCustomerBaseStateProbeError =
                    "Il2Cpp.CustomerBase was not found by the game type inspector.";

                return;
            }

            // The IL2CPP interop wrapper exposes native field metadata
            // through static NativeFieldInfoPtr_* fields. The live
            // CustomerBase state itself is exposed through properties.
            // Keep the field diagnostics explicit rather than pretending
            // those wrapper metadata fields are runtime state.
            _lastCustomerBaseStateProbeFieldCount =
                0;

            _lastCustomerBaseStateProbeFields =
                string.Empty;

            var safePropertyNames =
                new HashSet<string>(
                    new[]
                    {
                        "cableLinks",
                        "currentSpeed",
                        "currentTotalAppSpeeRequirements",
                        "customerBaseID",
                        "customerID",
                        "customerItem",
                        "howLongToWaitBeforeFine",
                        "maximumAppRequirementsSpeedTotal",
                        "wantsInternet",
                        "wasFullySatisfied"
                    },
                    StringComparer.OrdinalIgnoreCase);

            DCMLGameTypeMemberInfo[] properties =
                inspection.Properties
                    .Where(
                        value =>
                            !value.IsStatic &&
                            !value.IsInherited &&
                            value.CanRead &&
                            safePropertyNames.Contains(
                                value.Name))
                    .OrderBy(
                        value =>
                            value.Name,
                        StringComparer.OrdinalIgnoreCase)
                    .ToArray();

            string[] propertyNames =
                properties
                    .Select(
                        value =>
                            value.Name)
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToArray();

            _lastCustomerBaseStateProbePropertyCount =
                propertyNames.Length;

            _lastCustomerBaseStateProbeProperties =
                string.Join(
                    ", ",
                    properties.Select(
                        value =>
                            value.Name +
                            ":" +
                            (
                                value.ValueTypeFullName.Length == 0
                                    ? "(unknown)"
                                    : value.ValueTypeFullName
                            )));

            _lastCustomerBaseRelatedTypeSummary =
                BuildCustomerBaseRelatedTypeSummary();

            if (propertyNames.Length == 0)
            {
                _lastCustomerBaseStateProbeError =
                    "No targeted readable Il2Cpp.CustomerBase properties were found.";

                return;
            }

            int[] gameObjectIds =
                customerBaseGameObjectIds
                    .Distinct()
                    .ToArray();

            IReadOnlyList<DCMLGameComponentState> states =
                await _gameComponentStateReader
                    .ReadAsync(
                        new DCMLGameComponentStateQuery(
                            componentTypeName:
                                "Il2Cpp.CustomerBase",
                            memberNames:
                                propertyNames,
                            sceneName:
                                sceneName,
                            scope:
                                DCMLGameComponentScope.Scene,
                            includeInactive:
                                true,
                            maxResults:
                                Math.Min(
                                    gameObjectIds.Length,
                                    DCMLGameComponentStateQuery.MaximumMaxResults),
                            gameObjectInstanceIds:
                                gameObjectIds))
                    .ConfigureAwait(
                        false);

            _lastCustomerBaseStateProbeComponentCount =
                states.Count;

            var topologyTargetIds =
                new HashSet<int>(
                    topology.Edges
                        .Where(
                            edge =>
                                edge.TargetResolved &&
                                edge.TargetCable is not null)
                        .Select(
                            edge =>
                                edge.TargetCable!.ComponentInstanceId));

            _lastCustomerBaseCableLinkCollectionTopologyTargetCount =
                topologyTargetIds.Count;

            var allCableLinkReferenceIds =
                new HashSet<int>();

            var cableCollectionSamples =
                new List<string>();

            var referenceTypes =
                new HashSet<string>(
                    StringComparer.Ordinal);

            var unsupportedTypes =
                new HashSet<string>(
                    StringComparer.Ordinal);

            var samples =
                new List<string>();

            foreach (
                DCMLGameComponentState state in
                states)
            {
                var values =
                    new List<string>();

                foreach (
                    KeyValuePair<string, DCMLGameValue> pair in
                    state.Values
                        .OrderBy(
                            value =>
                                value.Key,
                            StringComparer.OrdinalIgnoreCase))
                {
                    DCMLGameValue value =
                        pair.Value;

                    _lastCustomerBaseStateProbeValueCount++;

                    switch (value.Kind)
                    {
                        case DCMLGameValueKind.Null:
                            _lastCustomerBaseStateProbeNullCount++;
                            break;

                        case DCMLGameValueKind.String:
                        case DCMLGameValueKind.Boolean:
                        case DCMLGameValueKind.Integer:
                        case DCMLGameValueKind.Number:
                        case DCMLGameValueKind.Enum:
                            _lastCustomerBaseStateProbeScalarCount++;
                            break;

                        case DCMLGameValueKind.Reference:
                            _lastCustomerBaseStateProbeReferenceCount++;

                            if (
                                value.ReferenceValue is not null &&
                                value.ReferenceValue.TypeName.Length > 0
                            )
                            {
                                referenceTypes.Add(
                                    value.ReferenceValue.TypeName);
                            }

                            break;

                        case DCMLGameValueKind.ReferenceCollection:
                            _lastCustomerBaseStateProbeReferenceCount +=
                                value.ReferenceValues.Count;

                            foreach (
                                DCMLGameReference collectionReference in
                                value.ReferenceValues)
                            {
                                if (
                                    collectionReference.TypeName.Length > 0
                                )
                                {
                                    referenceTypes.Add(
                                        collectionReference.TypeName);
                                }
                            }

                            break;

                        case DCMLGameValueKind.Unsupported:
                            _lastCustomerBaseStateProbeUnsupportedCount++;

                            if (value.TypeName.Length > 0)
                            {
                                unsupportedTypes.Add(
                                    value.TypeName);
                            }

                            break;

                        case DCMLGameValueKind.Unavailable:
                            _lastCustomerBaseStateProbeUnavailableCount++;
                            break;
                    }

                    if (
                        values.Count < 24 &&
                        (
                            value.Kind ==
                                DCMLGameValueKind.Reference ||
                            value.Kind ==
                                DCMLGameValueKind.ReferenceCollection ||
                            value.Kind ==
                                DCMLGameValueKind.String ||
                            value.Kind ==
                                DCMLGameValueKind.Boolean ||
                            value.Kind ==
                                DCMLGameValueKind.Integer ||
                            value.Kind ==
                                DCMLGameValueKind.Number ||
                            value.Kind ==
                                DCMLGameValueKind.Enum ||
                            value.Kind ==
                                DCMLGameValueKind.Unsupported
                        )
                    )
                    {
                        values.Add(
                            pair.Key +
                            "=" +
                            FormatGameValue(
                                value));
                    }
                }

                if (
                    state.Values.TryGetValue(
                        "cableLinks",
                        out DCMLGameValue? cableLinksValue) &&
                    cableLinksValue.Kind ==
                        DCMLGameValueKind.ReferenceCollection
                )
                {
                    _lastCustomerBaseCableLinkCollectionBaseCount++;

                    if (
                        cableLinksValue.CollectionCount.HasValue)
                    {
                        _lastCustomerBaseCableLinkCollectionDeclaredCount +=
                            cableLinksValue.CollectionCount.Value;
                    }

                    _lastCustomerBaseCableLinkCollectionReferenceCount +=
                        cableLinksValue.ReferenceValues.Count;

                    foreach (
                        DCMLGameReference cableReference in
                        cableLinksValue.ReferenceValues)
                    {
                        allCableLinkReferenceIds.Add(
                            cableReference.InstanceId);

                        if (
                            topologyTargetIds.Contains(
                                cableReference.InstanceId)
                        )
                        {
                            _lastCustomerBaseCableLinkCollectionTopologyTargetMatchCount++;
                        }
                        else
                        {
                            _lastCustomerBaseCableLinkCollectionNonTargetReferenceCount++;
                        }
                    }

                    if (
                        cableCollectionSamples.Count < 9
                    )
                    {
                        long? baseId =
                            state.Values.TryGetValue(
                                "customerBaseID",
                                out DCMLGameValue? baseIdValue)
                                ? baseIdValue.IntegerValue
                                : null;

                        cableCollectionSamples.Add(
                            "base#" +
                            (
                                baseId?.ToString() ??
                                "?"
                            ) +
                            "/go#" +
                            state.GameObjectInstanceId +
                            "=[" +
                            string.Join(
                                ", ",
                                cableLinksValue.ReferenceValues
                                    .Take(8)
                                    .Select(
                                        reference =>
                                            reference.TypeName +
                                            "#" +
                                            reference.InstanceId +
                                            ":" +
                                            reference.Name +
                                            "|topologyTarget=" +
                                            topologyTargetIds.Contains(
                                                reference.InstanceId))) +
                            "]");
                    }
                }

                if (samples.Count < 9)
                {
                    samples.Add(
                        "go#" +
                        state.GameObjectInstanceId +
                        "/component#" +
                        state.ComponentInstanceId +
                        ":" +
                        state.Name +
                        "{" +
                        string.Join(
                            ", ",
                            values) +
                        "}");
                }
            }

            _lastCustomerBaseCableLinkCollectionUniqueReferenceCount =
                allCableLinkReferenceIds.Count;

            _lastCustomerBaseCableLinkCollectionSample =
                string.Join(
                    " || ",
                    cableCollectionSamples);

            _lastCustomerBaseStateProbeReferenceTypes =
                referenceTypes.Count == 0
                    ? "(none)"
                    : string.Join(
                        ", ",
                        referenceTypes
                            .OrderBy(
                                value =>
                                    value,
                                StringComparer.Ordinal));

            _lastCustomerBaseStateProbeUnsupportedTypes =
                unsupportedTypes.Count == 0
                    ? "(none)"
                    : string.Join(
                        ", ",
                        unsupportedTypes
                            .OrderBy(
                                value =>
                                    value,
                                StringComparer.Ordinal));

            _lastCustomerBaseStateProbeSample =
                string.Join(
                    " || ",
                    samples);

            _lastCustomerBaseStateProbeError =
                string.Empty;
        }
        catch (Exception exception)
        {
            ResetCustomerBaseStateProbe();

            _lastCustomerBaseStateProbeError =
                exception.GetType().FullName +
                ": " +
                exception.Message;
        }
    }

    private string BuildCustomerBaseRelatedTypeSummary()
    {
        if (_gameTypeInspector is null)
        {
            return "(type inspector unavailable)";
        }

        string[] typeNames =
        {
            "Il2Cpp.CustomerItem",
            "Il2Cpp.CustomerBaseSaveData"
        };

        var summaries =
            new List<string>();

        foreach (
            string typeName in
            typeNames)
        {
            DCMLGameTypeInspection? inspection =
                _gameTypeInspector.Inspect(
                    new DCMLGameTypeInspectionQuery(
                        typeName,
                        includeInheritedMembers:
                            false,
                        maxMembers:
                            4096));

            if (inspection is null)
            {
                summaries.Add(
                    typeName +
                    ":NOT_FOUND");

                continue;
            }

            string[] readableProperties =
                inspection.Properties
                    .Where(
                        value =>
                            !value.IsStatic &&
                            !value.IsInherited &&
                            value.CanRead)
                    .OrderBy(
                        value =>
                            value.Name,
                        StringComparer.OrdinalIgnoreCase)
                    .Take(32)
                    .Select(
                        value =>
                            value.Name +
                            ":" +
                            (
                                value.ValueTypeFullName.Length == 0
                                    ? "(unknown)"
                                    : value.ValueTypeFullName
                            ))
                    .ToArray();

            string[] directInstanceFields =
                inspection.Fields
                    .Where(
                        value =>
                            !value.IsStatic &&
                            !value.IsInherited)
                    .OrderBy(
                        value =>
                            value.Name,
                        StringComparer.OrdinalIgnoreCase)
                    .Take(32)
                    .Select(
                        value =>
                            value.Name +
                            ":" +
                            (
                                value.ValueTypeFullName.Length == 0
                                    ? "(unknown)"
                                    : value.ValueTypeFullName
                            ))
                    .ToArray();

            summaries.Add(
                typeName +
                "{properties=[" +
                (
                    readableProperties.Length == 0
                        ? "(none)"
                        : string.Join(
                            ", ",
                            readableProperties)
                ) +
                "]; fields=[" +
                (
                    directInstanceFields.Length == 0
                        ? "(none)"
                        : string.Join(
                            ", ",
                            directInstanceFields)
                ) +
                "]}");
        }

        return
            string.Join(
                " || ",
                summaries);
    }

    private static string FormatGameValue(
        DCMLGameValue value)
    {
        switch (value.Kind)
        {
            case DCMLGameValueKind.Null:
                return "(null)";

            case DCMLGameValueKind.String:
            case DCMLGameValueKind.Enum:
                return
                    value.StringValue ??
                    "(null)";

            case DCMLGameValueKind.Boolean:
                return
                    value.BooleanValue?.ToString() ??
                    "(null)";

            case DCMLGameValueKind.Integer:
                return
                    value.IntegerValue?.ToString() ??
                    "(null)";

            case DCMLGameValueKind.Number:
                return
                    value.NumberValue?.ToString() ??
                    "(null)";

            case DCMLGameValueKind.Reference:
                if (value.ReferenceValue is null)
                {
                    return "(null-reference)";
                }

                return
                    value.ReferenceValue.TypeName +
                    "#" +
                    value.ReferenceValue.InstanceId +
                    ":" +
                    value.ReferenceValue.Name;

            case DCMLGameValueKind.ReferenceCollection:
                return
                    "[" +
                    string.Join(
                        ", ",
                        value.ReferenceValues
                            .Take(8)
                            .Select(
                                reference =>
                                    reference.TypeName +
                                    "#" +
                                    reference.InstanceId +
                                    ":" +
                                    reference.Name)) +
                    (
                        value.ReferenceValues.Count > 8
                            ? ", ..."
                            : string.Empty
                    ) +
                    "]";

            case DCMLGameValueKind.Unsupported:
                return
                    "<unsupported:" +
                    (
                        value.TypeName.Length == 0
                            ? "unknown"
                            : value.TypeName
                    ) +
                    ">";

            case DCMLGameValueKind.Unavailable:
                return
                    "<unavailable:" +
                    (
                        value.Diagnostic.Length == 0
                            ? "no diagnostic"
                            : value.Diagnostic
                    ) +
                    ">";

            default:
                return
                    "<" +
                    value.Kind +
                    ">";
        }
    }

    private void ResetCustomerBaseStateProbe()
    {
        _lastCustomerBaseStateProbeComponentCount = 0;
        _lastCustomerBaseStateProbeFieldCount = 0;
        _lastCustomerBaseStateProbePropertyCount = 0;
        _lastCustomerBaseStateProbeValueCount = 0;
        _lastCustomerBaseStateProbeReferenceCount = 0;
        _lastCustomerBaseStateProbeScalarCount = 0;
        _lastCustomerBaseStateProbeNullCount = 0;
        _lastCustomerBaseStateProbeUnsupportedCount = 0;
        _lastCustomerBaseStateProbeUnavailableCount = 0;
        _lastCustomerBaseStateProbeFields = string.Empty;
        _lastCustomerBaseStateProbeProperties = string.Empty;
        _lastCustomerBaseRelatedTypeSummary = string.Empty;
        _lastCustomerBaseStateProbeReferenceTypes = string.Empty;
        _lastCustomerBaseStateProbeUnsupportedTypes = string.Empty;
        _lastCustomerBaseStateProbeSample = string.Empty;
        _lastCustomerBaseStateProbeError = string.Empty;

        _lastCustomerBaseCableLinkCollectionBaseCount = 0;
        _lastCustomerBaseCableLinkCollectionDeclaredCount = 0;
        _lastCustomerBaseCableLinkCollectionReferenceCount = 0;
        _lastCustomerBaseCableLinkCollectionUniqueReferenceCount = 0;
        _lastCustomerBaseCableLinkCollectionTopologyTargetCount = 0;
        _lastCustomerBaseCableLinkCollectionTopologyTargetMatchCount = 0;
        _lastCustomerBaseCableLinkCollectionNonTargetReferenceCount = 0;
        _lastCustomerBaseCableLinkCollectionSample = string.Empty;
    }

    private static string FormatHardwareReference(
        DataCenterHardwareReference? reference)
    {
        if (reference is null)
        {
            return "(null)";
        }

        return
            reference.TypeName +
            "#" +
            reference.InstanceId +
            ":" +
            reference.Name;
    }

    private static string FormatNullable(
        object? value)
    {
        return value?.ToString() ?? "(null)";
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

    private void RunTargetedSemanticDiscovery(
        string sceneName)
    {
        if (_dataCenterApi is null)
        {
            return;
        }

        const int maxPerKind =
            64;

        string[] kinds =
        {
            DataCenterEntityKinds.Server,
            DataCenterEntityKinds.Rack,
            DataCenterEntityKinds.NetworkDevice,
            DataCenterEntityKinds.Cable
        };

        try
        {
            var resultsByKind =
                new Dictionary<string, IReadOnlyList<DataCenterEntityInfo>>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (
                string kind in
                kinds)
            {
                resultsByKind[kind] =
                    _dataCenterApi.Entities.Find(
                        new DataCenterEntityQuery(
                            kind:
                                kind,
                            sceneName:
                                sceneName,
                            includeInactive:
                                true,
                            includeUnknown:
                                false,
                            maxResults:
                                maxPerKind));
            }

            _targetedSemanticRuns++;
            _lastTargetedSemanticScene =
                sceneName;
            _lastTargetedSemanticError =
                string.Empty;

            _lastTargetedSemanticCounts =
                string.Join(
                    ", ",
                    kinds.Select(
                        kind =>
                            kind +
                            "=" +
                            resultsByKind[kind].Count));

            _lastTargetedSemanticAtLimit =
                string.Join(
                    ", ",
                    kinds.Where(
                        kind =>
                            resultsByKind[kind].Count >=
                            maxPerKind));

            _lastTargetedSemanticSample =
                string.Join(
                    " || ",
                    kinds.SelectMany(
                            kind =>
                                resultsByKind[kind]
                                    .Take(3))
                        .Select(
                            value =>
                                value.Kind +
                                ":" +
                                value.HierarchyPath +
                                " [" +
                                value.ClassificationRuleId +
                                "]"));

            AppendProof(
                "TargetedSemanticDiscovery");

            _logger?.Info(
                "Targeted semantic discovery for scene '" +
                sceneName +
                "' returned " +
                _lastTargetedSemanticCounts +
                ".");
        }
        catch (Exception exception)
        {
            _targetedSemanticRuns++;
            _lastTargetedSemanticScene =
                sceneName;
            _lastTargetedSemanticCounts =
                string.Empty;
            _lastTargetedSemanticAtLimit =
                string.Empty;
            _lastTargetedSemanticError =
                exception.GetType().FullName +
                ": " +
                exception.Message;
            _lastTargetedSemanticSample =
                string.Empty;

            AppendProof(
                "TargetedSemanticDiscoveryError");

            _logger?.Error(
                $"Targeted semantic discovery failed for scene '{sceneName}'.");

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

    private void RunGameTypeCatalog(
        string sceneName)
    {
        if (
            _context is null ||
            _gameTypeCatalog is null
        )
        {
            return;
        }

        try
        {
            IReadOnlyList<DCMLGameTypeInfo> allTypes =
                _gameTypeCatalog.Find(
                    new DCMLGameTypeQuery(
                        fullNameStartsWith:
                            "Il2Cpp.",
                        maxResults:
                            DCMLGameTypeQuery.MaximumMaxResults));

            var keywordResults =
                GameTypeKeywords
                    .Select(
                        keyword =>
                            new GameTypeKeywordResult(
                                keyword,
                                allTypes
                                    .Where(
                                        value =>
                                            value.FullName.IndexOf(
                                                keyword,
                                                StringComparison.OrdinalIgnoreCase) >= 0)
                                    .ToArray()))
                    .ToArray();

            _gameTypeCatalogRuns++;
            _lastGameTypeCatalogTypeCount =
                allTypes.Count;
            _lastGameTypeCatalogAtResultLimit =
                allTypes.Count ==
                DCMLGameTypeQuery.MaximumMaxResults;
            _lastGameTypeCatalogScene =
                sceneName;
            _lastGameTypeCatalogError =
                string.Empty;

            _lastGameTypeCatalogKeywordCounts =
                string.Join(
                    ", ",
                    keywordResults
                        .Select(
                            value =>
                                value.Keyword +
                                "=" +
                                value.Types.Count));

            _lastGameTypeCatalogSample =
                string.Join(
                    " || ",
                    keywordResults
                        .SelectMany(
                            value =>
                                value.Types)
                        .GroupBy(
                            value =>
                                value.AssemblyName +
                                "|" +
                                value.FullName,
                            StringComparer.OrdinalIgnoreCase)
                        .Select(
                            group =>
                                group.First())
                        .OrderBy(
                            value =>
                                value.FullName,
                            StringComparer.OrdinalIgnoreCase)
                        .Take(16)
                        .Select(
                            value =>
                                value.FullName +
                                " [" +
                                value.Kind +
                                "; base=" +
                                (
                                    value.BaseTypeFullName.Length == 0
                                        ? "none"
                                        : value.BaseTypeFullName
                                ) +
                                "]"));

            _lastGameTypeCatalogPath =
                WriteGameTypeCatalog(
                    sceneName,
                    allTypes,
                    keywordResults);

            AppendProof(
                "GameTypeCatalog");

            _logger?.Info(
                $"Game type catalog found {allTypes.Count} loaded Il2Cpp type(s) for scene '{sceneName}'.");
        }
        catch (Exception exception)
        {
            _gameTypeCatalogRuns++;
            _lastGameTypeCatalogTypeCount =
                0;
            _lastGameTypeCatalogAtResultLimit =
                false;
            _lastGameTypeCatalogScene =
                sceneName;
            _lastGameTypeCatalogPath =
                string.Empty;
            _lastGameTypeCatalogKeywordCounts =
                string.Empty;
            _lastGameTypeCatalogSample =
                string.Empty;
            _lastGameTypeCatalogError =
                exception.GetType().FullName +
                ": " +
                exception.Message;

            AppendProof(
                "GameTypeCatalogError");

            _logger?.Error(
                $"Game type catalog failed for scene '{sceneName}'.");

            _logger?.Error(
                exception.ToString());
        }
    }

    private string WriteGameTypeCatalog(
        string sceneName,
        IReadOnlyList<DCMLGameTypeInfo> allTypes,
        IReadOnlyList<GameTypeKeywordResult> keywordResults)
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
                    sceneName)
                    ? "unnamed-scene"
                    : sceneName);

        string path =
            Path.Combine(
                _context.DataDirectory,
                "DCML.GameTypeCatalog." +
                safeSceneName +
                ".log");

        var lines =
            new List<string>();

        lines.Add(
            "DCML Loaded Game Type Catalog");

        lines.Add(
            $"UTC: {DateTime.UtcNow:O}");

        lines.Add(
            $"Scene: {sceneName}");

        lines.Add(
            $"Il2CppTypeCount: {allTypes.Count}");

        lines.Add(
            $"AtResultLimit: {allTypes.Count == DCMLGameTypeQuery.MaximumMaxResults}");

        lines.Add(
            $"ResultLimit: {DCMLGameTypeQuery.MaximumMaxResults}");

        lines.Add(
            $"KeywordCount: {keywordResults.Count}");

        lines.Add(
            string.Empty);

        lines.Add(
            "Keyword matches");

        foreach (
            GameTypeKeywordResult result in
            keywordResults)
        {
            lines.Add(
                $"[{result.Keyword}] Count={result.Types.Count}");

            foreach (
                DCMLGameTypeInfo type in
                result.Types)
            {
                lines.Add(
                    FormatGameType(
                        type));
            }

            lines.Add(
                string.Empty);
        }

        lines.Add(
            "All loaded Il2Cpp types");

        foreach (
            DCMLGameTypeInfo type in
            allTypes)
        {
            lines.Add(
                FormatGameType(
                    type));
        }

        lines.Add(
            string.Empty);

        File.WriteAllLines(
            path,
            lines);

        return
            path;
    }

    private static string FormatGameType(
        DCMLGameTypeInfo type)
    {
        return
            type.FullName +
            " | Assembly=" +
            type.AssemblyName +
            " | Kind=" +
            type.Kind +
            " | Abstract=" +
            type.IsAbstract +
            " | Base=" +
            (
                type.BaseTypeFullName.Length == 0
                    ? "none"
                    : type.BaseTypeFullName
            ) +
            " | Interfaces=" +
            (
                type.InterfaceFullNames.Count == 0
                    ? "none"
                    : string.Join(
                        ",",
                        type.InterfaceFullNames)
            );
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

        if (_gameTypeCatalog is null)
        {
            throw new InvalidOperationException(
                "The game type catalog service is unavailable.");
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

    private void RunGameResourceDiscovery(
        string sceneName)
    {
        if (_gameResourceDiscovery is null)
        {
            return;
        }

        try
        {
            IReadOnlyList<DCMLGameResourceInfo> servers =
                _gameResourceDiscovery.Find(
                    new DCMLGameResourceQuery(
                        componentTypeName:
                            "Il2Cpp.Server",
                        maxResults:
                            64));

            IReadOnlyList<DCMLGameResourceInfo> racks =
                _gameResourceDiscovery.Find(
                    new DCMLGameResourceQuery(
                        componentTypeName:
                            "Il2Cpp.Rack",
                        maxResults:
                            64));

            IReadOnlyList<DCMLGameResourceInfo> switches =
                _gameResourceDiscovery.Find(
                    new DCMLGameResourceQuery(
                        componentTypeName:
                            "Il2Cpp.NetworkSwitch",
                        maxResults:
                            64));

            IReadOnlyList<DCMLGameResourceInfo> routers =
                _gameResourceDiscovery.Find(
                    new DCMLGameResourceQuery(
                        componentTypeName:
                            "Il2Cpp.Router",
                        maxResults:
                            64));

            IReadOnlyList<DCMLGameResourceInfo> firewalls =
                _gameResourceDiscovery.Find(
                    new DCMLGameResourceQuery(
                        componentTypeName:
                            "Il2Cpp.Firewall",
                        maxResults:
                            64));

            IReadOnlyList<DCMLGameResourceInfo> cables =
                _gameResourceDiscovery.Find(
                    new DCMLGameResourceQuery(
                        componentTypeName:
                            "Il2Cpp.CableLink",
                        maxResults:
                            64));

            DCMLGameResourceInfo[] networkDevices =
                switches
                    .Concat(
                        routers)
                    .Concat(
                        firewalls)
                    .GroupBy(
                        value =>
                            value.InstanceId)
                    .Select(
                        group =>
                            group.First())
                    .OrderBy(
                        value =>
                            value.Name,
                        StringComparer.OrdinalIgnoreCase)
                    .ThenBy(
                        value =>
                            value.InstanceId)
                    .ToArray();

            _gameResourceDiscoveryRuns++;
            _lastGameResourceDiscoveryScene =
                sceneName;
            _lastGameResourceDiscoveryServerCount =
                servers.Count;
            _lastGameResourceDiscoveryRackCount =
                racks.Count;
            _lastGameResourceDiscoveryNetworkDeviceCount =
                networkDevices.Length;
            _lastGameResourceDiscoveryCableCount =
                cables.Count;
            _lastGameResourceDiscoveryError =
                string.Empty;

            _lastGameResourceDiscoverySample =
                string.Join(
                    " || ",
                    servers
                        .Select(
                            value =>
                                "server:" +
                                FormatResource(
                                    value))
                        .Concat(
                            racks.Select(
                                value =>
                                    "rack:" +
                                    FormatResource(
                                        value)))
                        .Concat(
                            networkDevices.Select(
                                value =>
                                    "network-device:" +
                                    FormatResource(
                                        value)))
                        .Concat(
                            cables.Select(
                                value =>
                                    "cable:" +
                                    FormatResource(
                                        value)))
                        .Take(
                            16));

            AppendProof(
                "GameResourceDiscovery");

            _logger?.Info(
                "Game resource discovery for scene '" +
                sceneName +
                "' found server=" +
                servers.Count +
                ", rack=" +
                racks.Count +
                ", network-device=" +
                networkDevices.Length +
                ", cable=" +
                cables.Count +
                ".");
        }
        catch (Exception exception)
        {
            _gameResourceDiscoveryRuns++;
            _lastGameResourceDiscoveryScene =
                sceneName;
            _lastGameResourceDiscoveryServerCount =
                0;
            _lastGameResourceDiscoveryRackCount =
                0;
            _lastGameResourceDiscoveryNetworkDeviceCount =
                0;
            _lastGameResourceDiscoveryCableCount =
                0;
            _lastGameResourceDiscoveryError =
                exception.GetType().FullName +
                ": " +
                exception.Message;
            _lastGameResourceDiscoverySample =
                string.Empty;

            AppendProof(
                "GameResourceDiscoveryError");

            _logger?.Error(
                $"Game resource discovery failed after scene '{sceneName}' initialized.");

            _logger?.Error(
                exception.ToString());
        }
    }

    private static string FormatResource(
        DCMLGameResourceInfo value)
    {
        return
            value.Name +
            " [" +
            string.Join(
                ",",
                value.ComponentTypeNames.Take(6)) +
            "] #" +
            value.InstanceId;
    }

    private void RunGameTypeInspection(
        string sceneName)
    {
        if (
            _context is null ||
            _gameTypeInspector is null
        )
        {
            return;
        }

        try
        {
            DCMLGameTypeInspection[] inspections =
                InspectedGameTypeNames
                    .Select(
                        typeFullName =>
                            _gameTypeInspector.Inspect(
                                new DCMLGameTypeInspectionQuery(
                                    typeFullName,
                                    includeInheritedMembers:
                                        true,
                                    maxMembers:
                                        4096)))
                    .Where(
                        value =>
                            value is not null)
                    .Cast<DCMLGameTypeInspection>()
                    .ToArray();

            _gameTypeInspectionRuns++;
            _lastGameTypeInspectionScene =
                sceneName;
            _lastGameTypeInspectionTypeCount =
                inspections.Length;
            _lastGameTypeInspectionMemberCount =
                inspections.Sum(
                    value =>
                        value.TotalMemberCount);
            _lastGameTypeInspectionAtLimit =
                string.Join(
                    ", ",
                    inspections
                        .Where(
                            value =>
                                value.AtMemberLimit)
                        .Select(
                            value =>
                                value.TypeFullName));

            if (
                _lastGameTypeInspectionAtLimit.Length == 0
            )
            {
                _lastGameTypeInspectionAtLimit =
                    "(none)";
            }

            _lastGameTypeInspectionSummary =
                string.Join(
                    " || ",
                    inspections.Select(
                        value =>
                            value.TypeFullName +
                            ": bases=" +
                            value.BaseTypeFullNames.Count +
                            ", interfaces=" +
                            value.InterfaceFullNames.Count +
                            ", ctors=" +
                            value.Constructors.Count +
                            ", fields=" +
                            value.Fields.Count +
                            ", properties=" +
                            value.Properties.Count +
                            ", methods=" +
                            value.Methods.Count +
                            ", total=" +
                            value.TotalMemberCount));

            _lastGameTypeInspectionPath =
                WriteGameTypeInspection(
                    sceneName,
                    inspections);

            _lastGameTypeInspectionError =
                string.Empty;

            AppendProof(
                "GameTypeInspection");

            _logger?.Info(
                "Game type inspection for scene '" +
                sceneName +
                "' inspected " +
                inspections.Length +
                " target type(s) and " +
                _lastGameTypeInspectionMemberCount +
                " member(s).");
        }
        catch (Exception exception)
        {
            _gameTypeInspectionRuns++;
            _lastGameTypeInspectionScene =
                sceneName;
            _lastGameTypeInspectionTypeCount =
                0;
            _lastGameTypeInspectionMemberCount =
                0;
            _lastGameTypeInspectionAtLimit =
                string.Empty;
            _lastGameTypeInspectionPath =
                string.Empty;
            _lastGameTypeInspectionSummary =
                string.Empty;
            _lastGameTypeInspectionError =
                exception.GetType().FullName +
                ": " +
                exception.Message;

            AppendProof(
                "GameTypeInspectionError");

            _logger?.Error(
                $"Game type inspection failed after scene '{sceneName}' initialized.");

            _logger?.Error(
                exception.ToString());
        }
    }

    private string WriteGameTypeInspection(
        string sceneName,
        IReadOnlyList<DCMLGameTypeInspection> inspections)
    {
        if (_context is null)
        {
            throw new InvalidOperationException(
                "The module context is unavailable.");
        }

        Directory.CreateDirectory(
            _context.DataDirectory);

        string path =
            Path.Combine(
                _context.DataDirectory,
                "DCML.GameTypeInspection." +
                MakeSafeFileName(
                    sceneName) +
                ".log");

        var lines =
            new List<string>
            {
                "DCML Game Type Inspection",
                $"UTC: {DateTime.UtcNow:O}",
                $"Scene: {sceneName}",
                $"RequestedTypeCount: {InspectedGameTypeNames.Length}",
                $"FoundTypeCount: {inspections.Count}",
                string.Empty
            };

        foreach (
            string requestedTypeName in
            InspectedGameTypeNames)
        {
            DCMLGameTypeInspection? inspection =
                inspections.FirstOrDefault(
                    value =>
                        string.Equals(
                            value.TypeFullName,
                            requestedTypeName,
                            StringComparison.OrdinalIgnoreCase));

            lines.Add(
                "============================================================");

            lines.Add(
                requestedTypeName);

            if (inspection is null)
            {
                lines.Add(
                    "Status: NOT FOUND");

                lines.Add(
                    string.Empty);

                continue;
            }

            lines.Add(
                "Status: FOUND");

            lines.Add(
                $"Assembly: {inspection.AssemblyName}");

            lines.Add(
                $"BaseTypes: {FormatNameList(inspection.BaseTypeFullNames)}");

            lines.Add(
                $"Interfaces: {FormatNameList(inspection.InterfaceFullNames)}");

            lines.Add(
                $"TotalMemberCount: {inspection.TotalMemberCount}");

            lines.Add(
                $"ReturnedMemberCount: {inspection.Members.Count}");

            lines.Add(
                $"AtMemberLimit: {inspection.AtMemberLimit}");

            lines.Add(
                string.Empty);

            AppendMemberSection(
                lines,
                "CONSTRUCTORS",
                inspection.Constructors);

            AppendMemberSection(
                lines,
                "FIELDS",
                inspection.Fields);

            AppendMemberSection(
                lines,
                "PROPERTIES",
                inspection.Properties);

            AppendMemberSection(
                lines,
                "METHODS",
                inspection.Methods);
        }

        File.WriteAllLines(
            path,
            lines);

        return path;
    }

    private static void AppendMemberSection(
        ICollection<string> lines,
        string title,
        IReadOnlyList<DCMLGameTypeMemberInfo> members)
    {
        lines.Add(
            title +
            " (" +
            members.Count +
            ")");

        if (members.Count == 0)
        {
            lines.Add(
                "  (none)");

            lines.Add(
                string.Empty);

            return;
        }

        foreach (
            DCMLGameTypeMemberInfo member in
            members)
        {
            lines.Add(
                "  " +
                (
                    member.IsInherited
                        ? "[inherited from " +
                            member.DeclaringTypeFullName +
                            "] "
                        : string.Empty
                ) +
                member.Signature);
        }

        lines.Add(
            string.Empty);
    }

    private static string FormatNameList(
        IReadOnlyList<string> values)
    {
        return
            values.Count == 0
                ? "(none)"
                : string.Join(
                    " -> ",
                    values);
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
                    $"LastLifecycleStage: {_settings.LastLifecycleStage}",
                    $"EnableAutomaticSceneDiagnostics: {_settings.EnableAutomaticSceneDiagnostics}",
                    $"SceneDiagnosticDelayFrames: {_settings.SceneDiagnosticDelayFrames}",
                    $"EnableHeavyAutomaticSceneDiagnostics: {_settings.EnableHeavyAutomaticSceneDiagnostics}",
                    $"EnableCablePersistenceMetadataProbe: {_settings.EnableCablePersistenceMetadataProbe}",
                    $"CablePersistenceProbeDelayFrames: {_settings.CablePersistenceProbeDelayFrames}");

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
                    $"LastSceneEvent: {FormatLastSceneEvent()}",
                    $"AutomaticSceneDiagnosticPending: {_automaticSceneDiagnosticPending}",
                    $"AutomaticSceneDiagnosticScene: {_automaticSceneDiagnosticScene}",
                    $"AutomaticSceneDiagnosticFramesRemaining: {_automaticSceneDiagnosticFramesRemaining}",
                    $"AutomaticSceneDiagnosticStage: {_automaticSceneDiagnosticStage}",
                    $"AutomaticSceneDiagnosticSchedules: {_automaticSceneDiagnosticSchedules}",
                    $"AutomaticSceneDiagnosticCompletions: {_automaticSceneDiagnosticCompletions}",
                    $"AutomaticSceneDiagnosticCancellations: {_automaticSceneDiagnosticCancellations}",
                    $"AutomaticSceneDiagnosticLastError: {_automaticSceneDiagnosticLastError}",
                    $"CablePersistenceMetadataProbePending: {_cablePersistenceMetadataProbePending}",
                    $"CablePersistenceMetadataProbeScene: {_cablePersistenceMetadataProbeScene}",
                    $"CablePersistenceMetadataProbeFramesRemaining: {_cablePersistenceMetadataProbeFramesRemaining}");

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

        var targetedSemanticLines =
            string.Join(
                Environment.NewLine,
                $"TargetedSemanticRuns: {_targetedSemanticRuns}",
                $"LastTargetedSemanticScene: {_lastTargetedSemanticScene}",
                $"LastTargetedSemanticCounts: {_lastTargetedSemanticCounts}",
                $"LastTargetedSemanticAtLimit: {_lastTargetedSemanticAtLimit}",
                $"LastTargetedSemanticError: {_lastTargetedSemanticError}",
                $"LastTargetedSemanticSample: {_lastTargetedSemanticSample}");

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

        var gameTypeCatalogLines =
            string.Join(
                Environment.NewLine,
                $"GameTypeCatalogRuns: {_gameTypeCatalogRuns}",
                $"LastGameTypeCatalogTypeCount: {_lastGameTypeCatalogTypeCount}",
                $"LastGameTypeCatalogAtResultLimit: {_lastGameTypeCatalogAtResultLimit}",
                $"LastGameTypeCatalogScene: {_lastGameTypeCatalogScene}",
                $"LastGameTypeCatalogPath: {_lastGameTypeCatalogPath}",
                $"LastGameTypeCatalogKeywordCounts: {_lastGameTypeCatalogKeywordCounts}",
                $"LastGameTypeCatalogError: {_lastGameTypeCatalogError}",
                $"LastGameTypeCatalogSample: {_lastGameTypeCatalogSample}");

        var gameResourceDiscoveryLines =
            string.Join(
                Environment.NewLine,
                $"GameResourceDiscoveryRuns: {_gameResourceDiscoveryRuns}",
                $"LastGameResourceDiscoveryScene: {_lastGameResourceDiscoveryScene}",
                $"LastGameResourceDiscoveryServerCount: {_lastGameResourceDiscoveryServerCount}",
                $"LastGameResourceDiscoveryRackCount: {_lastGameResourceDiscoveryRackCount}",
                $"LastGameResourceDiscoveryNetworkDeviceCount: {_lastGameResourceDiscoveryNetworkDeviceCount}",
                $"LastGameResourceDiscoveryCableCount: {_lastGameResourceDiscoveryCableCount}",
                $"LastGameResourceDiscoveryError: {_lastGameResourceDiscoveryError}",
                $"LastGameResourceDiscoverySample: {_lastGameResourceDiscoverySample}");

        var gameTypeInspectionLines =
            string.Join(
                Environment.NewLine,
                $"GameTypeInspectionRuns: {_gameTypeInspectionRuns}",
                $"LastGameTypeInspectionScene: {_lastGameTypeInspectionScene}",
                $"LastGameTypeInspectionTypeCount: {_lastGameTypeInspectionTypeCount}",
                $"LastGameTypeInspectionMemberCount: {_lastGameTypeInspectionMemberCount}",
                $"LastGameTypeInspectionAtLimit: {_lastGameTypeInspectionAtLimit}",
                $"LastGameTypeInspectionPath: {_lastGameTypeInspectionPath}",
                $"LastGameTypeInspectionError: {_lastGameTypeInspectionError}",
                $"LastGameTypeInspectionSummary: {_lastGameTypeInspectionSummary}");

        var cablePersistenceMetadataLines =
            string.Join(
                Environment.NewLine,
                $"CablePersistenceMetadataProbeRuns: {_cablePersistenceMetadataProbeRuns}",
                $"LastCablePersistenceMetadataProbeScene: {_lastCablePersistenceMetadataProbeScene}",
                $"LastCablePersistenceMetadataCandidateTypeCount: {_lastCablePersistenceMetadataCandidateTypeCount}",
                $"LastCablePersistenceMetadataInspectedTypeCount: {_lastCablePersistenceMetadataInspectedTypeCount}",
                $"LastCablePersistenceMetadataRelevantMemberCount: {_lastCablePersistenceMetadataRelevantMemberCount}",
                $"LastCablePersistenceMetadataPath: {_lastCablePersistenceMetadataPath}",
                $"LastCablePersistenceMetadataCandidateTypes: {_lastCablePersistenceMetadataCandidateTypes}",
                $"LastCablePersistenceMetadataRelevantMembers: {_lastCablePersistenceMetadataRelevantMembers}",
                $"LastCablePersistenceMetadataError: {_lastCablePersistenceMetadataError}");

        var gameThreadLines =
            string.Join(
                Environment.NewLine,
                $"GameThreadProbeRuns: {_gameThreadProbeRuns}",
                $"LastGameThreadInitializeWasMainThread: {_lastGameThreadInitializeWasMainThread}",
                $"LastGameThreadBackgroundWasMainThread: {_lastGameThreadBackgroundWasMainThread}",
                $"LastGameThreadPostWasMainThread: {_lastGameThreadPostWasMainThread}",
                $"LastGameThreadInvokeWasMainThread: {_lastGameThreadInvokeWasMainThread}",
                $"LastGameThreadPostCount: {_lastGameThreadPostCount}",
                $"LastGameThreadInvokeCount: {_lastGameThreadInvokeCount}",
                $"LastGameThreadError: {_lastGameThreadError}");

        var hardwareSnapshotLines =
            string.Join(
                Environment.NewLine,
                $"HardwareSnapshotRuns: {_hardwareSnapshotRuns}",
                $"LastHardwareSnapshotScene: {_lastHardwareSnapshotScene}",
                $"LastHardwareSnapshotServerCount: {_lastHardwareSnapshotServerCount}",
                $"LastHardwareSnapshotRackCount: {_lastHardwareSnapshotRackCount}",
                $"LastHardwareSnapshotNetworkDeviceCount: {_lastHardwareSnapshotNetworkDeviceCount}",
                $"LastHardwareSnapshotSfpCount: {_lastHardwareSnapshotSfpCount}",
                $"LastHardwareSnapshotCableCount: {_lastHardwareSnapshotCableCount}",
                $"LastHardwareSnapshotServerDefinitionCount: {_lastHardwareSnapshotServerDefinitionCount}",
                $"LastHardwareSnapshotServerInstanceCount: {_lastHardwareSnapshotServerInstanceCount}",
                $"LastHardwareSnapshotRackDefinitionCount: {_lastHardwareSnapshotRackDefinitionCount}",
                $"LastHardwareSnapshotRackInstanceCount: {_lastHardwareSnapshotRackInstanceCount}",
                $"LastHardwareSnapshotNetworkDeviceDefinitionCount: {_lastHardwareSnapshotNetworkDeviceDefinitionCount}",
                $"LastHardwareSnapshotNetworkDeviceInstanceCount: {_lastHardwareSnapshotNetworkDeviceInstanceCount}",
                $"LastHardwareSnapshotSfpDefinitionCount: {_lastHardwareSnapshotSfpDefinitionCount}",
                $"LastHardwareSnapshotSfpInstanceCount: {_lastHardwareSnapshotSfpInstanceCount}",
                $"LastHardwareSnapshotCableDefinitionCount: {_lastHardwareSnapshotCableDefinitionCount}",
                $"LastHardwareSnapshotCableInstanceCount: {_lastHardwareSnapshotCableInstanceCount}",
                $"LastHardwareSnapshotSfpLinkedCount: {_lastHardwareSnapshotSfpLinkedCount}",
                $"LastHardwareSnapshotCableParentServerCount: {_lastHardwareSnapshotCableParentServerCount}",
                $"LastHardwareSnapshotCableParentSwitchCount: {_lastHardwareSnapshotCableParentSwitchCount}",
                $"LastHardwareSnapshotCableParentPatchPanelCount: {_lastHardwareSnapshotCableParentPatchPanelCount}",
                $"LastHardwareSnapshotCableParentInternetCount: {_lastHardwareSnapshotCableParentInternetCount}",
                $"LastHardwareSnapshotCableInsertedSfpCount: {_lastHardwareSnapshotCableInsertedSfpCount}",
                $"LastHardwareRelationshipSample: {_lastHardwareRelationshipSample}",
                "LastHardwareTopologyIdentityMode: ComponentInstanceId",
                $"LastHardwareTopologyNodeCount: {_lastHardwareTopologyNodeCount}",
                $"LastHardwareTopologyEdgeCount: {_lastHardwareTopologyEdgeCount}",
                $"LastHardwareTopologyResolvedEdgeCount: {_lastHardwareTopologyResolvedEdgeCount}",
                $"LastHardwareTopologyUnresolvedEdgeCount: {_lastHardwareTopologyUnresolvedEdgeCount}",
                $"LastHardwareTopologyCableSearchPages: {_lastHardwareTopologyCableSearchPages}",
                $"LastHardwareTopologyCableCandidatesScanned: {_lastHardwareTopologyCableCandidatesScanned}",
                $"LastHardwareTopologyCableSearchExhausted: {_lastHardwareTopologyCableSearchExhausted}",
                $"LastHardwareTopologyNonSceneSearchPages: {_lastHardwareTopologyNonSceneSearchPages}",
                $"LastHardwareTopologyNonSceneCandidatesScanned: {_lastHardwareTopologyNonSceneCandidatesScanned}",
                $"LastHardwareTopologyNonSceneTargetMatchCount: {_lastHardwareTopologyNonSceneTargetMatchCount}",
                $"LastHardwareTopologyNonSceneSearchExhausted: {_lastHardwareTopologyNonSceneSearchExhausted}",
                $"LastHardwareTopologyTargetCableDetailRequestedCount: {_lastHardwareTopologyTargetCableDetailRequestedCount}",
                $"LastHardwareTopologyTargetCableDetailFoundCount: {_lastHardwareTopologyTargetCableDetailFoundCount}",
                $"LastHardwareTopologyTargetCableParentServerCount: {_lastHardwareTopologyTargetCableParentServerCount}",
                $"LastHardwareTopologyTargetCableParentSwitchCount: {_lastHardwareTopologyTargetCableParentSwitchCount}",
                $"LastHardwareTopologyTargetCableParentPatchPanelCount: {_lastHardwareTopologyTargetCableParentPatchPanelCount}",
                $"LastHardwareTopologyTargetCableParentInternetCount: {_lastHardwareTopologyTargetCableParentInternetCount}",
                $"LastHardwareTopologyTargetCableInsertedSfpCount: {_lastHardwareTopologyTargetCableInsertedSfpCount}",
                $"LastHardwareTopologyTargetCableSfpPortCount: {_lastHardwareTopologyTargetCableSfpPortCount}",
                $"LastHardwareTopologyTargetCableEndpointCount: {_lastHardwareTopologyTargetCableEndpointCount}",
                $"LastHardwareTopologyTargetHierarchyTargetCount: {_lastHardwareTopologyTargetHierarchyTargetCount}",
                $"LastHardwareTopologyTargetHierarchyMatchedTargetCount: {_lastHardwareTopologyTargetHierarchyMatchedTargetCount}",
                $"LastHardwareTopologyTargetHierarchyObjectCount: {_lastHardwareTopologyTargetHierarchyObjectCount}",
                $"LastHardwareTopologyTargetHierarchyServerAncestorCount: {_lastHardwareTopologyTargetHierarchyServerAncestorCount}",
                $"LastHardwareTopologyTargetHierarchyNetworkDeviceAncestorCount: {_lastHardwareTopologyTargetHierarchyNetworkDeviceAncestorCount}",
                $"LastHardwareTopologyTargetHierarchyPatchPanelAncestorCount: {_lastHardwareTopologyTargetHierarchyPatchPanelAncestorCount}",
                $"LastHardwareTopologyTargetHierarchyInternetAncestorCount: {_lastHardwareTopologyTargetHierarchyInternetAncestorCount}",
                $"LastHardwareTopologyTargetHierarchyRackAncestorCount: {_lastHardwareTopologyTargetHierarchyRackAncestorCount}",
                $"LastHardwareTopologyTargetHierarchySample: {_lastHardwareTopologyTargetHierarchySample}",
                $"LastHardwareTopologyTargetHierarchyError: {_lastHardwareTopologyTargetHierarchyError}",
                $"LastHardwareTopologyCustomerBaseRootCount: {_lastHardwareTopologyCustomerBaseRootCount}",
                $"LastHardwareTopologyCustomerBaseSubtreeObjectCount: {_lastHardwareTopologyCustomerBaseSubtreeObjectCount}",
                $"LastHardwareTopologyCustomerBaseSubtreeIl2CppTypeCount: {_lastHardwareTopologyCustomerBaseSubtreeIl2CppTypeCount}",
                $"LastHardwareTopologyCustomerBaseSubtreeNetworkSwitchCount: {_lastHardwareTopologyCustomerBaseSubtreeNetworkSwitchCount}",
                $"LastHardwareTopologyCustomerBaseSubtreeRouterCount: {_lastHardwareTopologyCustomerBaseSubtreeRouterCount}",
                $"LastHardwareTopologyCustomerBaseSubtreeFirewallCount: {_lastHardwareTopologyCustomerBaseSubtreeFirewallCount}",
                $"LastHardwareTopologyCustomerBaseSubtreeServerCount: {_lastHardwareTopologyCustomerBaseSubtreeServerCount}",
                $"LastHardwareTopologyCustomerBaseSubtreePatchPanelCount: {_lastHardwareTopologyCustomerBaseSubtreePatchPanelCount}",
                $"LastHardwareTopologyCustomerBaseSubtreeInternetCount: {_lastHardwareTopologyCustomerBaseSubtreeInternetCount}",
                $"LastHardwareTopologyCustomerBaseSubtreeRackCount: {_lastHardwareTopologyCustomerBaseSubtreeRackCount}",
                $"LastHardwareTopologyCustomerBaseSubtreeCableLinkCount: {_lastHardwareTopologyCustomerBaseSubtreeCableLinkCount}",
                $"LastHardwareTopologyCustomerBaseSubtreeAtResultLimit: {_lastHardwareTopologyCustomerBaseSubtreeAtResultLimit}",
                $"LastHardwareTopologyCustomerBaseSubtreeTypes: {_lastHardwareTopologyCustomerBaseSubtreeTypes}",
                $"LastHardwareTopologyCustomerBaseSubtreeSample: {_lastHardwareTopologyCustomerBaseSubtreeSample}",
                $"LastHardwareTopologyCustomerBaseSubtreeError: {_lastHardwareTopologyCustomerBaseSubtreeError}",
                $"LastCustomerBaseStateProbeComponentCount: {_lastCustomerBaseStateProbeComponentCount}",
                $"LastCustomerBaseStateProbeFieldCount: {_lastCustomerBaseStateProbeFieldCount}",
                $"LastCustomerBaseStateProbePropertyCount: {_lastCustomerBaseStateProbePropertyCount}",
                $"LastCustomerBaseStateProbeValueCount: {_lastCustomerBaseStateProbeValueCount}",
                $"LastCustomerBaseStateProbeReferenceCount: {_lastCustomerBaseStateProbeReferenceCount}",
                $"LastCustomerBaseStateProbeScalarCount: {_lastCustomerBaseStateProbeScalarCount}",
                $"LastCustomerBaseStateProbeNullCount: {_lastCustomerBaseStateProbeNullCount}",
                $"LastCustomerBaseStateProbeUnsupportedCount: {_lastCustomerBaseStateProbeUnsupportedCount}",
                $"LastCustomerBaseStateProbeUnavailableCount: {_lastCustomerBaseStateProbeUnavailableCount}",
                $"LastCustomerBaseStateProbeFields: {_lastCustomerBaseStateProbeFields}",
                $"LastCustomerBaseStateProbeProperties: {_lastCustomerBaseStateProbeProperties}",
                $"LastCustomerBaseRelatedTypeSummary: {_lastCustomerBaseRelatedTypeSummary}",
                $"LastCustomerBaseStateProbeReferenceTypes: {_lastCustomerBaseStateProbeReferenceTypes}",
                $"LastCustomerBaseStateProbeUnsupportedTypes: {_lastCustomerBaseStateProbeUnsupportedTypes}",
                $"LastCustomerBaseStateProbeSample: {_lastCustomerBaseStateProbeSample}",
                $"LastCustomerBaseStateProbeError: {_lastCustomerBaseStateProbeError}",
                $"LastCustomerBaseCableLinkCollectionBaseCount: {_lastCustomerBaseCableLinkCollectionBaseCount}",
                $"LastCustomerBaseCableLinkCollectionDeclaredCount: {_lastCustomerBaseCableLinkCollectionDeclaredCount}",
                $"LastCustomerBaseCableLinkCollectionReferenceCount: {_lastCustomerBaseCableLinkCollectionReferenceCount}",
                $"LastCustomerBaseCableLinkCollectionUniqueReferenceCount: {_lastCustomerBaseCableLinkCollectionUniqueReferenceCount}",
                $"LastCustomerBaseCableLinkCollectionTopologyTargetCount: {_lastCustomerBaseCableLinkCollectionTopologyTargetCount}",
                $"LastCustomerBaseCableLinkCollectionTopologyTargetMatchCount: {_lastCustomerBaseCableLinkCollectionTopologyTargetMatchCount}",
                $"LastCustomerBaseCableLinkCollectionNonTargetReferenceCount: {_lastCustomerBaseCableLinkCollectionNonTargetReferenceCount}",
                $"LastCustomerBaseCableLinkCollectionSample: {_lastCustomerBaseCableLinkCollectionSample}",
                $"LastHardwareTopologySample: {_lastHardwareTopologySample}",
                $"LastHardwareTopologyError: {_lastHardwareTopologyError}",
                $"LastHardwareSnapshotError: {_lastHardwareSnapshotError}",
                $"LastHardwareSnapshotSample: {_lastHardwareSnapshotSample}");

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
                targetedSemanticLines,
                componentInventoryLines,
                gameTypeCatalogLines,
                gameResourceDiscoveryLines,
                gameTypeInspectionLines,
                cablePersistenceMetadataLines,
                gameThreadLines,
                hardwareSnapshotLines,
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

    private sealed class GameTypeKeywordResult
    {
        public GameTypeKeywordResult(
            string keyword,
            IReadOnlyList<DCMLGameTypeInfo> types)
        {
            Keyword =
                keyword;

            Types =
                types;
        }

        public string Keyword { get; }

        public IReadOnlyList<DCMLGameTypeInfo> Types { get; }
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

        public bool EnableAutomaticSceneDiagnostics { get; set; }

        public int SceneDiagnosticDelayFrames { get; set; } =
            DefaultAutomaticSceneDiagnosticDelayFrames;

        public bool EnableHeavyAutomaticSceneDiagnostics { get; set; }

        public bool EnableCablePersistenceMetadataProbe { get; set; }

        public int CablePersistenceProbeDelayFrames { get; set; } =
            900;

        public bool EnablePhysicalCablePersistenceSource { get; set; }

        public string PhysicalCableSavePath { get; set; } =
            string.Empty;

        public string PhysicalCableHelperHostPath { get; set; } =
            string.Empty;

        public string PhysicalCableHelperDllPath { get; set; } =
            string.Empty;

        public bool EnablePhysicalCablePersistenceSourceProbe { get; set; }
    }
}
