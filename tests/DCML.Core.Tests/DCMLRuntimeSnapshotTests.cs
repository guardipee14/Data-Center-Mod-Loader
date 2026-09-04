using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using DCML.Core.Runtime;
using Xunit;

namespace DCML.Core.Tests;

public sealed class DCMLRuntimeSnapshotTests
{
    [Fact]
    public void GetSnapshot_BeforeStart_IsEmpty()
    {
        var runtime =
            CreateRuntime();

        DCMLModuleRuntimeResult snapshot =
            runtime.GetSnapshot();

        Assert.Empty(
            snapshot.Modules
        );

        Assert.Empty(
            snapshot.Issues
        );

        Assert.True(
            snapshot.Success
        );
    }

    [Fact]
    public void GetSnapshot_AfterStart_ReportsCurrentInventory()
    {
        var runtime =
            CreateRuntime();

        runtime.Start(
            new[]
            {
                CreatePackage(
                    "dcml.test.zeta"
                ),
                CreatePackage(
                    "dcml.test.alpha"
                )
            }
        );

        DCMLModuleRuntimeResult snapshot =
            runtime.GetSnapshot();

        Assert.Equal(
            new[]
            {
                "dcml.test.alpha",
                "dcml.test.zeta"
            },
            snapshot.Modules
                .Select(
                    module =>
                        module.ModuleId
                )
                .ToArray()
        );

        Assert.All(
            snapshot.Modules,
            module =>
                Assert.Equal(
                    DCMLModuleRuntimeState.Running,
                    module.State
                )
        );
    }

    [Fact]
    public void GetSnapshot_PreservesRuntimeIssueHistory()
    {
        var activator =
            new SnapshotActivator();

        activator.ThrowOnCreate.Add(
            "dcml.test.failed"
        );

        var runtime =
            new DCMLModuleRuntime(
                activator,
                new SnapshotContextFactory()
            );

        DCMLModuleRuntimeResult startResult =
            runtime.Start(
                new[]
                {
                    CreatePackage(
                        "dcml.test.failed"
                    ),
                    CreatePackage(
                        "dcml.test.running"
                    )
                }
            );

        Assert.False(
            startResult.Success
        );

        DCMLModuleRuntimeResult snapshot =
            runtime.GetSnapshot();

        Assert.Contains(
            snapshot.Issues,
            issue =>
                issue.ModuleId ==
                    "dcml.test.failed" &&
                issue.Code ==
                    "DCML_RUNTIME_ACTIVATION_FAILED"
        );

        Assert.Equal(
            DCMLModuleRuntimeState.Failed,
            GetState(
                snapshot,
                "dcml.test.failed"
            )
        );

        Assert.Equal(
            DCMLModuleRuntimeState.Running,
            GetState(
                snapshot,
                "dcml.test.running"
            )
        );
    }

    [Fact]
    public void GetSnapshot_AfterStop_ReportsStoppedState()
    {
        var runtime =
            CreateRuntime();

        runtime.Start(
            new[]
            {
                CreatePackage(
                    "dcml.test.module"
                )
            }
        );

        runtime.Stop();

        DCMLModuleRuntimeResult snapshot =
            runtime.GetSnapshot();

        Assert.False(
            runtime.IsRunning
        );

        Assert.Equal(
            DCMLModuleRuntimeState.Stopped,
            GetState(
                snapshot,
                "dcml.test.module"
            )
        );
    }

    private static DCMLModuleRuntime CreateRuntime()
    {
        return new DCMLModuleRuntime(
            new SnapshotActivator(),
            new SnapshotContextFactory()
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

    private static DCMLModuleRuntimeState GetState(
        DCMLModuleRuntimeResult result,
        string moduleId
    )
    {
        return result.Modules
            .Single(
                module =>
                    module.ModuleId ==
                    moduleId
            )
            .State;
    }

    private sealed class SnapshotActivator :
        IDCMLModuleActivator
    {
        public HashSet<string> ThrowOnCreate { get; } =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

        public IDCMLModule Create(
            DCMLModulePackage package
        )
        {
            if (
                ThrowOnCreate.Contains(
                    package.Manifest.Id
                )
            )
            {
                throw new InvalidOperationException(
                    "activation failure"
                );
            }

            return new SnapshotModule(
                package.Manifest.Id,
                package.Manifest.Version
            );
        }
    }

    private sealed class SnapshotContextFactory :
        IDCMLModuleContextFactory
    {
        public IDCMLModuleContext CreateContext(
            DCMLModulePackage package
        )
        {
            return new SnapshotContext();
        }
    }

    private sealed class SnapshotContext :
        IDCMLModuleContext
    {
        public string ModuleDirectory =>
            "module";

        public string DataDirectory =>
            "data";

        public IServiceProvider Services =>
            EmptyServiceProvider.Instance;
    }

    private sealed class EmptyServiceProvider :
        IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } =
            new EmptyServiceProvider();

        public object? GetService(
            Type serviceType
        )
        {
            return null;
        }
    }

    private sealed class SnapshotModule :
        IDCMLModule
    {
        public SnapshotModule(
            string id,
            string version
        )
        {
            Id =
                id;

            Version =
                version;
        }

        public string Id { get; }

        
        public string Name =>
            Id;

        public string Version { get; }

        public void Initialize(
            IDCMLModuleContext context
        )
        {
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }
    }
}
