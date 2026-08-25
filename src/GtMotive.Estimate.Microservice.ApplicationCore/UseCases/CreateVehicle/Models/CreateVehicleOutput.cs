using System;

namespace GtMotive.Estimate.Microservice.ApplicationCore.UseCases.CreateVehicle.Models
{
    /// <summary>
    /// Output message for the Create Vehicle use case.
    /// </summary>
    /// <param name="Id">Identifier of the created vehicle.</param>
    /// <param name="Brand">Vehicle brand.</param>
    /// <param name="Model">Vehicle model.</param>
    /// <param name="LicensePlate">Vehicle license plate.</param>
    /// <param name="ManufactureDate">Vehicle manufacture date.</param>
    public sealed record CreateVehicleOutput(Guid Id, string Brand, string Model, string LicensePlate, DateTime ManufactureDate) : IUseCaseOutput;
}
