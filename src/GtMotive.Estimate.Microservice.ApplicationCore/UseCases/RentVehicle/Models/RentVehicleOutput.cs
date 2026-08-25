using System;

namespace GtMotive.Estimate.Microservice.ApplicationCore.UseCases.RentVehicle.Models
{
    /// <summary>
    /// Output message for the Rent Vehicle use case.
    /// </summary>
    /// <param name="VehicleId">Identifier of the rented vehicle.</param>
    /// <param name="RenterId">Identifier of the person renting the vehicle.</param>
    /// <param name="RentedAt">Date and time of the rental.</param>
    public sealed record RentVehicleOutput(Guid VehicleId, string RenterId, DateTime RentedAt) : IUseCaseOutput;
}
