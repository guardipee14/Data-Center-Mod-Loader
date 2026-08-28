using System;
using System.IO;
using System.Linq;
using DCML.Core.Models;
using DCML.Core.Runtime;
using Xunit;

namespace DCML.Core.Tests
{
    public sealed class DCMLPackageDiscoveryTests
    {
        [Fact]
        public void Discover_FindsValidPackage()
        {
            string root =
                CreateTemporaryRoot();

            try
            {
                CreatePackage(
                    root,
                    "NetworkDoctor",
                    "dcml.example.networkdoctor"
                );

                DCMLPackageDiscoveryResult result =
                    DCMLPackageDiscovery.Discover(
                        root
                    );

                Assert.True(result.Success);
                Assert.Single(result.Packages);
                Assert.Empty(result.Failures);

                Assert.Equal(
                    "dcml.example.networkdoctor",
                    result.Packages[0].Manifest.Id
                );
            }
            finally
            {
                DeleteTemporaryRoot(
                    root
                );
            }
        }

        [Fact]
        public void Discover_BadPackageDoesNotBlockGoodPackage()
        {
            string root =
                CreateTemporaryRoot();

            try
            {
                CreatePackage(
                    root,
                    "Good",
                    "dcml.example.good"
                );

                string badDirectory =
                    Path.Combine(
                        root,
                        "Bad"
                    );

                Directory.CreateDirectory(
                    badDirectory
                );

                File.WriteAllText(
                    Path.Combine(
                        badDirectory,
                        "manifest.json"
                    ),
                    "{ definitely-not-json"
                );

                DCMLPackageDiscoveryResult result =
                    DCMLPackageDiscovery.Discover(
                        root
                    );

                Assert.False(result.Success);
                Assert.Single(result.Packages);
                Assert.Single(result.Failures);

                Assert.Equal(
                    "dcml.example.good",
                    result.Packages[0].Manifest.Id
                );

                Assert.Equal(
                    "DCML_MANIFEST_JSON_INVALID",
                    result.Failures[0].ErrorCode
                );
            }
            finally
            {
                DeleteTemporaryRoot(
                    root
                );
            }
        }

        [Fact]
        public void Discover_ReportsMissingManifest()
        {
            string root =
                CreateTemporaryRoot();

            try
            {
                Directory.CreateDirectory(
                    Path.Combine(
                        root,
                        "MissingManifest"
                    )
                );

                DCMLPackageDiscoveryResult result =
                    DCMLPackageDiscovery.Discover(
                        root
                    );

                Assert.Empty(result.Packages);
                Assert.Single(result.Failures);

                Assert.Equal(
                    "DCML_PACKAGE_MANIFEST_NOT_FOUND",
                    result.Failures[0].ErrorCode
                );
            }
            finally
            {
                DeleteTemporaryRoot(
                    root
                );
            }
        }

        [Fact]
        public void Discover_PreservesManifestValidationIssues()
        {
            string root =
                CreateTemporaryRoot();

            try
            {
                CreatePackage(
                    root,
                    "InvalidVersion",
                    "dcml.example.invalid",
                    "banana"
                );

                DCMLPackageDiscoveryResult result =
                    DCMLPackageDiscovery.Discover(
                        root
                    );

                Assert.Empty(result.Packages);
                Assert.Single(result.Failures);

                Assert.Equal(
                    "DCML_PACKAGE_MANIFEST_INVALID",
                    result.Failures[0].ErrorCode
                );

                Assert.Contains(
                    result.Failures[0].ValidationIssues,
                    issue =>
                        issue.Code ==
                        "DCML_MANIFEST_VERSION_INVALID"
                );
            }
            finally
            {
                DeleteTemporaryRoot(
                    root
                );
            }
        }

        [Fact]
        public void Discover_RejectsDuplicateModuleIds()
        {
            string root =
                CreateTemporaryRoot();

            try
            {
                CreatePackage(
                    root,
                    "First",
                    "dcml.example.duplicate"
                );

                CreatePackage(
                    root,
                    "Second",
                    "DCML.EXAMPLE.DUPLICATE"
                );

                DCMLPackageDiscoveryResult result =
                    DCMLPackageDiscovery.Discover(
                        root
                    );

                Assert.Single(result.Packages);
                Assert.Single(result.Failures);

                Assert.Equal(
                    "DCML_PACKAGE_DUPLICATE_MODULE_ID",
                    result.Failures[0].ErrorCode
                );
            }
            finally
            {
                DeleteTemporaryRoot(
                    root
                );
            }
        }

        [Fact]
        public void Discover_ReportsMissingRoot()
        {
            string root =
                Path.Combine(
                    Path.GetTempPath(),
                    "DCML-Core-Tests",
                    Guid.NewGuid().ToString("N")
                );

            DCMLPackageDiscoveryResult result =
                DCMLPackageDiscovery.Discover(
                    root
                );

            Assert.Empty(result.Packages);
            Assert.Single(result.Failures);

            Assert.Equal(
                "DCML_DISCOVERY_ROOT_NOT_FOUND",
                result.Failures[0].ErrorCode
            );
        }

        private static string CreateTemporaryRoot()
        {
            string root =
                Path.Combine(
                    Path.GetTempPath(),
                    "DCML-Core-Tests",
                    Guid.NewGuid().ToString("N")
                );

            Directory.CreateDirectory(
                root
            );

            return root;
        }

        private static void CreatePackage(
            string root,
            string directoryName,
            string moduleId,
            string version = "1.0.0"
        )
        {
            string packageDirectory =
                Path.Combine(
                    root,
                    directoryName
                );

            Directory.CreateDirectory(
                packageDirectory
            );

            var manifest =
                new DCMLModuleManifest
                {
                    Id =
                        moduleId,
                    Name =
                        directoryName,
                    Version =
                        version,
                    EntryAssembly =
                        directoryName + ".dll",
                    EntryType =
                        directoryName + ".Module"
                };

            string json =
                DCMLManifestJson.Serialize(
                    manifest
                );

            File.WriteAllText(
                Path.Combine(
                    packageDirectory,
                    "manifest.json"
                ),
                json
            );
        }

        private static void DeleteTemporaryRoot(
            string root
        )
        {
            if (
                Directory.Exists(
                    root
                )
            )
            {
                Directory.Delete(
                    root,
                    true
                );
            }
        }
    }
}
