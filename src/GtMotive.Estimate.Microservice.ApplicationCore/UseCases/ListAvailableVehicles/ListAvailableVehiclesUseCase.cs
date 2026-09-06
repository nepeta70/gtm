using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ListAvailableVehicles.Models;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ListAvailableVehicles.Ports;
using GtMotive.Estimate.Microservice.Domain.Interfaces;

namespace GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ListAvailableVehicles
{
    /// <summary>
    /// Lists the vehicles that are currently available for rent.
    /// </summary>
    /// <param name="vehicleRepository">The vehicle repository instance.</param>
    /// <param name="outputPort">The output port handler for returning the response.</param>
    public sealed class ListAvailableVehiclesUseCase(
        IVehicleReadRepository vehicleRepository,
        IListAvailableVehiclesOutputPort outputPort) : IUseCase<ListAvailableVehiclesInput>
    {
        /// <summary>
        /// Executes the process of retrieving all available vehicles for rent.
        /// </summary>
        /// <param name="input">The input request model for listing available vehicles.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task Execute(ListAvailableVehiclesInput input, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(input);

            var vehicles = await vehicleRepository.GetAvailableAsync(cancellationToken);

            var dtos = vehicles
                .Select(v => new AvailableVehicleDto(v.Id, v.Brand, v.Model, v.LicensePlate, v.ManufactureDate))
                .ToList();

            outputPort.StandardHandle(new ListAvailableVehiclesOutput(dtos));
        }
    }
}
