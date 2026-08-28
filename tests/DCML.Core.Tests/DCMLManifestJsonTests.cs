using DCML.Core.Models;
using DCML.Core.Runtime;
using Xunit;

namespace DCML.Core.Tests
{
    public sealed class DCMLManifestJsonTests
    {
        [Fact]
        public void SerializeAndDeserialize_RoundTripsManifest()
        {
            var original =
                new DCMLModuleManifest
                {
                    Id =
                        "dcml.example.networkdoctor",
                    Name =
                        "Network Doctor",
                    Version =
                        "1.2.3",
                    Description =
                        "Diagnoses network problems.",
                    Author =
                        "DCML Test Suite",
                    EntryAssembly =
                        "NetworkDoctor.dll",
                    EntryType =
                        "NetworkDoctor.Module",
                    MinimumDCMLVersion =
                        "0.1.0"
                };

            original.Dependencies.Add(
                new DCMLModuleDependency
                {
                    Id =
                        "dcml.example.common",
                    MinimumVersion =
                        "1.0.0"
                }
            );

            string json =
                DCMLManifestJson.Serialize(
                    original
                );

            DCMLManifestReadResult result =
                DCMLManifestJson.Deserialize(
                    json
                );

            Assert.True(result.Success);
            Assert.NotNull(result.Manifest);

            Assert.Equal(
                original.Id,
                result.Manifest!.Id
            );

            Assert.Equal(
                original.Version,
                result.Manifest.Version
            );

            Assert.Equal(
                DCMLManifestSchema.CurrentVersion,
                result.Manifest.SchemaVersion
            );

            Assert.Single(
                result.Manifest.Dependencies
            );
        }

        [Fact]
        public void Deserialize_RejectsMalformedJson()
        {
            DCMLManifestReadResult result =
                DCMLManifestJson.Deserialize(
                    "{ definitely-not-json"
                );

            Assert.False(result.Success);

            Assert.Equal(
                "DCML_MANIFEST_JSON_INVALID",
                result.ErrorCode
            );
        }

        [Fact]
        public void Deserialize_RejectsEmptyJson()
        {
            DCMLManifestReadResult result =
                DCMLManifestJson.Deserialize(
                    string.Empty
                );

            Assert.False(result.Success);

            Assert.Equal(
                "DCML_MANIFEST_JSON_REQUIRED",
                result.ErrorCode
            );
        }

        [Fact]
        public void Deserialize_RejectsUnsupportedSchema()
        {
            const string json =
                @"{
  ""schemaVersion"": 999,
  ""id"": ""dcml.example.test"",
  ""name"": ""Test Module"",
  ""version"": ""1.0.0"",
  ""entryAssembly"": ""Test.dll"",
  ""entryType"": ""Test.Module"",
  ""requiresRestart"": false,
  ""dependencies"": []
}";

            DCMLManifestReadResult result =
                DCMLManifestJson.Deserialize(
                    json
                );

            Assert.False(result.Success);

            Assert.Contains(
                result.Validation.Issues,
                issue =>
                    issue.Code ==
                    "DCML_MANIFEST_SCHEMA_UNSUPPORTED"
            );
        }
    }
}
