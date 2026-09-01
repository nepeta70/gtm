namespace GtMotive.Estimate.Microservice.Infrastructure.Bus.Settings
{
    /// <summary>
    /// Configuration options for the messaging bus.
    /// </summary>
    public sealed class BusSettings
    {
        /// <summary>
        /// Gets or sets the bus provider name.
        /// Supported values: InMemory, AzureServiceBus.
        /// </summary>
        public string Provider { get; set; } = "InMemory";

        /// <summary>
        /// Gets or sets the connection string for the external bus provider.
        /// Required when Provider is AzureServiceBus.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the default queue or topic name used when no event-specific name is configured.
        /// </summary>
        public string DefaultQueueOrTopicName { get; set; } = "gtmotive-events";
    }
}
