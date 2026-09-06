using System;
using System.Threading;
using System.Threading.Tasks;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.CreateVehicle.Models;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.CreateVehicle.Ports;
using GtMotive.Estimate.Microservice.Domain.Entities;
using GtMotive.Estimate.Microservice.Domain.Events;
using GtMotive.Estimate.Microservice.Domain.Interfaces;

namespace GtMotive.Estimate.Microservice.ApplicationCore.UseCases.CreateVehicle
{
    /// <summary>
    /// Registers a new vehicle in the fleet.
    /// </summary>
    /// <param name="vehicleRepository">The vehicle repository persistence port.</param>
    /// <param name="unitOfWork">The unit of work port for transaction boundary management.</param>
    /// <param name="outputPort">The output port to present use case execution results.</param>
    /// <param name="logger">The application logging abstraction.</param>
    /// <param name="busFactory">The message bus factory abstraction for domain event publishing.</param>
    public sealed class CreateVehicleUseCase(
        IVehicleWriteRepository vehicleRepository,
        IUnitOfWork unitOfWork,
        ICreateVehicleOutputPort outputPort,
        IAppLogger<CreateVehicleUseCase> logger,
        IBusFactory busFactory) : IUseCase<CreateVehicleInput>
    {
        /// <summary>
        /// Executes the vehicle creation use case.
        /// </summary>
        /// <param name="input">Input payload containing initial vehicle data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task Execute(CreateVehicleInput input, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(input);

            logger.LogInformation("Creating vehicle with license plate: {LicensePlate}", input.LicensePlate);

            var vehicle = new Vehicle(
                Guid.NewGuid(),
                input.Brand,
                input.Model,
                input.LicensePlate,
                input.ManufactureDate);

            await vehicleRepository.AddAsync(vehicle, cancellationToken);
            await unitOfWork.Save(cancellationToken);

            var vehicleCreatedEvent = new VehicleCreatedEvent(
                vehicle.Id,
                vehicle.LicensePlate.Value,
                DateTime.UtcNow);

            await busFactory.GetClient(typeof(VehicleCreatedEvent)).Send(vehicleCreatedEvent, cancellationToken);

            logger.LogInformation("Vehicle {VehicleId} successfully created", vehicle.Id);

            outputPort.StandardHandle(new CreateVehicleOutput(
                vehicle.Id,
                vehicle.Brand,
                vehicle.Model,
                vehicle.LicensePlate.Value,
                vehicle.ManufactureDate));
        }
    }
}
