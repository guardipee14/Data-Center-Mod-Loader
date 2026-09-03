using System;
using System.IO;
using System.Linq;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using DCML.Core.Runtime;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLLoaderAcceptanceIndependenceTests
{
    [Fact]
    public void MinimalProbeProject_ReferencesCoreOnly()
    {
        string root =
            GetSolutionRoot();

        string projectPath =
            Path.Combine(
                root,
                "src",
                "DCML.LoaderAcceptanceProbe.Minimal",
                "DCML.LoaderAcceptanceProbe.Minimal.csproj");

        string source =
            File.ReadAllText(
                projectPath);

        Assert.Contains(
            @"..\DCML.Core\DCML.Core.csproj",
            source);

        Assert.DoesNotContain(
            "DCML.DataCenter",
            source,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            "DCML.Loader.MelonLoader",
            source,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            "MelonLoader",
            source,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MinimalProbeSource_DoesNotRequestOptionalServices()
    {
        string root =
            GetSolutionRoot();

        string modulePath =
            Path.Combine(
                root,
                "src",
                "DCML.LoaderAcceptanceProbe.Minimal",
                "MinimalModule.cs");

        string source =
            File.ReadAllText(
                modulePath);

        Assert.DoesNotContain(
            "GetService",
            source,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "IDCMLLogger",
            source,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "IDCMLConfiguration",
            source,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "IDCMLEventBus",
            source,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "IDCMLGame",
            source,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "DCML.DataCenter",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void MinimalProbeManifest_OmitsCapabilityRequirements()
    {
        string root =
            GetSolutionRoot();

        string manifestPath =
            Path.Combine(
                root,
                "src",
                "DCML.LoaderAcceptanceProbe.Minimal",
                "manifest.json");

        string json =
            File.ReadAllText(
                manifestPath);

        Assert.DoesNotContain(
            "requiredCapabilities",
            json,
            StringComparison.OrdinalIgnoreCase);

        DCMLManifestReadResult parsed =
            DCMLManifestJson.Deserialize(
                json);

        Assert.True(
            parsed.Success);

        Assert.NotNull(
            parsed.Manifest);

        Assert.Empty(
            parsed.Manifest!.RequiredCapabilities);
    }

    [Fact]
    public void MinimalProbe_PassesCompatibilityWithEmptyHostCapabilityCatalog()
    {
        using ProbeEnvironment environment =
            ProbeEnvironment.Create();

        Assert.Empty(
            environment.Discovery.Failures);

        DCMLPackageCompatibilityResult compatibility =
            DCMLPackageCompatibilityEvaluator.Evaluate(
                environment.Discovery.Packages,
                "0.0.3",
                Array.Empty<DCMLCapabilityDescriptor>());

        Assert.True(
            compatibility.Success);

        Assert.Single(
            compatibility.CompatiblePackages);

        Assert.Empty(
            compatibility.IncompatiblePackages);

        Assert.Empty(
            compatibility.Issues);
    }

    [Fact]
    public void MinimalProbe_DiscoverResolveActivateAndRunWithoutOptionalServices()
    {
        using ProbeEnvironment environment =
            ProbeEnvironment.Create();

        Assert.Empty(
            environment.Discovery.Failures);

        DCMLPackageCompatibilityResult compatibility =
            DCMLPackageCompatibilityEvaluator.Evaluate(
                environment.Discovery.Packages,
                "0.0.3",
                Array.Empty<DCMLCapabilityDescriptor>());

        DCMLDependencyResolutionResult resolution =
            DCMLDependencyResolver.Resolve(
                compatibility.CompatiblePackages);

        Assert.True(
            resolution.Success);

        Assert.Empty(
            resolution.Issues);

        Assert.Single(
            resolution.LoadOrder);

        DCMLModuleRuntimeResult runtimeResult =
            environment.Runtime.Start(
                resolution.LoadOrder);

        Assert.True(
            runtimeResult.Success);

        var record =
            Assert.Single(
                runtimeResult.Modules);

        Assert.Equal(
            "dcml.probe.loader-acceptance-minimal",
            record.ModuleId);

        Assert.Equal(
            DCMLModuleRuntimeState.Running,
            record.State);

        Assert.Empty(
            runtimeResult.Issues);

        string[] trace =
            File.ReadAllLines(
                environment.TracePath);

        Assert.Equal(
            new[]
            {
                "Initialize",
                "Start"
            },
            trace);

        DCMLModuleRuntimeResult stopResult =
            environment.Runtime.Stop();

        Assert.True(
            stopResult.Success);

        Assert.Equal(
            "Stop",
            File.ReadAllLines(
                environment.TracePath)[^1]);
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
            DCMLModuleRuntime runtime)
        {
            Root =
                root;

            DataRoot =
                dataRoot;

            Discovery =
                discovery;

            Runtime =
                runtime;
        }

        public string Root { get; }

        public string DataRoot { get; }

        public DCMLPackageDiscoveryResult Discovery { get; }

        public DCMLModuleRuntime Runtime { get; }

        public string TracePath =>
            Path.Combine(
                DataRoot,
                "dcml.probe.loader-acceptance-minimal",
                "loader-acceptance-probe.log");

        public static ProbeEnvironment Create()
        {
            string solutionRoot =
                GetSolutionRoot();

            string root =
                Path.Combine(
                    Path.GetTempPath(),
                    "DCML-LoaderAcceptanceProbe",
                    Guid.NewGuid().ToString("N"));

            string modulesRoot =
                Path.Combine(
                    root,
                    "Modules");

            string dataRoot =
                Path.Combine(
                    root,
                    "Data");

            string sourceRoot =
                Path.Combine(
                    solutionRoot,
                    "src",
                    "DCML.LoaderAcceptanceProbe.Minimal");

            string packageRoot =
                Path.Combine(
                    modulesRoot,
                    "Minimal");

            Directory.CreateDirectory(
                packageRoot);

            Directory.CreateDirectory(
                dataRoot);

            string assemblySource =
                Path.Combine(
                    sourceRoot,
                    "bin",
                    "Release",
                    "net6.0",
                    "DCML.LoaderAcceptanceProbe.Minimal.dll");

            string manifestSource =
                Path.Combine(
                    sourceRoot,
                    "manifest.json");

            Assert.True(
                File.Exists(
                    assemblySource));

            File.Copy(
                assemblySource,
                Path.Combine(
                    packageRoot,
                    "DCML.LoaderAcceptanceProbe.Minimal.dll"));

            File.Copy(
                manifestSource,
                Path.Combine(
                    packageRoot,
                    "manifest.json"));

            DCMLPackageDiscoveryResult discovery =
                DCMLPackageDiscovery.Discover(
                    modulesRoot);

            var runtime =
                new DCMLModuleRuntime(
                    new DCMLReflectionModuleActivator(),
                    new EmptyContextFactory(
                        dataRoot));

            return
                new ProbeEnvironment(
                    root,
                    dataRoot,
                    discovery,
                    runtime);
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
                // Reflection-loaded assemblies may remain locked until
                // test-process exit on Windows.
            }
        }
    }

    private sealed class EmptyContextFactory :
        IDCMLModuleContextFactory
    {
        private readonly string _dataRoot;

        public EmptyContextFactory(
            string dataRoot)
        {
            _dataRoot =
                dataRoot;
        }

        public IDCMLModuleContext CreateContext(
            DCMLModulePackage package)
        {
            string dataDirectory =
                Path.Combine(
                    _dataRoot,
                    package.Manifest.Id);

            return
                new EmptyContext(
                    package.PackageDirectory,
                    dataDirectory);
        }
    }

    private sealed class EmptyContext :
        IDCMLModuleContext
    {
        public EmptyContext(
            string moduleDirectory,
            string dataDirectory)
        {
            ModuleDirectory =
                moduleDirectory;

            DataDirectory =
                dataDirectory;
        }

        public string ModuleDirectory { get; }

        public string DataDirectory { get; }

        public IServiceProvider Services =>
            EmptyServiceProvider.Instance;
    }

    private sealed class EmptyServiceProvider :
        IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } =
            new EmptyServiceProvider();

        public object? GetService(
            Type serviceType)
        {
            return null;
        }
    }
}
