using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using GtMotive.Estimate.Microservice.Domain.Interfaces;

namespace GtMotive.Estimate.Microservice.Infrastructure.Bus
{
    /// <summary>
    /// Azure Service Bus implementation of <see cref="IBus"/>.
    /// Sends messages to the configured queue or topic.
    /// </summary>
    public sealed class AzureServiceBus : IBus, IAsyncDisposable
    {
        private readonly ServiceBusClient _client;
        private readonly ServiceBusSender _sender;
        private readonly IAppLogger<AzureServiceBus> _logger;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBus"/> class.
        /// </summary>
        /// <param name="connectionString">The Azure Service Bus namespace connection string.</param>
        /// <param name="queueOrTopicName">The destination queue or topic name.</param>
        /// <param name="logger">The application logger.</param>
        public AzureServiceBus(string connectionString, string queueOrTopicName, IAppLogger<AzureServiceBus> logger)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
            ArgumentException.ThrowIfNullOrWhiteSpace(queueOrTopicName);
            ArgumentNullException.ThrowIfNull(logger);

            _client = new ServiceBusClient(connectionString);
            _sender = _client.CreateSender(queueOrTopicName);
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task Send(object message)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(message);

            var body = JsonSerializer.Serialize(message, message.GetType());
            var serviceBusMessage = new ServiceBusMessage(body)
            {
                ApplicationProperties =
                {
                    ["MessageType"] = message.GetType().FullName
                }
            };

            await _sender.SendMessageAsync(serviceBusMessage).ConfigureAwait(false);

            _logger.LogInformation(
                "Azure Service Bus sent message {MessageType} to {EntityPath}",
                message.GetType().Name,
                _sender.EntityPath);
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            await _sender.DisposeAsync().ConfigureAwait(false);
            await _client.DisposeAsync().ConfigureAwait(false);
        }
    }
}
