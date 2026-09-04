using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using DCML.Core.Runtime;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLDiagnosticsBuilderTests
{
    [Fact]
    public void Build_MapsDiscoveryAndValidationFailures()
    {
        string root =
            Path.Combine(
                Path.GetTempPath(),
                "DCML-Diagnostics-" +
                Guid.NewGuid().ToString("N")
            );

        string packageDirectory =
            Path.Combine(
                root,
                "Invalid"
            );

        Directory.CreateDirectory(
            packageDirectory
        );

        try
        {
            string manifestPath =
                Path.Combine(
                    packageDirectory,
                    "manifest.json"
                );

            File.WriteAllText(
                manifestPath,
                """
                {
                  "schemaVersion": 1,
                  "id": "",
                  "name": "Invalid",
                  "version": "1.0.0",
                  "entryAssembly": "Invalid.dll",
                  "entryType": "Invalid.Module",
                  "dependencies": []
                }
                """
            );

            DCMLPackageDiscoveryResult discovery =
                DCMLPackageDiscovery.Discover(
                    root
                );

            DCMLDiagnosticsSnapshot snapshot =
                DCMLDiagnosticsBuilder.Build(
                    discovery:
                        discovery
                );

            Assert.Contains(
                snapshot.Diagnostics,
                diagnostic =>
                    diagnostic.Stage ==
                        DCMLDiagnosticStage.Discovery &&
                    diagnostic.PackageDirectory ==
                        packageDirectory
            );

            Assert.Contains(
                snapshot.Diagnostics,
                diagnostic =>
                    diagnostic.Stage ==
                        DCMLDiagnosticStage.Validation &&
                    diagnostic.ManifestPath ==
                        manifestPath
            );

            Assert.True(
                snapshot.HasErrors
            );
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(
                    root,
                    true
                );
            }
        }
    }

    [Fact]
    public void Build_MapsCompatibilityFailureAndStatus()
    {
        DCMLModulePackage package =
            CreatePackage(
                "dcml.test.compatibility"
            );

        package.Manifest.MinimumDCMLVersion =
            "9.0.0";

        DCMLPackageCompatibilityResult compatibility =
            DCMLPackageCompatibilityEvaluator.Evaluate(
                new[]
                {
                    package
                },
                "1.0.0",
                Array.Empty<DCMLCapabilityDescriptor>()
            );

        DCMLDiagnosticsSnapshot snapshot =
            DCMLDiagnosticsBuilder.Build(
                compatibility:
                    compatibility
            );

        DCMLModuleStatus status =
            Assert.Single(
                snapshot.Modules
            );

        Assert.Equal(
            DCMLModuleStatusState.Incompatible,
            status.State
        );

        Assert.Contains(
            snapshot.Diagnostics,
            diagnostic =>
                diagnostic.Stage ==
                    DCMLDiagnosticStage.Compatibility &&
                diagnostic.ModuleId ==
                    package.Manifest.Id
        );
    }

    [Fact]
    public void Build_MapsDependencyFailureAndBlockedStatus()
    {
        DCMLModulePackage package =
            CreatePackage(
                "dcml.test.consumer"
            );

        package.Manifest.Dependencies.Add(
            new DCMLModuleDependency
            {
                Id =
                    "dcml.test.missing",
                Optional =
                    false
            }
        );

        DCMLPackageCompatibilityResult compatibility =
            DCMLPackageCompatibilityEvaluator.Evaluate(
                new[]
                {
                    package
                },
                "1.0.0",
                Array.Empty<DCMLCapabilityDescriptor>()
            );

        DCMLDependencyResolutionResult resolution =
            DCMLDependencyResolver.Resolve(
                compatibility.CompatiblePackages
            );

        DCMLDiagnosticsSnapshot snapshot =
            DCMLDiagnosticsBuilder.Build(
                compatibility:
                    compatibility,
                dependencyResolution:
                    resolution
            );

        DCMLModuleStatus status =
            Assert.Single(
                snapshot.Modules
            );

        Assert.Equal(
            DCMLModuleStatusState.Blocked,
            status.State
        );

        Assert.Contains(
            snapshot.Diagnostics,
            diagnostic =>
                diagnostic.Stage ==
                    DCMLDiagnosticStage.DependencyResolution &&
                diagnostic.ModuleId ==
                    package.Manifest.Id &&
                diagnostic.DependencyId ==
                    "dcml.test.missing"
        );
    }

    [Fact]
    public void Build_MapsRuntimeActivationFailure()
    {
        DCMLModulePackage package =
            CreatePackage(
                "dcml.test.runtime"
            );

        var runtime =
            new DCMLModuleRuntimeResult(
                new[]
                {
                    new DCMLModuleRuntimeEntry(
                        package,
                        DCMLModuleRuntimeState.Failed
                    )
                },
                new[]
                {
                    new DCMLModuleRuntimeIssue(
                        package.Manifest.Id,
                        "DCML_RUNTIME_ACTIVATION_FAILED",
                        "The module could not be activated.",
                        exceptionType:
                            "System.InvalidOperationException"
                    )
                }
            );

        DCMLDiagnosticsSnapshot snapshot =
            DCMLDiagnosticsBuilder.Build(
                runtime:
                    runtime
            );

        DCMLModuleStatus status =
            Assert.Single(
                snapshot.Modules
            );

        Assert.Equal(
            DCMLModuleStatusState.Failed,
            status.State
        );

        DCMLDiagnosticIssue diagnostic =
            Assert.Single(
                snapshot.Diagnostics
            );

        Assert.Equal(
            DCMLDiagnosticStage.Activation,
            diagnostic.Stage
        );

        Assert.Equal(
            "System.InvalidOperationException",
            diagnostic.ExceptionType
        );
    }

    [Fact]
    public void Formatter_ProducesDeveloperFacingContext()
    {
        var issue =
            new DCMLDiagnosticIssue(
                DCMLDiagnosticStage.DependencyResolution,
                DCMLDiagnosticSeverity.Error,
                "DCML_DEPENDENCY_REQUIRED_MISSING",
                "Required dependency is unavailable.",
                moduleId:
                    "dcml.test.consumer",
                dependencyId:
                    "dcml.test.provider"
            );

        string formatted =
            DCMLDiagnosticFormatter.Format(
                issue
            );

        Assert.Contains(
            "[Error]",
            formatted
        );

        Assert.Contains(
            "[DependencyResolution]",
            formatted
        );

        Assert.Contains(
            "DCML_DEPENDENCY_REQUIRED_MISSING",
            formatted
        );

        Assert.Contains(
            "module=dcml.test.consumer",
            formatted
        );

        Assert.Contains(
            "dependency=dcml.test.provider",
            formatted
        );
    }

    private static DCMLModulePackage CreatePackage(
        string moduleId
    )
    {
        var manifest =
            new DCMLModuleManifest
            {
                Id =
                    moduleId,
                Name =
                    moduleId,
                Version =
                    "1.0.0",
                EntryAssembly =
                    "Module.dll",
                EntryType =
                    "Module.Entry"
            };

        return new DCMLModulePackage(
            Path.Combine(
                "packages",
                moduleId
            ),
            Path.Combine(
                "packages",
                moduleId,
                "manifest.json"
            ),
            manifest
        );
    }
}
