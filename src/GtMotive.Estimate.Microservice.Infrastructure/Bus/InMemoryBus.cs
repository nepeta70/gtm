using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GtMotive.Estimate.Microservice.Domain.Interfaces;

namespace GtMotive.Estimate.Microservice.Infrastructure.Bus
{
    /// <summary>
    /// In-memory implementation of <see cref="IBus"/>.
    /// Useful for local development, Docker scenarios and integration tests
    /// where no external message broker is available.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="InMemoryBus"/> class.
    /// </remarks>
    /// <param name="logger">The application logger.</param>
    public sealed class InMemoryBus(IAppLogger<InMemoryBus> logger) : IBus
    {
        private readonly IAppLogger<InMemoryBus> _logger = logger;
        private readonly ConcurrentBag<object> _messages = [];

        /// <summary>
        /// Gets a snapshot of the messages that have been sent through this bus instance.
        /// </summary>
        public IReadOnlyCollection<object> SentMessages => [.. _messages];

        /// <inheritdoc />
        public Task Send(object message, CancellationToken cancellationToken = default)
        {
            if (message is null)
            {
                return Task.CompletedTask;
            }

            _messages.Add(message);

            _logger.LogInformation(
                "In-memory bus sent message {MessageType}: {@Message}",
                message.GetType().Name,
                message);

            return Task.CompletedTask;
        }
    }
}
