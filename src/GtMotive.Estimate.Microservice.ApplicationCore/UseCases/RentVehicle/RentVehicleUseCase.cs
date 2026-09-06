using System;
using System.Threading;
using System.Threading.Tasks;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.RentVehicle.Models;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.RentVehicle.Ports;
using GtMotive.Estimate.Microservice.Domain.Events;
using GtMotive.Estimate.Microservice.Domain.Exceptions;
using GtMotive.Estimate.Microservice.Domain.Interfaces;

namespace GtMotive.Estimate.Microservice.ApplicationCore.UseCases.RentVehicle
{
    /// <summary>
    /// Rents an available vehicle to a customer.
    /// Enforces the business rule that a single person cannot have more than one active rental at a time.
    /// </summary>
    /// <param name="vehicleRepository">The vehicle repository persistence port.</param>
    /// <param name="vehicleReadRepository">The vehicle read repository persistence port.</param>
    /// <param name="unitOfWork">The unit of work port for transaction boundary management.</param>
    /// <param name="outputPort">The output port to present use case execution results.</param>
    /// <param name="logger">The application logging abstraction.</param>
    /// <param name="busFactory">The message bus factory abstraction for domain event publishing.</param>
    public sealed class RentVehicleUseCase(
        IVehicleWriteRepository vehicleRepository,
        IVehicleReadRepository vehicleReadRepository,
        IUnitOfWork unitOfWork,
        IRentVehicleOutputPort outputPort,
        IAppLogger<RentVehicleUseCase> logger,
        IBusFactory busFactory) : IUseCase<RentVehicleInput>
    {
        /// <summary>
        /// Executes the process of renting a vehicle to a customer.
        /// </summary>
        /// <param name="input">The input model containing the vehicle ID and renter ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task Execute(RentVehicleInput input, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(input);

            logger.LogInformation("Attempting to rent vehicle {VehicleId} to renter {RenterId}", input.VehicleId, input.RenterId);

            var vehicle = await vehicleRepository.GetByIdAsync(input.VehicleId, cancellationToken);

            if (vehicle is null)
            {
                logger.LogWarning("Vehicle {VehicleId} was not found", input.VehicleId);
                outputPort.NotFoundHandle($"Vehicle '{input.VehicleId}' was not found.");
                return;
            }

            var renterHasActiveRental = await vehicleReadRepository.HasActiveRentalAsync(input.RenterId, cancellationToken);

            if (renterHasActiveRental)
            {
                logger.LogWarning("Renter {RenterId} already has an active vehicle rental", input.RenterId);
                throw new DomainException($"Renter '{input.RenterId}' already has an active vehicle rental.");
            }

            vehicle.Rent(input.RenterId);

            await vehicleRepository.UpdateAsync(vehicle, cancellationToken);
            await unitOfWork.Save();

            var vehicleRentedEvent = new VehicleRentedEvent(
                vehicle.Id,
                vehicle.RenterId,
                vehicle.RentedAt.Value);

            await busFactory.GetClient(typeof(VehicleRentedEvent)).Send(vehicleRentedEvent);

            logger.LogInformation("Vehicle {VehicleId} successfully rented to {RenterId}", vehicle.Id, vehicle.RenterId);

            outputPort.StandardHandle(new RentVehicleOutput(vehicle.Id, vehicle.RenterId, vehicle.RentedAt.Value));
        }
    }
}
