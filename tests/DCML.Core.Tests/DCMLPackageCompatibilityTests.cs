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

public sealed class DCMLPackageCompatibilityTests
{
    [Fact]
    public void OldManifestJson_DefaultsRequiredCapabilitiesToEmpty()
    {
        const string json =
            """
            {
              "schemaVersion": 1,
              "id": "dcml.test.old",
              "name": "Old",
              "version": "1.0.0",
              "entryAssembly": "Old.dll",
              "entryType": "Old.Module",
              "dependencies": []
            }
            """;

        var parsed =
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
    public void ManifestJson_RoundTripsRequiredCapability()
    {
        DCMLModuleManifest manifest =
            CreateManifest(
                "dcml.test.roundtrip");

        manifest.RequiredCapabilities.Add(
            new DCMLCapabilityRequirement
            {
                Id =
                    DCMLRuntimeCapabilities.Events,
                MinimumVersion =
                    "1.0.0"
            });

        string json =
            DCMLManifestJson.Serialize(
                manifest);

        var parsed =
            DCMLManifestJson.Deserialize(
                json);

        Assert.True(
            parsed.Success);

        DCMLCapabilityRequirement requirement =
            Assert.Single(
                parsed.Manifest!.RequiredCapabilities);

        Assert.Equal(
            DCMLRuntimeCapabilities.Events,
            requirement.Id);

        Assert.Equal(
            "1.0.0",
            requirement.MinimumVersion);
    }

    [Fact]
    public void Validator_RejectsNullRequiredCapabilities()
    {
        DCMLModuleManifest manifest =
            CreateManifest(
                "dcml.test.null-capabilities");

        manifest.RequiredCapabilities =
            null!;

        DCMLValidationResult result =
            DCMLModuleManifestValidator.Validate(
                manifest);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                "DCML_MANIFEST_REQUIRED_CAPABILITIES_INVALID");
    }

    [Fact]
    public void Validator_RejectsNullCapabilityEntry()
    {
        DCMLModuleManifest manifest =
            CreateManifest(
                "dcml.test.null-capability");

        manifest.RequiredCapabilities.Add(
            null!);

        DCMLValidationResult result =
            DCMLModuleManifestValidator.Validate(
                manifest);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                "DCML_MANIFEST_REQUIRED_CAPABILITY_INVALID");
    }

    [Fact]
    public void Validator_RejectsMissingCapabilityId()
    {
        DCMLModuleManifest manifest =
            CreateManifest(
                "dcml.test.missing-capability-id");

        manifest.RequiredCapabilities.Add(
            new DCMLCapabilityRequirement());

        DCMLValidationResult result =
            DCMLModuleManifestValidator.Validate(
                manifest);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                "DCML_MANIFEST_REQUIRED_CAPABILITY_ID_REQUIRED");
    }

    [Fact]
    public void Validator_RejectsInvalidCapabilityMinimumVersion()
    {
        DCMLModuleManifest manifest =
            CreateManifest(
                "dcml.test.invalid-capability-version");

        manifest.RequiredCapabilities.Add(
            new DCMLCapabilityRequirement
            {
                Id =
                    DCMLRuntimeCapabilities.Events,
                MinimumVersion =
                    "1.0"
            });

        DCMLValidationResult result =
            DCMLModuleManifestValidator.Validate(
                manifest);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                "DCML_MANIFEST_REQUIRED_CAPABILITY_VERSION_INVALID");
    }

    [Fact]
    public void Validator_RejectsDuplicateCapabilityIdsCaseInsensitively()
    {
        DCMLModuleManifest manifest =
            CreateManifest(
                "dcml.test.duplicate-capability");

        manifest.RequiredCapabilities.Add(
            new DCMLCapabilityRequirement
            {
                Id =
                    "dcml.events"
            });

        manifest.RequiredCapabilities.Add(
            new DCMLCapabilityRequirement
            {
                Id =
                    "DCML.EVENTS"
            });

        DCMLValidationResult result =
            DCMLModuleManifestValidator.Validate(
                manifest);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                "DCML_MANIFEST_DUPLICATE_REQUIRED_CAPABILITY");
    }

    [Fact]
    public void Evaluator_AllowsPackageWhenRequirementsAreSatisfied()
    {
        DCMLModuleManifest manifest =
            CreateManifest(
                "dcml.test.satisfied");

        manifest.MinimumDCMLVersion =
            "0.0.3";

        manifest.RequiredCapabilities.Add(
            new DCMLCapabilityRequirement
            {
                Id =
                    DCMLRuntimeCapabilities.Events,
                MinimumVersion =
                    "1.0.0"
            });

        DCMLPackageCompatibilityResult result =
            Evaluate(
                manifest);

        Assert.True(
            result.Success);

        Assert.Single(
            result.CompatiblePackages);

        Assert.Empty(
            result.Issues);
    }

    [Fact]
    public void Evaluator_RejectsUnsatisfiedMinimumDCMLVersion()
    {
        DCMLModuleManifest manifest =
            CreateManifest(
                "dcml.test.newer-dcml");

        manifest.MinimumDCMLVersion =
            "0.0.4";

        DCMLPackageCompatibilityResult result =
            Evaluate(
                manifest);

        Assert.False(
            result.Success);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                "DCML_COMPATIBILITY_DCML_VERSION_UNSATISFIED");
    }

    [Fact]
    public void Evaluator_RejectsMissingCapability()
    {
        DCMLModuleManifest manifest =
            CreateManifest(
                "dcml.test.missing-capability");

        manifest.RequiredCapabilities.Add(
            new DCMLCapabilityRequirement
            {
                Id =
                    "dcml.test.not-present",
                MinimumVersion =
                    "1.0.0"
            });

        DCMLPackageCompatibilityResult result =
            Evaluate(
                manifest);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                "DCML_COMPATIBILITY_CAPABILITY_MISSING" &&
                issue.RequirementId ==
                "dcml.test.not-present");
    }

    [Fact]
    public void Evaluator_AllowsPresenceOnlyCapabilityRequirement()
    {
        DCMLModuleManifest manifest =
            CreateManifest(
                "dcml.test.presence-only");

        manifest.RequiredCapabilities.Add(
            new DCMLCapabilityRequirement
            {
                Id =
                    DCMLRuntimeCapabilities.Events
            });

        Assert.True(
            Evaluate(
                manifest)
            .Success);
    }

    [Fact]
    public void Evaluator_RejectsUnsatisfiedCapabilityVersion()
    {
        DCMLModuleManifest manifest =
            CreateManifest(
                "dcml.test.capability-version");

        manifest.RequiredCapabilities.Add(
            new DCMLCapabilityRequirement
            {
                Id =
                    DCMLRuntimeCapabilities.Events,
                MinimumVersion =
                    "1.1.0"
            });

        DCMLPackageCompatibilityResult result =
            Evaluate(
                manifest);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                "DCML_COMPATIBILITY_CAPABILITY_VERSION_UNSATISFIED");
    }

    [Fact]
    public void Evaluator_AllowsEqualCapabilityVersion()
    {
        DCMLModuleManifest manifest =
            CreateManifest(
                "dcml.test.capability-equal");

        manifest.RequiredCapabilities.Add(
            new DCMLCapabilityRequirement
            {
                Id =
                    DCMLRuntimeCapabilities.Events,
                MinimumVersion =
                    "1.0.0"
            });

        Assert.True(
            Evaluate(
                manifest)
            .Success);
    }

    [Fact]
    public void Evaluator_BlocksRequiredDependentOfIncompatiblePackage()
    {
        DCMLModuleManifest dependency =
            CreateManifest(
                "dcml.test.incompatible");

        dependency.RequiredCapabilities.Add(
            new DCMLCapabilityRequirement
            {
                Id =
                    "dcml.test.missing"
            });

        DCMLModuleManifest dependent =
            CreateManifest(
                "dcml.test.dependent");

        dependent.Dependencies.Add(
            new DCMLModuleDependency
            {
                Id =
                    dependency.Id,
                Optional =
                    false
            });

        DCMLPackageCompatibilityResult result =
            DCMLPackageCompatibilityEvaluator.Evaluate(
                new[]
                {
                    CreatePackage(
                        dependency),
                    CreatePackage(
                        dependent)
                },
                "0.0.3",
                CreateCapabilities());

        Assert.Equal(
            2,
            result.IncompatiblePackageCount);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.ModuleId ==
                    dependent.Id &&
                issue.Code ==
                    "DCML_COMPATIBILITY_DEPENDENCY_INCOMPATIBLE");
    }

    [Fact]
    public void Evaluator_DoesNotBlockOptionalDependentOfIncompatiblePackage()
    {
        DCMLModuleManifest dependency =
            CreateManifest(
                "dcml.test.optional-incompatible");

        dependency.RequiredCapabilities.Add(
            new DCMLCapabilityRequirement
            {
                Id =
                    "dcml.test.missing"
            });

        DCMLModuleManifest consumer =
            CreateManifest(
                "dcml.test.optional-consumer");

        consumer.Dependencies.Add(
            new DCMLModuleDependency
            {
                Id =
                    dependency.Id,
                Optional =
                    true
            });

        DCMLPackageCompatibilityResult result =
            DCMLPackageCompatibilityEvaluator.Evaluate(
                new[]
                {
                    CreatePackage(
                        dependency),
                    CreatePackage(
                        consumer)
                },
                "0.0.3",
                CreateCapabilities());

        Assert.Single(
            result.CompatiblePackages);

        Assert.Equal(
            consumer.Id,
            result.CompatiblePackages[0].Manifest.Id);
    }

    [Fact]
    public void RealUnsupportedCapabilityProbe_IsExcludedBeforeActivation()
    {
        string solutionRoot =
            GetSolutionRoot();

        string sourceRoot =
            Path.Combine(
                solutionRoot,
                "src",
                "DCML.CompatibilityProbe.UnsupportedCapability");

        string assemblySource =
            Path.Combine(
                sourceRoot,
                "bin",
                "Release",
                "net6.0",
                "DCML.CompatibilityProbe.UnsupportedCapability.dll");

        string manifestSource =
            Path.Combine(
                sourceRoot,
                "manifest.json");

        Assert.True(
            File.Exists(
                assemblySource));

        string root =
            Path.Combine(
                Path.GetTempPath(),
                "DCML-CompatibilityProbe",
                Guid.NewGuid().ToString("N"));

        string modulesRoot =
            Path.Combine(
                root,
                "Modules");

        string dataRoot =
            Path.Combine(
                root,
                "Data");

        string packageRoot =
            Path.Combine(
                modulesRoot,
                "Unsupported");

        Directory.CreateDirectory(
            packageRoot);

        Directory.CreateDirectory(
            dataRoot);

        File.Copy(
            assemblySource,
            Path.Combine(
                packageRoot,
                "DCML.CompatibilityProbe.UnsupportedCapability.dll"));

        File.Copy(
            manifestSource,
            Path.Combine(
                packageRoot,
                "manifest.json"));

        try
        {
            DCMLPackageDiscoveryResult discovery =
                DCMLPackageDiscovery.Discover(
                    modulesRoot);

            Assert.Empty(
                discovery.Failures);

            Assert.Single(
                discovery.Packages);

            DCMLPackageCompatibilityResult compatibility =
                DCMLPackageCompatibilityEvaluator.Evaluate(
                    discovery.Packages,
                    "0.0.3",
                    CreateCapabilities());

            Assert.Empty(
                compatibility.CompatiblePackages);

            Assert.Contains(
                compatibility.Issues,
                issue =>
                    issue.Code ==
                    "DCML_COMPATIBILITY_CAPABILITY_MISSING");

            DCMLDependencyResolutionResult resolution =
                DCMLDependencyResolver.Resolve(
                    compatibility.CompatiblePackages);

            var runtime =
                new DCMLModuleRuntime(
                    new DCMLReflectionModuleActivator(),
                    new ProbeContextFactory(
                        dataRoot));

            DCMLModuleRuntimeResult runtimeResult =
                runtime.Start(
                    resolution.LoadOrder);

            Assert.True(
                runtimeResult.Success);

            Assert.Empty(
                runtimeResult.Modules);

            Assert.False(
                File.Exists(
                    Path.Combine(
                        dataRoot,
                        "dcml.probe.compatibility-unsupported",
                        "ACTIVATED-UNEXPECTEDLY.txt")));
        }
        finally
        {
            try
            {
                Directory.Delete(
                    root,
                    true);
            }
            catch
            {
            }
        }
    }

    private static DCMLPackageCompatibilityResult Evaluate(
        DCMLModuleManifest manifest)
    {
        return
            DCMLPackageCompatibilityEvaluator.Evaluate(
                new[]
                {
                    CreatePackage(
                        manifest)
                },
                "0.0.3",
                CreateCapabilities());
    }

    private static IReadOnlyCollection<DCMLCapabilityDescriptor> CreateCapabilities()
    {
        return
            new[]
            {
                new DCMLCapabilityDescriptor(
                    DCMLRuntimeCapabilities.Events,
                    "1.0.0"),
                new DCMLCapabilityDescriptor(
                    DCMLRuntimeCapabilities.Logging,
                    "1.0.0")
            };
    }

    private static DCMLModuleManifest CreateManifest(
        string id)
    {
        return
            new DCMLModuleManifest
            {
                Id =
                    id,
                Name =
                    id,
                Version =
                    "1.0.0",
                EntryAssembly =
                    id + ".dll",
                EntryType =
                    id + ".Module",
                Dependencies =
                    new List<DCMLModuleDependency>(),
                RequiredCapabilities =
                    new List<DCMLCapabilityRequirement>()
            };
    }

    private static DCMLModulePackage CreatePackage(
        DCMLModuleManifest manifest)
    {
        return
            new DCMLModulePackage(
                @"C:\DCML\Tests\" +
                manifest.Id,
                @"C:\DCML\Tests\" +
                manifest.Id +
                @"\manifest.json",
                manifest);
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

    private sealed class ProbeContextFactory :
        IDCMLModuleContextFactory
    {
        private readonly string _dataRoot;

        public ProbeContextFactory(
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
                new ProbeContext(
                    package.PackageDirectory,
                    dataDirectory);
        }
    }

    private sealed class ProbeContext :
        IDCMLModuleContext
    {
        public ProbeContext(
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
