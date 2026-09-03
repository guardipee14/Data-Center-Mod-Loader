using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using DCML.Core.Runtime;
using DCML.Core.Services;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLMultiModuleIntegrationTests
{
    [Fact]
    public void ProbeManifests_AreValidAndDeclareRequiredDependency()
    {
        string root =
            GetSolutionRoot();

        string publisherJson =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "DCML.MultiModuleProbe.Publisher",
                    "manifest.json"));

        string consumerJson =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "DCML.MultiModuleProbe.Consumer",
                    "manifest.json"));

        var publisher =
            DCMLManifestJson.Deserialize(
                publisherJson);

        var consumer =
            DCMLManifestJson.Deserialize(
                consumerJson);

        Assert.True(
            publisher.Success);

        Assert.True(
            consumer.Success);

        Assert.NotNull(
            publisher.Manifest);

        Assert.NotNull(
            consumer.Manifest);

        Assert.Equal(
            "dcml.probe.publisher",
            publisher.Manifest!.Id);

        DCMLModuleDependency dependency =
            Assert.Single(
                consumer.Manifest!.Dependencies);

        Assert.Equal(
            publisher.Manifest.Id,
            dependency.Id);

        Assert.Equal(
            "1.0.0",
            dependency.MinimumVersion);

        Assert.False(
            dependency.Optional);
    }

    [Fact]
    public void RealPackages_DiscoverResolveActivateAndExchangeEvent()
    {
        using var environment =
            ProbeEnvironment.Create();

        Assert.Empty(
            environment.Discovery.Failures);

        Assert.Equal(
            2,
            environment.Discovery.Packages.Count);

        Assert.True(
            environment.Resolution.Success);

        Assert.Equal(
            new[]
            {
                "dcml.probe.publisher",
                "dcml.probe.consumer"
            },
            environment.Resolution.LoadOrder
                .Select(
                    package =>
                        package.Manifest.Id)
                .ToArray());

        DCMLModuleRuntimeResult result =
            environment.Runtime.Start(
                environment.Resolution.LoadOrder);

        Assert.True(
            result.Success);

        Assert.All(
            result.Modules,
            module =>
                Assert.Equal(
                    DCMLModuleRuntimeState.Running,
                    module.State));

        string[] publisherTrace =
            environment.ReadTrace(
                "dcml.probe.publisher");

        string[] consumerTrace =
            environment.ReadTrace(
                "dcml.probe.consumer");

        Assert.Equal(
            new[]
            {
                "Initialize",
                "Start",
                "RequestReceived",
                "ResponsePublishing",
                "ResponsePublished"
            },
            publisherTrace);

        Assert.Equal(
            new[]
            {
                "Initialize",
                "Start",
                "RequestPublishing",
                "ResponseReceived",
                "HandshakeComplete"
            },
            consumerTrace);
    }

    [Fact]
    public void RealPackages_StopConsumerBeforePublisher()
    {
        using var environment =
            ProbeEnvironment.Create();

        DCMLModuleRuntimeResult startResult =
            environment.Runtime.Start(
                environment.Resolution.LoadOrder);

        Assert.True(
            startResult.Success);

        DCMLModuleRuntimeResult stopResult =
            environment.Runtime.Stop();

        Assert.True(
            stopResult.Success);

        string[] publisherTrace =
            environment.ReadTrace(
                "dcml.probe.publisher");

        string[] consumerTrace =
            environment.ReadTrace(
                "dcml.probe.consumer");

        Assert.Equal(
            "Stop",
            consumerTrace[^1]);

        int observedIndex =
            Array.IndexOf(
                publisherTrace,
                "ConsumerStopObserved");

        int publisherStopIndex =
            Array.IndexOf(
                publisherTrace,
                "Stop");

        Assert.True(
            observedIndex >= 0);

        Assert.True(
            publisherStopIndex >
            observedIndex);
    }

    private static string GetSolutionRoot()
    {
        return
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    ".."));
    }

    private sealed class ProbeEnvironment :
        IDisposable
    {
        private ProbeEnvironment(
            string root,
            string dataRoot,
            DCMLPackageDiscoveryResult discovery,
            DCMLDependencyResolutionResult resolution,
            DCMLModuleRuntime runtime)
        {
            Root =
                root;

            DataRoot =
                dataRoot;

            Discovery =
                discovery;

            Resolution =
                resolution;

            Runtime =
                runtime;
        }

        public string Root { get; }

        public string DataRoot { get; }

        public DCMLPackageDiscoveryResult Discovery { get; }

        public DCMLDependencyResolutionResult Resolution { get; }

        public DCMLModuleRuntime Runtime { get; }

        public static ProbeEnvironment Create()
        {
            string solutionRoot =
                GetSolutionRoot();

            string root =
                Path.Combine(
                    Path.GetTempPath(),
                    "DCML-MultiModuleProbe",
                    Guid.NewGuid().ToString("N"));

            string modulesRoot =
                Path.Combine(
                    root,
                    "Modules");

            string dataRoot =
                Path.Combine(
                    root,
                    "Data");

            Directory.CreateDirectory(
                modulesRoot);

            Directory.CreateDirectory(
                dataRoot);

            StagePackage(
                solutionRoot,
                modulesRoot,
                "DCML.MultiModuleProbe.Publisher");

            StagePackage(
                solutionRoot,
                modulesRoot,
                "DCML.MultiModuleProbe.Consumer");

            DCMLPackageDiscoveryResult discovery =
                DCMLPackageDiscovery.Discover(
                    modulesRoot);

            DCMLDependencyResolutionResult resolution =
                DCMLDependencyResolver.Resolve(
                    discovery.Packages);

            var eventBus =
                new DCMLEventBus();

            var runtime =
                new DCMLModuleRuntime(
                    new DCMLReflectionModuleActivator(),
                    new ProbeContextFactory(
                        dataRoot,
                        eventBus));

            return
                new ProbeEnvironment(
                    root,
                    dataRoot,
                    discovery,
                    resolution,
                    runtime);
        }

        public string[] ReadTrace(
            string moduleId)
        {
            string path =
                Path.Combine(
                    DataRoot,
                    moduleId,
                    "multimodule-probe.log");

            Assert.True(
                File.Exists(path),
                "Probe trace was not created: " +
                path);

            return
                File.ReadAllLines(
                    path);
        }

        public void Dispose()
        {
            if (Runtime.IsRunning)
            {
                Runtime.Stop();
            }

            try
            {
                Directory.Delete(
                    Root,
                    true);
            }
            catch
            {
                // Assembly.LoadFrom may keep package files locked on Windows
                // until the test process exits. The probe uses a unique temp
                // root, so cleanup failure is harmless and must not hide a
                // runtime assertion.
            }
        }

        private static void StagePackage(
            string solutionRoot,
            string modulesRoot,
            string projectName)
        {
            string sourceRoot =
                Path.Combine(
                    solutionRoot,
                    "src",
                    projectName);

            string assemblySource =
                Path.Combine(
                    sourceRoot,
                    "bin",
                    "Release",
                    "net6.0",
                    projectName + ".dll");

            string manifestSource =
                Path.Combine(
                    sourceRoot,
                    "manifest.json");

            Assert.True(
                File.Exists(assemblySource),
                "Probe assembly was not built: " +
                assemblySource);

            Assert.True(
                File.Exists(manifestSource),
                "Probe manifest was not found: " +
                manifestSource);

            string packageRoot =
                Path.Combine(
                    modulesRoot,
                    projectName);

            Directory.CreateDirectory(
                packageRoot);

            File.Copy(
                assemblySource,
                Path.Combine(
                    packageRoot,
                    projectName + ".dll"));

            File.Copy(
                manifestSource,
                Path.Combine(
                    packageRoot,
                    "manifest.json"));
        }
    }

    private sealed class ProbeContextFactory :
        IDCMLModuleContextFactory
    {
        private readonly string _dataRoot;

        private readonly IDCMLEventBus _eventBus;

        public ProbeContextFactory(
            string dataRoot,
            IDCMLEventBus eventBus)
        {
            _dataRoot =
                dataRoot;

            _eventBus =
                eventBus;
        }

        public IDCMLModuleContext CreateContext(
            DCMLModulePackage package)
        {
            string dataDirectory =
                Path.Combine(
                    _dataRoot,
                    package.Manifest.Id);

            Directory.CreateDirectory(
                dataDirectory);

            return
                new ProbeContext(
                    package.PackageDirectory,
                    dataDirectory,
                    new DCMLServiceProvider(
                        (
                            typeof(IDCMLEventBus),
                            _eventBus
                        )));
        }
    }

    private sealed class ProbeContext :
        IDCMLModuleContext
    {
        public ProbeContext(
            string moduleDirectory,
            string dataDirectory,
            IServiceProvider services)
        {
            ModuleDirectory =
                moduleDirectory;

            DataDirectory =
                dataDirectory;

            Services =
                services;
        }

        public string ModuleDirectory { get; }

        public string DataDirectory { get; }

        public IServiceProvider Services { get; }
    }
}
