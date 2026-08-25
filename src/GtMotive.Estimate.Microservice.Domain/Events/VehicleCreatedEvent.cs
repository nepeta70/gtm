using System;
using GtMotive.Estimate.Microservice.Domain.Interfaces;

namespace GtMotive.Estimate.Microservice.Domain.Events
{
    /// <summary>
    /// Represents a domain event raised when a new vehicle is successfully registered in the fleet.
    /// </summary>
    /// <param name="VehicleId">The unique identifier of the created vehicle.</param>
    /// <param name="LicensePlate">The license plate of the created vehicle.</param>
    /// <param name="OccurredOn">The UTC timestamp when the event occurred.</param>
    public sealed record VehicleCreatedEvent(
        Guid VehicleId,
        string LicensePlate,
        DateTime OccurredOn) : IDomainEvent;
}
