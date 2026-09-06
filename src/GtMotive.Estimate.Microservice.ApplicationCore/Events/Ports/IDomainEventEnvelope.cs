using GtMotive.Estimate.Microservice.Domain.Interfaces;

namespace GtMotive.Estimate.Microservice.ApplicationCore.Events.Ports
{
    /// <summary>
    /// Holds a single domain event produced by the current use case execution for deferred publishing.
    /// </summary>
    public interface IDomainEventEnvelope
    {
        /// <summary>
        /// Gets the domain event held by this envelope.
        /// </summary>
        IDomainEvent DomainEvent { get; }

        /// <summary>
        /// Gets a value indicating whether an event has been stored in this envelope.
        /// </summary>
        bool HasEvent { get; }

        /// <summary>
        /// Stores a domain event in this envelope, replacing any previously stored event.
        /// </summary>
        /// <param name="domainEvent">The domain event to store.</param>
        void Add(IDomainEvent domainEvent);

        /// <summary>
        /// Clears the stored domain event from this envelope.
        /// </summary>
        void Clear();
    }
}
