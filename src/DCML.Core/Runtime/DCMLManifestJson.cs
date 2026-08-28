using System;
using System.Text.Json;
using DCML.Core.Models;

namespace DCML.Core.Runtime
{
    /// <summary>
    /// Serializes and deserializes DCML module manifests.
    /// </summary>
    public static class DCMLManifestJson
    {
        private static readonly JsonSerializerOptions SerializerOptions =
            new JsonSerializerOptions
            {
                PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase,

                PropertyNameCaseInsensitive =
                    true,

                WriteIndented =
                    true
            };

        /// <summary>
        /// Serializes a manifest to JSON.
        /// </summary>
        public static string Serialize(
            DCMLModuleManifest manifest
        )
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(
                    nameof(manifest)
                );
            }

            return JsonSerializer.Serialize(
                manifest,
                SerializerOptions
            );
        }

        /// <summary>
        /// Parses and validates a manifest from JSON.
        /// </summary>
        public static DCMLManifestReadResult Deserialize(
            string json
        )
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return CreateParseFailure(
                    "DCML_MANIFEST_JSON_REQUIRED",
                    "Manifest JSON is required."
                );
            }

            try
            {
                DCMLModuleManifest? manifest =
                    JsonSerializer.Deserialize<DCMLModuleManifest>(
                        json,
                        SerializerOptions
                    );

                if (manifest == null)
                {
                    return CreateParseFailure(
                        "DCML_MANIFEST_JSON_NULL",
                        "Manifest JSON did not contain a manifest object."
                    );
                }

                DCMLValidationResult validation =
                    DCMLModuleManifestValidator.Validate(
                        manifest
                    );

                return new DCMLManifestReadResult(
                    manifest,
                    validation
                );
            }
            catch (JsonException exception)
            {
                return CreateParseFailure(
                    "DCML_MANIFEST_JSON_INVALID",
                    exception.Message
                );
            }
        }

        private static DCMLManifestReadResult CreateParseFailure(
            string code,
            string message
        )
        {
            var validation =
                new DCMLValidationResult();

            return new DCMLManifestReadResult(
                null,
                validation,
                code,
                message
            );
        }
    }
}
