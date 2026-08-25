using System;

namespace GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ReturnVehicle.Models
{
    /// <summary>
    /// Output message for the Return Vehicle use case.
    /// </summary>
    /// <param name="VehicleId">Identifier of the returned vehicle.</param>
    public sealed record ReturnVehicleOutput(Guid VehicleId) : IUseCaseOutput;
}
