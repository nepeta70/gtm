using System;

namespace GtMotive.Estimate.Microservice.Domain.Entities
{
    /// <summary>
    /// Flat read model for listing available vehicles. No behavior, only data.
    /// </summary>
    public sealed record VehicleReadModel(
        Guid Id,
        string Brand,
        string Model,
        string LicensePlate,
        DateTime ManufactureDate);
}
