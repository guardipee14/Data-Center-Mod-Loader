using DCML.Core.Models;
using DCML.Core.Runtime;
using Xunit;

namespace DCML.Core.Tests
{
    public sealed class DCMLModuleManifestVersionTests
    {
        [Fact]
        public void Validate_RejectsInvalidModuleVersion()
        {
            DCMLModuleManifest manifest =
                CreateValidManifest();

            manifest.Version =
                "banana";

            DCMLValidationResult result =
                DCMLModuleManifestValidator.Validate(
                    manifest
                );

            Assert.Contains(
                result.Issues,
                issue =>
                    issue.Code ==
                    "DCML_MANIFEST_VERSION_INVALID"
            );
        }

        [Fact]
        public void Validate_RejectsInvalidMinimumDCMLVersion()
        {
            DCMLModuleManifest manifest =
                CreateValidManifest();

            manifest.MinimumDCMLVersion =
                "next";

            DCMLValidationResult result =
                DCMLModuleManifestValidator.Validate(
                    manifest
                );

            Assert.Contains(
                result.Issues,
                issue =>
                    issue.Code ==
                    "DCML_MANIFEST_MINIMUM_DCML_VERSION_INVALID"
            );
        }

        [Fact]
        public void Validate_RejectsInvalidDependencyMinimumVersion()
        {
            DCMLModuleManifest manifest =
                CreateValidManifest();

            manifest.Dependencies.Add(
                new DCMLModuleDependency
                {
                    Id =
                        "dcml.example.common",
                    MinimumVersion =
                        "latest"
                }
            );

            DCMLValidationResult result =
                DCMLModuleManifestValidator.Validate(
                    manifest
                );

            Assert.Contains(
                result.Issues,
                issue =>
                    issue.Code ==
                    "DCML_MANIFEST_DEPENDENCY_VERSION_INVALID"
            );
        }

        private static DCMLModuleManifest
            CreateValidManifest()
        {
            return new DCMLModuleManifest
            {
                Id =
                    "dcml.example.networkdoctor",
                Name =
                    "Network Doctor",
                Version =
                    "1.0.0",
                EntryAssembly =
                    "NetworkDoctor.dll",
                EntryType =
                    "NetworkDoctor.Module",
                MinimumDCMLVersion =
                    "0.1.0"
            };
        }
    }
}
