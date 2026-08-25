using System.Collections.Generic;

namespace GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ListAvailableVehicles.Models
{
    /// <summary>
    /// Output message for the List Available Vehicles use case.
    /// </summary>
    /// <param name="Vehicles">The available vehicles.</param>
    public sealed record ListAvailableVehiclesOutput(IReadOnlyCollection<AvailableVehicleDto> Vehicles) : IUseCaseOutput;
}
