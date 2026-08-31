using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.RentVehicle;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.RentVehicle.Models;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.RentVehicle.Ports;
using GtMotive.Estimate.Microservice.Domain.Entities;
using GtMotive.Estimate.Microservice.Domain.Events;
using GtMotive.Estimate.Microservice.Domain.Exceptions;
using GtMotive.Estimate.Microservice.Domain.Interfaces;
using Moq;
using Xunit;

namespace GtMotive.Estimate.Microservice.UnitTests.ApplicationCore.UseCases
{
    /// <summary>
    /// Unit tests for <see cref="RentVehicleUseCase"/>.
    /// </summary>
    public sealed class RentVehicleUseCaseTests
    {
        private readonly Mock<IVehicleWriteRepository> _vehicleRepository = new(MockBehavior.Strict);
        private readonly Mock<IVehicleReadRepository> _vehicleReadRepository = new(MockBehavior.Strict);
        private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);
        private readonly Mock<IRentVehicleOutputPort> _outputPort = new(MockBehavior.Strict);
        private readonly Mock<IAppLogger<RentVehicleUseCase>> _logger = new();
        private readonly Mock<ITelemetry> _telemetry = new(MockBehavior.Strict);
        private readonly Mock<IBusFactory> _busFactory = new(MockBehavior.Strict);
        private readonly Mock<IBus> _bus = new(MockBehavior.Strict);

        public RentVehicleUseCaseTests()
        {
            // Setup the factory to always return our private _bus mock
            _busFactory
                .Setup(f => f.GetClient(typeof(VehicleRentedEvent)))
                .Returns(_bus.Object);
        }

        /// <summary>
        /// Verifies that executing the use case with a null input payload throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test execution.</returns>
        [Fact]
        public async Task ExecuteWhenInputIsNullThrowsArgumentNullException()
        {
            var sut = CreateSut();

            var act = () => sut.Execute(null, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentNullException>();

            VerifyNoOtherCalls();
        }

        /// <summary>
        /// Verifies that executing the use case with a non-existent vehicle ID invokes the output port's not found handler.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test execution.</returns>
        [Fact]
        public async Task ExecuteWhenVehicleDoesNotExistCallsNotFoundHandle()
        {
            var input = new RentVehicleInput(Guid.NewGuid(), "renter-1");

            _vehicleRepository
                .Setup(r => r.GetByIdAsync(input.VehicleId, CancellationToken.None))
                .ReturnsAsync((Vehicle)null);

            _outputPort
                .Setup(p => p.NotFoundHandle($"Vehicle '{input.VehicleId}' was not found."))
                .Verifiable();

            var sut = CreateSut();

            await sut.Execute(input, CancellationToken.None);

            _vehicleRepository.Verify(r => r.GetByIdAsync(input.VehicleId, CancellationToken.None), Times.Once);
            _outputPort.Verify(p => p.NotFoundHandle(It.IsAny<string>()), Times.Once);

            VerifyNoOtherCalls();
        }

        /// <summary>
        /// Verifies that attempting to rent a vehicle when the renter already holds an active rental throws a <see cref="DomainException"/>.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test execution.</returns>
        [Fact]
        public async Task ExecuteWhenRenterAlreadyHasAnActiveRentalThrowsDomainException()
        {
            var vehicle = new Vehicle(Guid.NewGuid(), "Toyota", "Corolla", "1234ABC", DateTime.UtcNow.AddYears(-1));
            var input = new RentVehicleInput(vehicle.Id, "renter-1");

            _vehicleRepository
                .Setup(r => r.GetByIdAsync(input.VehicleId, CancellationToken.None))
                .ReturnsAsync(vehicle);

            _vehicleReadRepository
                .Setup(r => r.HasActiveRentalAsync(input.RenterId, CancellationToken.None))
                .ReturnsAsync(true);

            var sut = CreateSut();

            var act = () => sut.Execute(input, CancellationToken.None);

            await act.Should().ThrowAsync<DomainException>();

            _vehicleRepository.Verify(r => r.GetByIdAsync(input.VehicleId, CancellationToken.None), Times.Once);
            _vehicleReadRepository.Verify(r => r.HasActiveRentalAsync(input.RenterId, CancellationToken.None), Times.Once);

            VerifyNoOtherCalls();
        }

        /// <summary>
        /// Verifies that executing the use case with an available vehicle and an eligible renter successfully rents the vehicle, emits telemetry, sends the domain event, and notifies the output port.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test execution.</returns>
        [Fact]
        public async Task ExecuteWhenVehicleIsAvailableAndRenterHasNoActiveRentalRentsTheVehicle()
        {
            var vehicle = new Vehicle(Guid.NewGuid(), "Toyota", "Corolla", "1234ABC", DateTime.UtcNow.AddYears(-1));
            var input = new RentVehicleInput(vehicle.Id, "renter-1");

            _vehicleRepository
                .Setup(r => r.GetByIdAsync(input.VehicleId, CancellationToken.None))
                .ReturnsAsync(vehicle);

            _vehicleReadRepository
                .Setup(r => r.HasActiveRentalAsync(input.RenterId, CancellationToken.None))
                .ReturnsAsync(false);

            _vehicleRepository
                .Setup(r => r.UpdateAsync(
                    It.Is<Vehicle>(v => v.Id == vehicle.Id && v.Status == VehicleStatus.Rented && v.RenterId == input.RenterId),
                    CancellationToken.None))
                .Returns(Task.CompletedTask);

            _unitOfWork
                .Setup(u => u.Save())
                .ReturnsAsync(1);

            _telemetry
                .Setup(t => t.TrackEvent(
                    nameof(VehicleRentedEvent),
                    It.Is<IDictionary<string, string>>(d =>
                        d[nameof(VehicleRentedEvent.VehicleId)] == vehicle.Id.ToString() &&
                        d[nameof(VehicleRentedEvent.RenterId)] == input.RenterId),
                    null))
                .Verifiable();

            _telemetry
                .Setup(t => t.TrackMetric(nameof(VehicleRentedEvent), 1, null))
                .Verifiable();

            _bus
                .Setup(b => b.Send(
                    It.Is<VehicleRentedEvent>(e =>
                        e.VehicleId == vehicle.Id &&
                        e.RenterId == input.RenterId &&
                        e.RentedAt != default)))
                .Returns(Task.CompletedTask);

            _outputPort
                .Setup(p => p.StandardHandle(It.Is<RentVehicleOutput>(output =>
                    output.VehicleId == vehicle.Id &&
                    output.RenterId == input.RenterId &&
                    output.RentedAt != default)))
                .Verifiable();

            var sut = CreateSut();

            await sut.Execute(input, CancellationToken.None);

            vehicle.Status.Should().Be(VehicleStatus.Rented);

            _vehicleRepository.Verify(r => r.GetByIdAsync(input.VehicleId, CancellationToken.None), Times.Once);
            _vehicleReadRepository.Verify(r => r.HasActiveRentalAsync(input.RenterId, CancellationToken.None), Times.Once);
            _vehicleRepository.Verify(r => r.UpdateAsync(It.IsAny<Vehicle>(), CancellationToken.None), Times.Once);
            _unitOfWork.Verify(u => u.Save(), Times.Once);
            _telemetry.Verify(
                t => t.TrackEvent(
                    nameof(VehicleRentedEvent),
                    It.IsAny<IDictionary<string, string>>(),
                    null),
                Times.Once);
            _telemetry.Verify(t => t.TrackMetric(nameof(VehicleRentedEvent), 1, null), Times.Once);
            _bus.Verify(b => b.Send(It.IsAny<VehicleRentedEvent>()), Times.Once);
            _outputPort.Verify(p => p.StandardHandle(It.IsAny<RentVehicleOutput>()), Times.Once);

            VerifyNoOtherCalls();
        }

        private RentVehicleUseCase CreateSut() =>
            new(
                _vehicleRepository.Object,
                _vehicleReadRepository.Object,
                _unitOfWork.Object,
                _outputPort.Object,
                _logger.Object,
                _telemetry.Object,
                _busFactory.Object);

        private void VerifyNoOtherCalls()
        {
            _vehicleRepository.VerifyNoOtherCalls();
            _vehicleReadRepository.VerifyNoOtherCalls();
            _unitOfWork.VerifyNoOtherCalls();
            _outputPort.VerifyNoOtherCalls();
            _telemetry.VerifyNoOtherCalls();
            _busFactory.VerifyNoOtherCalls();
            _bus.VerifyNoOtherCalls(); // Added _bus to strict verification
        }
    }
}
