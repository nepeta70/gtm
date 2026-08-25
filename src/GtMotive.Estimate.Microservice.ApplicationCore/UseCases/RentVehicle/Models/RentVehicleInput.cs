using System;

namespace GtMotive.Estimate.Microservice.ApplicationCore.UseCases.RentVehicle.Models
{
    /// <summary>
    /// Input message for the Rent Vehicle use case.
    /// </summary>
    /// <param name="VehicleId">Identifier of the vehicle to rent.</param>
    /// <param name="RenterId">Identifier of the person renting the vehicle.</param>
    public sealed record RentVehicleInput(Guid VehicleId, string RenterId) : IUseCaseInput;
}
