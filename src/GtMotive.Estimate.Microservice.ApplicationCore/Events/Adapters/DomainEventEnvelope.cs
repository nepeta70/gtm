using GtMotive.Estimate.Microservice.ApplicationCore.Events.Ports;
using GtMotive.Estimate.Microservice.Domain.Interfaces;

namespace GtMotive.Estimate.Microservice.ApplicationCore.Events.Adapters
{
    /// <summary>
    /// Default implementation of <see cref="IDomainEventEnvelope"/> that holds a single domain event
    /// for the current request scope.
    /// </summary>
    public sealed class DomainEventEnvelope : IDomainEventEnvelope
    {
        /// <summary>
        /// Gets the domain event held by this envelope, or <see langword="null"/> if none has been stored.
        /// </summary>
        public IDomainEvent DomainEvent { get; private set; }

        /// <summary>
        /// Gets a value indicating whether an event has been stored in this envelope.
        /// </summary>
        public bool HasEvent => DomainEvent is not null;

        /// <summary>
        /// Stores a domain event in this envelope, replacing any previously stored event.
        /// </summary>
        /// <param name="domainEvent">The domain event to store.</param>
        public void Add(IDomainEvent domainEvent)
        {
            DomainEvent = domainEvent;
        }

        /// <summary>
        /// Clears the stored domain event from this envelope.
        /// </summary>
        public void Clear()
        {
            DomainEvent = null;
        }
    }
}
