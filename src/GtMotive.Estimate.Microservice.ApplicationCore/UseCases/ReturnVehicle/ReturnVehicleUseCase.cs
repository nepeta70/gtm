using System;
using System.Threading;
using System.Threading.Tasks;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ReturnVehicle.Models;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ReturnVehicle.Ports;
using GtMotive.Estimate.Microservice.Domain.Events;
using GtMotive.Estimate.Microservice.Domain.Interfaces;

namespace GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ReturnVehicle
{
    /// <summary>
    /// Returns a previously rented vehicle, making it available again.
    /// </summary>
    /// <param name="vehicleRepository">The vehicle repository instance.</param>
    /// <param name="unitOfWork">The unit of work instance for transactional consistency.</param>
    /// <param name="outputPort">The output port handler for returning the response.</param>
    /// <param name="logger">The application logging abstraction.</param>
    /// <param name="busFactory">The message bus factory abstraction for domain event publishing.</param>
    public sealed class ReturnVehicleUseCase(
        IVehicleWriteRepository vehicleRepository,
        IUnitOfWork unitOfWork,
        IReturnVehicleOutputPort outputPort,
        IAppLogger<ReturnVehicleUseCase> logger,
        IBusFactory busFactory) : IUseCase<ReturnVehicleInput>
    {
        /// <summary>
        /// Executes the process of returning a vehicle.
        /// </summary>
        /// <param name="input">The input model containing the vehicle ID to return.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task Execute(ReturnVehicleInput input, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(input);

            logger.LogInformation("Attempting to return vehicle {VehicleId}", input.VehicleId);

            var vehicle = await vehicleRepository.GetByIdAsync(input.VehicleId, cancellationToken);

            if (vehicle is null)
            {
                logger.LogWarning("Vehicle {VehicleId} was not found", input.VehicleId);
                outputPort.NotFoundHandle($"Vehicle '{input.VehicleId}' was not found.");
                return;
            }

            vehicle.Return(input.RenterId);

            await vehicleRepository.UpdateAsync(vehicle, cancellationToken);
            await unitOfWork.Save();

            var vehicleReturnedEvent = new VehicleReturnedEvent(
                vehicle.Id,
                DateTime.UtcNow);

            await busFactory.GetClient(typeof(VehicleReturnedEvent)).Send(vehicleReturnedEvent);

            logger.LogInformation("Vehicle {VehicleId} successfully returned", vehicle.Id);

            outputPort.StandardHandle(new ReturnVehicleOutput(vehicle.Id));
        }
    }
}
