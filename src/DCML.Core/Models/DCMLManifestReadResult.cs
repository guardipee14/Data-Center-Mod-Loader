namespace DCML.Core.Models
{
    /// <summary>
    /// Contains the result of reading and validating a DCML
    /// module manifest.
    /// </summary>
    public sealed class DCMLManifestReadResult
    {
        public DCMLManifestReadResult(
            DCMLModuleManifest? manifest,
            DCMLValidationResult validation,
            string? errorCode = null,
            string? errorMessage = null
        )
        {
            Manifest = manifest;
            Validation = validation;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Gets whether the manifest was parsed and validated.
        /// </summary>
        public bool Success =>
            Manifest != null &&
            ErrorCode == null &&
            Validation.IsValid;

        /// <summary>
        /// Gets the parsed manifest, when parsing succeeded.
        /// </summary>
        public DCMLModuleManifest? Manifest { get; }

        /// <summary>
        /// Gets manifest validation information.
        /// </summary>
        public DCMLValidationResult Validation { get; }

        /// <summary>
        /// Gets a stable parsing error code, when parsing failed.
        /// </summary>
        public string? ErrorCode { get; }

        /// <summary>
        /// Gets a human-readable parsing error message.
        /// </summary>
        public string? ErrorMessage { get; }
    }
}
