using System.IO;
using DCML.Core.Runtime;
using Xunit;

namespace DCML.Core.Tests
{
    public sealed class DCMLEndToEndProbeManifestTests
    {
        [Fact]
        public void TestModuleManifest_IsValid()
        {
            string solutionRoot =
                Path.GetFullPath(
                    Path.Combine(
                        System.AppContext.BaseDirectory,
                        "..",
                        "..",
                        "..",
                        "..",
                        ".."
                    )
                );

            string manifestPath =
                Path.Combine(
                    solutionRoot,
                    "src",
                    "DCML.TestModule",
                    "manifest.json"
                );

            Assert.True(
                File.Exists(
                    manifestPath
                ),
                "DCML.TestModule manifest.json was not found."
            );

            string json =
                File.ReadAllText(
                    manifestPath
                );

            var result =
                DCMLManifestJson.Deserialize(
                    json
                );

            Assert.True(
                result.Success
            );

            Assert.NotNull(
                result.Manifest
            );

            Assert.Equal(
                "dcml.test.lifecycle",
                result.Manifest!.Id
            );

            Assert.Equal(
                "DCML.TestModule.dll",
                result.Manifest.EntryAssembly
            );

            Assert.Equal(
                "DCML.TestModule.TestModule",
                result.Manifest.EntryType
            );
        }
    }
}
