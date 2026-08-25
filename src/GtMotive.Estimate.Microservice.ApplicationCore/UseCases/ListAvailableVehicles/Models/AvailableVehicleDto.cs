using System;

namespace GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ListAvailableVehicles.Models
{
    /// <summary>
    /// Read model for an available vehicle.
    /// </summary>
    /// <param name="Id">Vehicle identifier.</param>
    /// <param name="Brand">Vehicle brand.</param>
    /// <param name="Model">Vehicle model.</param>
    /// <param name="LicensePlate">Vehicle license plate.</param>
    /// <param name="ManufactureDate">Vehicle manufacture date.</param>
    public sealed record AvailableVehicleDto(Guid Id, string Brand, string Model, string LicensePlate, DateTime ManufactureDate);
}
