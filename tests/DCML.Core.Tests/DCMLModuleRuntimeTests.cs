using System;
using System.Collections.Generic;
using System.Linq;
using DCML.Core.Abstractions;
using DCML.Core.Models;
using DCML.Core.Runtime;
using Xunit;

namespace DCML.Core.Tests
{
    public sealed class DCMLModuleRuntimeTests
    {
        [Fact]
        public void Start_InitializesAndStartsModule()
        {
            var events =
                new List<string>();

            DCMLModulePackage package =
                CreatePackage(
                    "dcml.example.alpha"
                );

            var runtime =
                CreateRuntime(
                    events
                );

            DCMLModuleRuntimeResult result =
                runtime.Start(
                    new[]
                    {
                        package
                    }
                );

            Assert.True(result.Success);
            Assert.True(runtime.IsRunning);

            Assert.Equal(
                new[]
                {
                    "create:dcml.example.alpha",
                    "context:dcml.example.alpha",
                    "initialize:dcml.example.alpha",
                    "start:dcml.example.alpha"
                },
                events
            );

            Assert.Equal(
                DCMLModuleRuntimeState.Running,
                GetState(
                    result,
                    package.Manifest.Id
                )
            );
        }

        [Fact]
        public void Start_UsesDependencySafeOrderProvidedByResolver()
        {
            var events =
                new List<string>();

            DCMLModulePackage common =
                CreatePackage(
                    "dcml.example.common"
                );

            DCMLModulePackage application =
                CreatePackage(
                    "dcml.example.application"
                );

            application.Manifest.Dependencies.Add(
                new DCMLModuleDependency
                {
                    Id =
                        common.Manifest.Id
                }
            );

            var runtime =
                CreateRuntime(
                    events
                );

            DCMLModuleRuntimeResult result =
                runtime.Start(
                    new[]
                    {
                        common,
                        application
                    }
                );

            Assert.True(result.Success);

            Assert.True(
                events.IndexOf(
                    "start:dcml.example.common"
                ) <
                events.IndexOf(
                    "initialize:dcml.example.application"
                )
            );
        }

        [Fact]
        public void Start_ActivationFailureDoesNotBlockIndependentModule()
        {
            var events =
                new List<string>();

            var activator =
                new FakeActivator(
                    events
                );

            activator.ThrowOnCreate.Add(
                "dcml.example.bad"
            );

            var runtime =
                new DCMLModuleRuntime(
                    activator,
                    new FakeContextFactory(
                        events
                    )
                );

            DCMLModuleRuntimeResult result =
                runtime.Start(
                    new[]
                    {
                        CreatePackage(
                            "dcml.example.bad"
                        ),
                        CreatePackage(
                            "dcml.example.good"
                        )
                    }
                );

            Assert.False(result.Success);

            Assert.Equal(
                DCMLModuleRuntimeState.Failed,
                GetState(
                    result,
                    "dcml.example.bad"
                )
            );

            Assert.Equal(
                DCMLModuleRuntimeState.Running,
                GetState(
                    result,
                    "dcml.example.good"
                )
            );
        }

        [Fact]
        public void Start_FailedDependencyBlocksDependent()
        {
            var events =
                new List<string>();

            var activator =
                new FakeActivator(
                    events
                );

            activator.ThrowOnCreate.Add(
                "dcml.example.common"
            );

            DCMLModulePackage common =
                CreatePackage(
                    "dcml.example.common"
                );

            DCMLModulePackage application =
                CreatePackage(
                    "dcml.example.application"
                );

            application.Manifest.Dependencies.Add(
                new DCMLModuleDependency
                {
                    Id =
                        common.Manifest.Id
                }
            );

            var runtime =
                new DCMLModuleRuntime(
                    activator,
                    new FakeContextFactory(
                        events
                    )
                );

            DCMLModuleRuntimeResult result =
                runtime.Start(
                    new[]
                    {
                        common,
                        application
                    }
                );

            Assert.Equal(
                DCMLModuleRuntimeState.Failed,
                GetState(
                    result,
                    common.Manifest.Id
                )
            );

            Assert.Equal(
                DCMLModuleRuntimeState.Blocked,
                GetState(
                    result,
                    application.Manifest.Id
                )
            );

            Assert.Contains(
                result.Issues,
                issue =>
                    issue.ModuleId ==
                        application.Manifest.Id &&
                    issue.Code ==
                        "DCML_RUNTIME_DEPENDENCY_NOT_RUNNING"
            );
        }

        [Fact]
        public void Start_OptionalFailedDependencyDoesNotBlockModule()
        {
            var events =
                new List<string>();

            var activator =
                new FakeActivator(
                    events
                );

            activator.ThrowOnCreate.Add(
                "dcml.example.optional"
            );

            DCMLModulePackage optional =
                CreatePackage(
                    "dcml.example.optional"
                );

            DCMLModulePackage consumer =
                CreatePackage(
                    "dcml.example.consumer"
                );

            consumer.Manifest.Dependencies.Add(
                new DCMLModuleDependency
                {
                    Id =
                        optional.Manifest.Id,
                    Optional =
                        true
                }
            );

            var runtime =
                new DCMLModuleRuntime(
                    activator,
                    new FakeContextFactory(
                        events
                    )
                );

            DCMLModuleRuntimeResult result =
                runtime.Start(
                    new[]
                    {
                        optional,
                        consumer
                    }
                );

            Assert.Equal(
                DCMLModuleRuntimeState.Failed,
                GetState(
                    result,
                    optional.Manifest.Id
                )
            );

            Assert.Equal(
                DCMLModuleRuntimeState.Running,
                GetState(
                    result,
                    consumer.Manifest.Id
                )
            );
        }

        [Fact]
        public void Start_InitializeFailureBlocksDependent()
        {
            var events =
                new List<string>();

            var activator =
                new FakeActivator(
                    events
                );

            activator.ThrowOnInitialize.Add(
                "dcml.example.common"
            );

            DCMLModulePackage common =
                CreatePackage(
                    "dcml.example.common"
                );

            DCMLModulePackage application =
                CreatePackage(
                    "dcml.example.application"
                );

            application.Manifest.Dependencies.Add(
                new DCMLModuleDependency
                {
                    Id =
                        common.Manifest.Id
                }
            );

            var runtime =
                new DCMLModuleRuntime(
                    activator,
                    new FakeContextFactory(
                        events
                    )
                );

            DCMLModuleRuntimeResult result =
                runtime.Start(
                    new[]
                    {
                        common,
                        application
                    }
                );

            Assert.Contains(
                result.Issues,
                issue =>
                    issue.ModuleId ==
                        common.Manifest.Id &&
                    issue.Code ==
                        "DCML_RUNTIME_INITIALIZE_FAILED"
            );

            Assert.Equal(
                DCMLModuleRuntimeState.Blocked,
                GetState(
                    result,
                    application.Manifest.Id
                )
            );
        }

        [Fact]
        public void Start_StartFailureCleansUpAndBlocksDependent()
        {
            var events =
                new List<string>();

            var activator =
                new FakeActivator(
                    events
                );

            activator.ThrowOnStart.Add(
                "dcml.example.common"
            );

            DCMLModulePackage common =
                CreatePackage(
                    "dcml.example.common"
                );

            DCMLModulePackage application =
                CreatePackage(
                    "dcml.example.application"
                );

            application.Manifest.Dependencies.Add(
                new DCMLModuleDependency
                {
                    Id =
                        common.Manifest.Id
                }
            );

            var runtime =
                new DCMLModuleRuntime(
                    activator,
                    new FakeContextFactory(
                        events
                    )
                );

            DCMLModuleRuntimeResult result =
                runtime.Start(
                    new[]
                    {
                        common,
                        application
                    }
                );

            Assert.Contains(
                "stop:dcml.example.common",
                events
            );

            Assert.Equal(
                DCMLModuleRuntimeState.Failed,
                GetState(
                    result,
                    common.Manifest.Id
                )
            );

            Assert.Equal(
                DCMLModuleRuntimeState.Blocked,
                GetState(
                    result,
                    application.Manifest.Id
                )
            );
        }

        [Fact]
        public void Start_RejectsModuleIdMismatch()
        {
            var events =
                new List<string>();

            var activator =
                new FakeActivator(
                    events
                );

            activator.ReportedIds[
                "dcml.example.manifest"
            ] =
                "dcml.example.assembly";

            var runtime =
                new DCMLModuleRuntime(
                    activator,
                    new FakeContextFactory(
                        events
                    )
                );

            DCMLModuleRuntimeResult result =
                runtime.Start(
                    new[]
                    {
                        CreatePackage(
                            "dcml.example.manifest"
                        )
                    }
                );

            Assert.False(result.Success);

            Assert.Contains(
                result.Issues,
                issue =>
                    issue.Code ==
                    "DCML_RUNTIME_MODULE_ID_MISMATCH"
            );
        }

        [Fact]
        public void Stop_StopsRunningModulesInReverseStartOrder()
        {
            var events =
                new List<string>();

            var runtime =
                CreateRuntime(
                    events
                );

            DCMLModulePackage first =
                CreatePackage(
                    "dcml.example.first"
                );

            DCMLModulePackage second =
                CreatePackage(
                    "dcml.example.second"
                );

            DCMLModuleRuntimeResult startResult =
                runtime.Start(
                    new[]
                    {
                        first,
                        second
                    }
                );

            Assert.True(startResult.Success);

            events.Clear();

            DCMLModuleRuntimeResult stopResult =
                runtime.Stop();

            Assert.True(stopResult.Success);

            Assert.Equal(
                new[]
                {
                    "stop:dcml.example.second",
                    "stop:dcml.example.first"
                },
                events
            );

            Assert.False(runtime.IsRunning);
        }

        [Fact]
        public void Stop_ContinuesWhenModuleStopThrows()
        {
            var events =
                new List<string>();

            var activator =
                new FakeActivator(
                    events
                );

            activator.ThrowOnStop.Add(
                "dcml.example.second"
            );

            var runtime =
                new DCMLModuleRuntime(
                    activator,
                    new FakeContextFactory(
                        events
                    )
                );

            runtime.Start(
                new[]
                {
                    CreatePackage(
                        "dcml.example.first"
                    ),
                    CreatePackage(
                        "dcml.example.second"
                    )
                }
            );

            events.Clear();

            DCMLModuleRuntimeResult result =
                runtime.Stop();

            Assert.False(result.Success);

            Assert.Contains(
                "stop:dcml.example.second",
                events
            );

            Assert.Contains(
                "stop:dcml.example.first",
                events
            );

            Assert.Contains(
                result.Issues,
                issue =>
                    issue.ModuleId ==
                        "dcml.example.second" &&
                    issue.Code ==
                        "DCML_RUNTIME_STOP_FAILED"
            );
        }

        private static DCMLModuleRuntime CreateRuntime(
            List<string> events
        )
        {
            return new DCMLModuleRuntime(
                new FakeActivator(
                    events
                ),
                new FakeContextFactory(
                    events
                )
            );
        }

        private static DCMLModulePackage CreatePackage(
            string id
        )
        {
            var manifest =
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
                        id + ".Module"
                };

            return new DCMLModulePackage(
                @"C:\DCML\Tests\" + id,
                @"C:\DCML\Tests\" + id + @"\manifest.json",
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

        private sealed class FakeActivator :
            IDCMLModuleActivator
        {
            private readonly List<string> _events;

            public FakeActivator(
                List<string> events
            )
            {
                _events = events;
            }

            public HashSet<string> ThrowOnCreate { get; } =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            public HashSet<string> ThrowOnInitialize { get; } =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            public HashSet<string> ThrowOnStart { get; } =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            public HashSet<string> ThrowOnStop { get; } =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            public Dictionary<string, string> ReportedIds { get; } =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase
                );

            public IDCMLModule Create(
                DCMLModulePackage package
            )
            {
                string id =
                    package.Manifest.Id;

                _events.Add(
                    "create:" + id
                );

                if (
                    ThrowOnCreate.Contains(
                        id
                    )
                )
                {
                    throw new InvalidOperationException(
                        "create failure"
                    );
                }

                string reportedId =
                    ReportedIds.TryGetValue(
                        id,
                        out string? mappedId
                    )
                        ? mappedId
                        : id;

                return new FakeModule(
                    id,
                    reportedId,
                    package.Manifest.Version,
                    _events,
                    ThrowOnInitialize.Contains(
                        id
                    ),
                    ThrowOnStart.Contains(
                        id
                    ),
                    ThrowOnStop.Contains(
                        id
                    )
                );
            }
        }

        private sealed class FakeContextFactory :
            IDCMLModuleContextFactory
        {
            private readonly List<string> _events;

            public FakeContextFactory(
                List<string> events
            )
            {
                _events = events;
            }

            public IDCMLModuleContext CreateContext(
                DCMLModulePackage package
            )
            {
                _events.Add(
                    "context:" +
                    package.Manifest.Id
                );

                return new FakeContext();
            }
        }

        private sealed class FakeContext :
            IDCMLModuleContext
        {
            public string ModuleDirectory =>
                @"C:\DCML\Tests";

            public string DataDirectory =>
                @"C:\DCML\Tests\Data";

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

        private sealed class FakeModule :
            IDCMLModule
        {
            private readonly string _eventId;
            private readonly List<string> _events;
            private readonly bool _throwOnInitialize;
            private readonly bool _throwOnStart;
            private readonly bool _throwOnStop;

            public FakeModule(
                string eventId,
                string reportedId,
                string version,
                List<string> events,
                bool throwOnInitialize,
                bool throwOnStart,
                bool throwOnStop
            )
            {
                _eventId = eventId;
                Id = reportedId;
                Name = eventId;
                Version = version;
                _events = events;
                _throwOnInitialize = throwOnInitialize;
                _throwOnStart = throwOnStart;
                _throwOnStop = throwOnStop;
            }

            public string Id { get; }

            public string Name { get; }

            public string Version { get; }

            public void Initialize(
                IDCMLModuleContext context
            )
            {
                _events.Add(
                    "initialize:" + _eventId
                );

                if (_throwOnInitialize)
                {
                    throw new InvalidOperationException(
                        "initialize failure"
                    );
                }
            }

            public void Start()
            {
                _events.Add(
                    "start:" + _eventId
                );

                if (_throwOnStart)
                {
                    throw new InvalidOperationException(
                        "start failure"
                    );
                }
            }

            public void Stop()
            {
                _events.Add(
                    "stop:" + _eventId
                );

                if (_throwOnStop)
                {
                    throw new InvalidOperationException(
                        "stop failure"
                    );
                }
            }
        }
    }
}
