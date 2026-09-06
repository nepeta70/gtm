// Domain/Interfaces/IOutboxRepository.cs
using System.Threading;
using System.Threading.Tasks;

namespace GtMotive.Estimate.Microservice.Domain.Interfaces
{
    /// <summary>
    /// Outbox repository interface for saving domain events to the outbox.
    /// </summary>
    public interface IEventStore
    {
        /// <summary>
        /// Appends a domain event to the event store.
        /// </summary>
        /// <typeparam name="TEvent">The type of the domain event.</typeparam>
        /// <param name="domainEvent">The domain event to append.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task AppendAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken)
            where TEvent : notnull;
    }
}
