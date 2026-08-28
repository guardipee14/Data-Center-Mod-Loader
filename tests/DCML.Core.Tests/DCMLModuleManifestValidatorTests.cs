using DCML.Core.Models;
using DCML.Core.Runtime;
using Xunit;

namespace DCML.Core.Tests
{
    public sealed class DCMLModuleManifestValidatorTests
    {
        [Fact]
        public void Validate_AcceptsValidManifest()
        {
            DCMLModuleManifest manifest =
                CreateValidManifest();

            DCMLValidationResult result =
                DCMLModuleManifestValidator.Validate(
                    manifest
                );

            Assert.True(result.IsValid);
            Assert.Empty(result.Issues);
        }

        [Fact]
        public void Validate_RejectsMissingRequiredFields()
        {
            var manifest =
                new DCMLModuleManifest();

            DCMLValidationResult result =
                DCMLModuleManifestValidator.Validate(
                    manifest
                );

            Assert.False(result.IsValid);

            Assert.Contains(
                result.Issues,
                issue =>
                    issue.Code ==
                    "DCML_MANIFEST_ID_REQUIRED"
            );

            Assert.Contains(
                result.Issues,
                issue =>
                    issue.Code ==
                    "DCML_MANIFEST_NAME_REQUIRED"
            );

            Assert.Contains(
                result.Issues,
                issue =>
                    issue.Code ==
                    "DCML_MANIFEST_VERSION_REQUIRED"
            );

            Assert.Contains(
                result.Issues,
                issue =>
                    issue.Code ==
                    "DCML_MANIFEST_ENTRY_ASSEMBLY_REQUIRED"
            );

            Assert.Contains(
                result.Issues,
                issue =>
                    issue.Code ==
                    "DCML_MANIFEST_ENTRY_TYPE_REQUIRED"
            );
        }

        [Fact]
        public void Validate_RejectsRootedEntryAssembly()
        {
            DCMLModuleManifest manifest =
                CreateValidManifest();

            manifest.EntryAssembly =
                @"C:\Bad\Module.dll";

            DCMLValidationResult result =
                DCMLModuleManifestValidator.Validate(
                    manifest
                );

            Assert.Contains(
                result.Issues,
                issue =>
                    issue.Code ==
                    "DCML_MANIFEST_ENTRY_ASSEMBLY_ROOTED"
            );
        }

        [Fact]
        public void Validate_RejectsEntryAssemblyTraversal()
        {
            DCMLModuleManifest manifest =
                CreateValidManifest();

            manifest.EntryAssembly =
                @"..\..\Bad.dll";

            DCMLValidationResult result =
                DCMLModuleManifestValidator.Validate(
                    manifest
                );

            Assert.Contains(
                result.Issues,
                issue =>
                    issue.Code ==
                    "DCML_MANIFEST_ENTRY_ASSEMBLY_TRAVERSAL"
            );
        }

        [Fact]
        public void Validate_RejectsSelfDependency()
        {
            DCMLModuleManifest manifest =
                CreateValidManifest();

            manifest.Dependencies.Add(
                new DCMLModuleDependency
                {
                    Id = manifest.Id
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
                    "DCML_MANIFEST_SELF_DEPENDENCY"
            );
        }

        [Fact]
        public void Validate_RejectsDuplicateDependencies()
        {
            DCMLModuleManifest manifest =
                CreateValidManifest();

            manifest.Dependencies.Add(
                new DCMLModuleDependency
                {
                    Id = "dcml.example.common"
                }
            );

            manifest.Dependencies.Add(
                new DCMLModuleDependency
                {
                    Id = "DCML.EXAMPLE.COMMON"
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
                    "DCML_MANIFEST_DUPLICATE_DEPENDENCY"
            );
        }

        [Fact]
        public void Validate_RejectsNonDllEntryAssembly()
        {
            DCMLModuleManifest manifest =
                CreateValidManifest();

            manifest.EntryAssembly =
                "NetworkDoctor.exe";

            DCMLValidationResult result =
                DCMLModuleManifestValidator.Validate(
                    manifest
                );

            Assert.Contains(
                result.Issues,
                issue =>
                    issue.Code ==
                    "DCML_MANIFEST_ENTRY_ASSEMBLY_EXTENSION"
            );
        }

        private static DCMLModuleManifest
            CreateValidManifest()
        {
            return new DCMLModuleManifest
            {
                Id = "dcml.example.networkdoctor",
                Name = "Network Doctor",
                Version = "1.0.0",
                Description =
                    "Example DCML module.",
                Author =
                    "DCML Test Suite",
                EntryAssembly =
                    "NetworkDoctor.dll",
                EntryType =
                    "NetworkDoctor.Module",
                MinimumDCMLVersion =
                    "0.1.0",
                RequiresRestart =
                    false
            };
        }
    }
}
