using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using GtMotive.Estimate.Microservice.Domain.Interfaces;
using Polly;

namespace GtMotive.Estimate.Microservice.Infrastructure.Bus
{
    /// <summary>
    /// Azure Service Bus implementation of <see cref="IBus"/>.
    /// Sends messages to the configured queue or topic through the shared Service Bus
    /// resilience pipeline (retry on transient failures, circuit breaker, timeout).
    /// </summary>
    public sealed class AzureServiceBus : IBus, IAsyncDisposable
    {
        private readonly ServiceBusClient _client;
        private readonly ServiceBusSender _sender;
        private readonly IAppLogger<AzureServiceBus> _logger;
        private readonly ResiliencePipeline _resiliencePipeline;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBus"/> class.
        /// </summary>
        /// <param name="connectionString">The Azure Service Bus namespace connection string.</param>
        /// <param name="queueOrTopicName">The destination queue or topic name.</param>
        /// <param name="logger">The application logger.</param>
        /// <param name="resiliencePipeline">The Service Bus resilience pipeline.</param>
        public AzureServiceBus(
            string connectionString,
            string queueOrTopicName,
            IAppLogger<AzureServiceBus> logger,
            ResiliencePipeline resiliencePipeline)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
            ArgumentException.ThrowIfNullOrWhiteSpace(queueOrTopicName);
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(resiliencePipeline);

            _client = new ServiceBusClient(connectionString);
            _sender = _client.CreateSender(queueOrTopicName);
            _logger = logger;
            _resiliencePipeline = resiliencePipeline;
        }

        /// <inheritdoc />
        public async Task Send(object message)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(message);

            var body = JsonSerializer.Serialize(message, message.GetType());
            var serviceBusMessage = new ServiceBusMessage(body)
            {
                // Stable id so broker-side deduplication can collapse retried sends
                // of the same logical message.
                MessageId = Guid.NewGuid().ToString(),
                ApplicationProperties =
                {
                    ["MessageType"] = message.GetType().FullName
                }
            };

            // IBus.Send exposes no cancellation token; the pipeline's timeout still applies.
            await _resiliencePipeline
                .ExecuteAsync(
                    async ct => await _sender.SendMessageAsync(serviceBusMessage, ct).ConfigureAwait(false),
                    CancellationToken.None)
                .ConfigureAwait(false);

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
