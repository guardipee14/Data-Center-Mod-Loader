namespace DCML.Core.Models
{
    /// <summary>
    /// Describes one validation problem found in a DCML manifest.
    /// </summary>
    public sealed class DCMLValidationIssue
    {
        public DCMLValidationIssue(
            string code,
            string message
        )
        {
            Code = code;
            Message = message;
        }

        /// <summary>
        /// Gets the stable validation code.
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// Gets the human-readable validation message.
        /// </summary>
        public string Message { get; }
    }
}
