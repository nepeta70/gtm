using System;
using GtMotive.Estimate.Microservice.Domain.Interfaces;

namespace GtMotive.Estimate.Microservice.Domain.Events
{
    /// <summary>
    /// Represents a domain event raised when a vehicle is successfully rented.
    /// </summary>
    /// <param name="VehicleId">The unique identifier of the rented vehicle.</param>
    /// <param name="RenterId">The identifier of the person renting the vehicle.</param>
    /// <param name="RentedAt">The UTC timestamp when the rental started.</param>
    public sealed record VehicleRentedEvent(
        Guid VehicleId,
        string RenterId,
        DateTime RentedAt) : IDomainEvent;
}
