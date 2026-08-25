using System;

namespace GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ReturnVehicle.Models
{
    /// <summary>
    /// Input message for the Return Vehicle use case.
    /// </summary>
    /// <param name="VehicleId">Identifier of the vehicle being returned.</param>
    public sealed record ReturnVehicleInput(Guid VehicleId) : IUseCaseInput;
}
