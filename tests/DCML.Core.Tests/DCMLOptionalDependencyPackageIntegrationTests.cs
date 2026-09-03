using System;
using System.IO;
using System.Linq;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using DCML.Core.Runtime;
using DCML.Core.Services;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLOptionalDependencyPackageIntegrationTests
{
    [Fact]
    public void ConsumerManifest_DeclaresProviderAsOptional()
    {
        string root =
            GetSolutionRoot();

        string json =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "DCML.OptionalProbe.Consumer",
                    "manifest.json"));

        var parsed =
            DCMLManifestJson.Deserialize(
                json);

        Assert.True(
            parsed.Success);

        Assert.NotNull(
            parsed.Manifest);

        DCMLModuleDependency dependency =
            Assert.Single(
                parsed.Manifest!.Dependencies);

        Assert.Equal(
            "dcml.probe.optional-provider",
            dependency.Id);

        Assert.Equal(
            "1.0.0",
            dependency.MinimumVersion);

        Assert.True(
            dependency.Optional);
    }

    [Fact]
    public void ConsumerPackage_RunsWhenOptionalProviderIsAbsent()
    {
        using var environment =
            ProbeEnvironment.Create(
                includeProvider: false);

        Assert.Empty(
            environment.Discovery.Failures);

        DCMLModulePackage consumer =
            Assert.Single(
                environment.Discovery.Packages);

        Assert.Equal(
            "dcml.probe.optional-consumer",
            consumer.Manifest.Id);

        Assert.True(
            environment.Resolution.Success);

        Assert.Empty(
            environment.Resolution.Issues);

        Assert.Single(
            environment.Resolution.LoadOrder);

        DCMLModuleRuntimeResult result =
            environment.Runtime.Start(
                environment.Resolution.LoadOrder);

        Assert.True(
            result.Success);

        Assert.Equal(
            DCMLModuleRuntimeState.Running,
            Assert.Single(
                result.Modules).State);

        string[] trace =
            environment.ReadTrace(
                "dcml.probe.optional-consumer");

        Assert.Contains(
            "ConsumerRunning",
            trace);

        Assert.Contains(
            "OptionalProviderNotObservedDuringQuery",
            trace);

        Assert.DoesNotContain(
            "OptionalProviderObserved",
            trace);
    }

    [Fact]
    public void ConsumerAndProvider_BothRunWhenOptionalProviderIsPresent()
    {
        using var environment =
            ProbeEnvironment.Create(
                includeProvider: true);

        Assert.Empty(
            environment.Discovery.Failures);

        Assert.Equal(
            2,
            environment.Discovery.Packages.Count);

        Assert.True(
            environment.Resolution.Success);

        Assert.Empty(
            environment.Resolution.Issues);

        Assert.Equal(
            new[]
            {
                "dcml.probe.optional-consumer",
                "dcml.probe.optional-provider"
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

        string[] consumerTrace =
            environment.ReadTrace(
                "dcml.probe.optional-consumer");

        string[] providerTrace =
            environment.ReadTrace(
                "dcml.probe.optional-provider");

        Assert.Contains(
            "OptionalProviderNotObservedDuringQuery",
            consumerTrace);

        Assert.Contains(
            "OptionalProviderObserved",
            consumerTrace);

        Assert.Contains(
            "PresencePublished",
            providerTrace);

        Assert.True(
            Array.IndexOf(
                consumerTrace,
                "ConsumerRunning") <
            Array.IndexOf(
                consumerTrace,
                "OptionalProviderObserved"));
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

        public static ProbeEnvironment Create(
            bool includeProvider)
        {
            string solutionRoot =
                GetSolutionRoot();

            string root =
                Path.Combine(
                    Path.GetTempPath(),
                    "DCML-OptionalDependencyProbe",
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
                "DCML.OptionalProbe.Consumer");

            if (includeProvider)
            {
                StagePackage(
                    solutionRoot,
                    modulesRoot,
                    "DCML.OptionalProbe.Provider");
            }

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
                    "optional-dependency-probe.log");

            Assert.True(
                File.Exists(path),
                "Optional dependency trace was not created: " +
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
                // Reflection-loaded test DLLs can remain locked until the
                // process exits. Unique temp roots make deferred cleanup safe.
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
                "Optional probe assembly was not built: " +
                assemblySource);

            Assert.True(
                File.Exists(manifestSource),
                "Optional probe manifest was not found: " +
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
