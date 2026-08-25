using System;
using GtMotive.Estimate.Microservice.Domain.Interfaces;

namespace GtMotive.Estimate.Microservice.Domain.Events
{
    /// <summary>
    /// Represents a domain event raised when a rented vehicle is returned to the fleet.
    /// </summary>
    /// <param name="VehicleId">The unique identifier of the returned vehicle.</param>
    /// <param name="ReturnedAt">The UTC timestamp when the vehicle was returned.</param>
    public sealed record VehicleReturnedEvent(
        Guid VehicleId,
        DateTime ReturnedAt) : IDomainEvent;
}
