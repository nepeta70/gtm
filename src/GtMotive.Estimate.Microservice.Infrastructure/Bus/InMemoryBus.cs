using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using GtMotive.Estimate.Microservice.Domain.Interfaces;

namespace GtMotive.Estimate.Microservice.Infrastructure.Bus
{
    /// <summary>
    /// In-memory implementation of <see cref="IBus"/>.
    /// Useful for local development, Docker scenarios and integration tests
    /// where no external message broker is available.
    /// </summary>
    public sealed class InMemoryBus : IBus
    {
        private readonly IAppLogger<InMemoryBus> _logger;
        private readonly ConcurrentBag<object> _messages = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryBus"/> class.
        /// </summary>
        /// <param name="logger">The application logger.</param>
        public InMemoryBus(IAppLogger<InMemoryBus> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets a snapshot of the messages that have been sent through this bus instance.
        /// </summary>
        public IReadOnlyCollection<object> SentMessages => [.. _messages];

        /// <inheritdoc />
        public Task Send(object message)
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
