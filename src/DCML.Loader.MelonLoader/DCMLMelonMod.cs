using System;
using System.IO;
using System.Linq;
using DCML.Core.Models;
using DCML.Core.Runtime;
using DCML.Core.Services;
using MelonLoader;
using MelonLoader.Utils;

namespace DCML.Loader.MelonLoader
{
    public sealed class DCMLMelonMod : MelonMod
    {
        private DCMLModuleRuntime _runtime;

        private DCMLEventBus _eventBus;

        private MelonGameLifecycle _gameLifecycle;

        private MelonGameObjectDiscovery _gameObjectDiscovery;

        private MelonGameTypeCatalog _gameTypeCatalog;

        private MelonGameResourceDiscovery _gameResourceDiscovery;

        private MelonGameTypeInspector _gameTypeInspector;

        private DCMLGameThreadDispatcher _gameThread;

        private MelonGameComponentStateReader _gameComponentStateReader;

        public override void OnInitializeMelon()
        {
            try
            {
                StartDCML();
            }
            catch (Exception exception)
            {
                LoggerInstance.Error(
                    "[DCML] Host initialization failed.");

                LoggerInstance.Error(
                    exception.ToString());
            }
        }

        public override void OnUpdate()
        {
            if (_gameThread == null)
            {
                return;
            }

            try
            {
                _gameThread.Drain();
            }
            catch (Exception exception)
            {
                LoggerInstance.Error(
                    "[DCML] Game-thread dispatch failed.");

                LoggerInstance.Error(
                    exception.ToString());
            }
        }

        public override void OnDeinitializeMelon()
        {
            if (_runtime == null)
            {
                return;
            }

            DCMLModuleRuntimeResult stopResult =
                _runtime.Stop();

            foreach (DCMLModuleRuntimeIssue issue in stopResult.Issues)
            {
                LoggerInstance.Error(
                    "[DCML] " +
                    issue.Code +
                    " [" +
                    issue.ModuleId +
                    "] " +
                    issue.Message);
            }

            LoggerInstance.Msg(
                "[DCML] Runtime stopped.");
        }

        public override void OnSceneWasLoaded(
            int buildIndex,
            string sceneName)
        {
            ReportSceneLifecycle(
                DCMLSceneLifecycleStage.Loaded,
                buildIndex,
                sceneName);
        }

        public override void OnSceneWasInitialized(
            int buildIndex,
            string sceneName)
        {
            ReportSceneLifecycle(
                DCMLSceneLifecycleStage.Initialized,
                buildIndex,
                sceneName);
        }

        public override void OnSceneWasUnloaded(
            int buildIndex,
            string sceneName)
        {
            ReportSceneLifecycle(
                DCMLSceneLifecycleStage.Unloaded,
                buildIndex,
                sceneName);
        }

        private void StartDCML()
        {
            string modulesRoot =
                GetModulesRoot();

            string dataRoot =
                Path.Combine(
                    MelonEnvironment.UserDataDirectory,
                    "DCML",
                    "Data");

            string gameRoot =
                GetGameRoot();

            Directory.CreateDirectory(
                modulesRoot);

            Directory.CreateDirectory(
                dataRoot);

            LoggerInstance.Msg(
                "[DCML] Modules root: " +
                modulesRoot);

            DCMLPackageDiscoveryResult discovery =
                DCMLPackageDiscovery.Discover(
                    modulesRoot);

            foreach (DCMLPackageDiscoveryFailure failure in discovery.Failures)
            {
                LoggerInstance.Warning(
                    "[DCML] Discovery " +
                    failure.ErrorCode +
                    " [" +
                    failure.PackageDirectory +
                    "] " +
                    failure.ErrorMessage);

                foreach (DCMLValidationIssue issue in failure.ValidationIssues)
                {
                    LoggerInstance.Warning(
                        "[DCML]   " +
                        issue.Code +
                        ": " +
                        issue.Message);
                }
            }

            DCMLDependencyResolutionResult resolution =
                DCMLDependencyResolver.Resolve(
                    discovery.Packages);

            foreach (DCMLDependencyResolutionIssue issue in resolution.Issues)
            {
                LoggerInstance.Warning(
                    "[DCML] Resolution " +
                    issue.Code +
                    " [" +
                    issue.ModuleId +
                    "] " +
                    issue.Message);
            }

            _eventBus =
                new DCMLEventBus();

            _gameLifecycle =
                new MelonGameLifecycle(
                    _eventBus);

            _gameObjectDiscovery =
                new MelonGameObjectDiscovery();

            _gameTypeCatalog =
                new MelonGameTypeCatalog();

            _gameResourceDiscovery =
                new MelonGameResourceDiscovery();

            _gameTypeInspector =
                new MelonGameTypeInspector();

            _gameThread =
                new DCMLGameThreadDispatcher(
                    exception =>
                    {
                        LoggerInstance.Error(
                            "[DCML] A posted game-thread callback failed.");

                        LoggerInstance.Error(
                            exception.ToString());
                    });

            _gameComponentStateReader =
                new MelonGameComponentStateReader(
                    _gameThread);

            _runtime =
                new DCMLModuleRuntime(
                    new MelonModuleActivator(),
                    new MelonModuleContextFactory(
                        dataRoot,
                        gameRoot,
                        _eventBus,
                        _gameLifecycle,
                        _gameObjectDiscovery,
                        _gameTypeCatalog,
                        _gameResourceDiscovery,
                        _gameTypeInspector,
                        _gameThread,
                        _gameComponentStateReader));

            DCMLModuleRuntimeResult startResult =
                _runtime.Start(
                    resolution.LoadOrder);

            foreach (DCMLModuleRuntimeIssue issue in startResult.Issues)
            {
                LoggerInstance.Error(
                    "[DCML] Runtime " +
                    issue.Code +
                    " [" +
                    issue.ModuleId +
                    "] " +
                    issue.Message);
            }

            int runningCount =
                startResult.Modules.Count(
                    module =>
                        module.State ==
                        DCMLModuleRuntimeState.Running);

            LoggerInstance.Msg(
                "[DCML] Discovery complete. " +
                discovery.Packages.Count +
                " valid package(s), " +
                discovery.Failures.Count +
                " discovery failure(s), " +
                runningCount +
                " running module(s).");
        }

        private void ReportSceneLifecycle(
            DCMLSceneLifecycleStage stage,
            int buildIndex,
            string sceneName)
        {
            if (_gameLifecycle == null)
            {
                return;
            }

            try
            {
                _gameLifecycle.Report(
                    stage,
                    buildIndex,
                    sceneName);

                LoggerInstance.Msg(
                    "[DCML] Scene " +
                    stage +
                    ": " +
                    buildIndex +
                    " '" +
                    (sceneName ?? string.Empty) +
                    "'.");
            }
            catch (Exception exception)
            {
                LoggerInstance.Error(
                    "[DCML] Scene lifecycle delivery failed.");

                LoggerInstance.Error(
                    exception.ToString());
            }
        }

        private static string GetModulesRoot()
        {
            string overrideRoot =
                Environment.GetEnvironmentVariable(
                    "DCML_MODULES_ROOT");

            if (!string.IsNullOrWhiteSpace(overrideRoot))
            {
                return Path.GetFullPath(
                    overrideRoot);
            }

            return Path.Combine(
                MelonEnvironment.UserDataDirectory,
                "DCML",
                "Modules");
        }

        private static string GetGameRoot()
        {
            string userDataDirectory =
                MelonEnvironment.UserDataDirectory;

            if (string.IsNullOrWhiteSpace(userDataDirectory))
            {
                throw new InvalidOperationException(
                    "MelonLoader did not provide a UserData directory.");
            }

            string fullUserDataDirectory =
                Path.GetFullPath(
                    userDataDirectory);

            DirectoryInfo parent =
                Directory.GetParent(
                    fullUserDataDirectory);

            if (parent == null)
            {
                throw new InvalidOperationException(
                    "DCML could not derive the game root from MelonLoader's UserData directory.");
            }

            return parent.FullName;
        }
    }
}
