namespace DCML.Core.Abstractions
{
    /// <summary>
    /// Defines the lifecycle contract implemented by a DCML module.
    /// </summary>
    public interface IDCMLModule
    {
        /// <summary>
        /// Gets the stable identifier for the module.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the human-readable module name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the module version.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Initializes the module and supplies its DCML context.
        /// This is called once for a module instance before Start.
        /// </summary>
        /// <param name="context">
        /// The context supplied by the active DCML host.
        /// </param>
        void Initialize(
            IDCMLModuleContext context
        );

        /// <summary>
        /// Starts the module.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the module and releases resources owned by it.
        /// </summary>
        void Stop();
    }
}
