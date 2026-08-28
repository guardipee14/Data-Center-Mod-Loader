using System.Linq;
using DCML.Core.Models;
using DCML.Core.Runtime;
using Xunit;

namespace DCML.Core.Tests
{
    public sealed class DCMLDependencyResolverTests
    {
        [Fact]
        public void Resolve_NoDependencies_UsesDeterministicIdOrder()
        {
            DCMLDependencyResolutionResult result =
                DCMLDependencyResolver.Resolve(
                    new[]
                    {
                        CreatePackage(
                            "dcml.example.zeta"
                        ),
                        CreatePackage(
                            "dcml.example.alpha"
                        )
                    }
                );

            Assert.True(result.Success);

            Assert.Equal(
                new[]
                {
                    "dcml.example.alpha",
                    "dcml.example.zeta"
                },
                result.LoadOrder
                    .Select(
                        package =>
                            package.Manifest.Id
                    )
                    .ToArray()
            );
        }

        [Fact]
        public void Resolve_RequiredDependency_LoadsDependencyFirst()
        {
            DCMLModulePackage application =
                CreatePackage(
                    "dcml.example.application"
                );

            application.Manifest.Dependencies.Add(
                new DCMLModuleDependency
                {
                    Id =
                        "dcml.example.common",
                    MinimumVersion =
                        "1.0.0"
                }
            );

            DCMLDependencyResolutionResult result =
                DCMLDependencyResolver.Resolve(
                    new[]
                    {
                        application,
                        CreatePackage(
                            "dcml.example.common",
                            "1.2.0"
                        )
                    }
                );

            Assert.True(result.Success);

            Assert.Equal(
                new[]
                {
                    "dcml.example.common",
                    "dcml.example.application"
                },
                result.LoadOrder
                    .Select(
                        package =>
                            package.Manifest.Id
                    )
                    .ToArray()
            );
        }

        [Fact]
        public void Resolve_MissingRequiredDependency_BlocksOnlyAffectedModule()
        {
            DCMLModulePackage blocked =
                CreatePackage(
                    "dcml.example.blocked"
                );

            blocked.Manifest.Dependencies.Add(
                new DCMLModuleDependency
                {
                    Id =
                        "dcml.example.missing"
                }
            );

            DCMLDependencyResolutionResult result =
                DCMLDependencyResolver.Resolve(
                    new[]
                    {
                        blocked,
                        CreatePackage(
                            "dcml.example.independent"
                        )
                    }
                );

            Assert.False(result.Success);
            Assert.Single(result.LoadOrder);

            Assert.Equal(
                "dcml.example.independent",
                result.LoadOrder[0].Manifest.Id
            );

            Assert.Contains(
                result.Issues,
                issue =>
                    issue.ModuleId ==
                        "dcml.example.blocked" &&
                    issue.Code ==
                        "DCML_DEPENDENCY_MISSING"
            );
        }

        [Fact]
        public void Resolve_MissingOptionalDependency_DoesNotBlockModule()
        {
            DCMLModulePackage package =
                CreatePackage(
                    "dcml.example.optional"
                );

            package.Manifest.Dependencies.Add(
                new DCMLModuleDependency
                {
                    Id =
                        "dcml.example.not-installed",
                    Optional =
                        true
                }
            );

            DCMLDependencyResolutionResult result =
                DCMLDependencyResolver.Resolve(
                    new[]
                    {
                        package
                    }
                );

            Assert.True(result.Success);
            Assert.Single(result.LoadOrder);
        }

        [Fact]
        public void Resolve_UnsatisfiedMinimumVersion_BlocksModule()
        {
            DCMLModulePackage application =
                CreatePackage(
                    "dcml.example.application"
                );

            application.Manifest.Dependencies.Add(
                new DCMLModuleDependency
                {
                    Id =
                        "dcml.example.common",
                    MinimumVersion =
                        "2.0.0"
                }
            );

            DCMLDependencyResolutionResult result =
                DCMLDependencyResolver.Resolve(
                    new[]
                    {
                        application,
                        CreatePackage(
                            "dcml.example.common",
                            "1.9.9"
                        )
                    }
                );

            Assert.False(result.Success);

            Assert.DoesNotContain(
                result.LoadOrder,
                package =>
                    package.Manifest.Id ==
                    "dcml.example.application"
            );

            Assert.Contains(
                result.Issues,
                issue =>
                    issue.ModuleId ==
                        "dcml.example.application" &&
                    issue.Code ==
                        "DCML_DEPENDENCY_VERSION_UNSATISFIED"
            );
        }

        [Fact]
        public void Resolve_RequiredDependencyCycle_BlocksCycleMembers()
        {
            DCMLModulePackage first =
                CreatePackage(
                    "dcml.example.first"
                );

            DCMLModulePackage second =
                CreatePackage(
                    "dcml.example.second"
                );

            first.Manifest.Dependencies.Add(
                new DCMLModuleDependency
                {
                    Id =
                        second.Manifest.Id
                }
            );

            second.Manifest.Dependencies.Add(
                new DCMLModuleDependency
                {
                    Id =
                        first.Manifest.Id
                }
            );

            DCMLDependencyResolutionResult result =
                DCMLDependencyResolver.Resolve(
                    new[]
                    {
                        first,
                        second
                    }
                );

            Assert.False(result.Success);
            Assert.Empty(result.LoadOrder);

            Assert.Equal(
                2,
                result.Issues.Count(
                    issue =>
                        issue.Code ==
                        "DCML_DEPENDENCY_CYCLE"
                )
            );
        }

        [Fact]
        public void Resolve_DependentOnCycle_IsBlockedTransitively()
        {
            DCMLModulePackage first =
                CreatePackage(
                    "dcml.example.first"
                );

            DCMLModulePackage second =
                CreatePackage(
                    "dcml.example.second"
                );

            DCMLModulePackage consumer =
                CreatePackage(
                    "dcml.example.consumer"
                );

            first.Manifest.Dependencies.Add(
                new DCMLModuleDependency
                {
                    Id =
                        second.Manifest.Id
                }
            );

            second.Manifest.Dependencies.Add(
                new DCMLModuleDependency
                {
                    Id =
                        first.Manifest.Id
                }
            );

            consumer.Manifest.Dependencies.Add(
                new DCMLModuleDependency
                {
                    Id =
                        first.Manifest.Id
                }
            );

            DCMLDependencyResolutionResult result =
                DCMLDependencyResolver.Resolve(
                    new[]
                    {
                        consumer,
                        first,
                        second
                    }
                );

            Assert.Empty(result.LoadOrder);

            Assert.Contains(
                result.Issues,
                issue =>
                    issue.ModuleId ==
                        "dcml.example.consumer" &&
                    issue.Code ==
                        "DCML_DEPENDENCY_BLOCKED"
            );
        }

        [Fact]
        public void Resolve_PrereleaseDoesNotSatisfyStableMinimum()
        {
            DCMLModulePackage application =
                CreatePackage(
                    "dcml.example.application"
                );

            application.Manifest.Dependencies.Add(
                new DCMLModuleDependency
                {
                    Id =
                        "dcml.example.common",
                    MinimumVersion =
                        "1.0.0"
                }
            );

            DCMLDependencyResolutionResult result =
                DCMLDependencyResolver.Resolve(
                    new[]
                    {
                        application,
                        CreatePackage(
                            "dcml.example.common",
                            "1.0.0-rc.1"
                        )
                    }
                );

            Assert.Contains(
                result.Issues,
                issue =>
                    issue.Code ==
                    "DCML_DEPENDENCY_VERSION_UNSATISFIED"
            );
        }

        private static DCMLModulePackage CreatePackage(
            string id,
            string version = "1.0.0"
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
                        version,
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
    }
}
