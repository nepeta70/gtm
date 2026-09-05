using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ReturnVehicle;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ReturnVehicle.Models;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ReturnVehicle.Ports;
using GtMotive.Estimate.Microservice.Domain.Entities;
using GtMotive.Estimate.Microservice.Domain.Events;
using GtMotive.Estimate.Microservice.Domain.Interfaces;
using Moq;
using Xunit;

namespace GtMotive.Estimate.Microservice.UnitTests.ApplicationCore.UseCases
{
    /// <summary>
    /// Unit tests for <see cref="ReturnVehicleUseCase"/>.
    /// </summary>
    public sealed class ReturnVehicleUseCaseTests
    {
        private readonly Mock<IVehicleWriteRepository> _vehicleRepository = new(MockBehavior.Strict);
        private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);
        private readonly Mock<IReturnVehicleOutputPort> _outputPort = new(MockBehavior.Strict);
        private readonly Mock<IAppLogger<ReturnVehicleUseCase>> _logger = new();
        private readonly Mock<ITelemetry> _telemetry = new(MockBehavior.Strict);
        private readonly Mock<IBusFactory> _busFactory = new(MockBehavior.Strict);
        private readonly Mock<IBus> _bus = new(MockBehavior.Strict);

        public ReturnVehicleUseCaseTests()
        {
            // Setup the factory to always return our private _bus mock
            _busFactory
                .Setup(f => f.GetClient(typeof(VehicleReturnedEvent)))
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
            var input = new ReturnVehicleInput(Guid.NewGuid(), "renter-3");

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
        /// Verifies that executing the use case with an existing rented vehicle successfully returns the vehicle, updates persistence, emits telemetry, sends the domain event, and notifies the output port.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test execution.</returns>
        [Fact]
        public async Task ExecuteWhenVehicleExistsReturnsVehicleAndPresentsOutput()
        {
            var vehicle = new Vehicle(Guid.NewGuid(), "Toyota", "Corolla", "1234ABC", DateTime.UtcNow.AddYears(-1));
            vehicle.Rent("renter-1");

            var input = new ReturnVehicleInput(vehicle.Id, "renter-1");

            _vehicleRepository
                .Setup(r => r.GetByIdAsync(input.VehicleId, CancellationToken.None))
                .ReturnsAsync(vehicle);

            _vehicleRepository
                .Setup(r => r.UpdateAsync(
                    It.Is<Vehicle>(v => v.Id == vehicle.Id && v.Status == VehicleStatus.Available),
                    CancellationToken.None))
                .Returns(Task.CompletedTask);

            _unitOfWork
                .Setup(u => u.Save())
                .ReturnsAsync(1);

            _telemetry
                .Setup(t => t.TrackEvent(
                    nameof(VehicleReturnedEvent),
                    It.Is<IDictionary<string, string>>(d =>
                        d[nameof(VehicleReturnedEvent.VehicleId)] == vehicle.Id.ToString()),
                    null))
                .Verifiable();

            _telemetry
                .Setup(t => t.TrackMetric(nameof(VehicleReturnedEvent), 1, null))
                .Verifiable();

            _bus
                .Setup(b => b.Send(
                    It.Is<VehicleReturnedEvent>(e =>
                        e.VehicleId == vehicle.Id &&
                        e.ReturnedAt != default)))
                .Returns(Task.CompletedTask);

            _outputPort
                .Setup(p => p.StandardHandle(It.Is<ReturnVehicleOutput>(output =>
                    output.VehicleId == vehicle.Id)))
                .Verifiable();

            var sut = CreateSut();

            await sut.Execute(input, CancellationToken.None);

            vehicle.Status.Should().Be(VehicleStatus.Available);

            _vehicleRepository.Verify(r => r.GetByIdAsync(input.VehicleId, CancellationToken.None), Times.Once);
            _vehicleRepository.Verify(r => r.UpdateAsync(It.IsAny<Vehicle>(), CancellationToken.None), Times.Once);
            _unitOfWork.Verify(u => u.Save(), Times.Once);
            _telemetry.Verify(
                t => t.TrackEvent(
                    nameof(VehicleReturnedEvent),
                    It.IsAny<IDictionary<string, string>>(),
                    null),
                Times.Once);
            _telemetry.Verify(t => t.TrackMetric(nameof(VehicleReturnedEvent), 1, null), Times.Once);
            _bus.Verify(b => b.Send(It.IsAny<VehicleReturnedEvent>()), Times.Once);
            _outputPort.Verify(p => p.StandardHandle(It.IsAny<ReturnVehicleOutput>()), Times.Once);

            VerifyNoOtherCalls();
        }

        /// <summary>
        /// Verifies that when unit of work save fails, telemetry, bus message, and output port operations are skipped.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test execution.</returns>
        [Fact]
        public async Task ExecuteWhenUnitOfWorkFailsDoesNotCallOutputPortOrBus()
        {
            var vehicle = new Vehicle(Guid.NewGuid(), "Toyota", "Corolla", "1234ABC", DateTime.UtcNow.AddYears(-1));
            vehicle.Rent("renter-1");

            var input = new ReturnVehicleInput(vehicle.Id, "renter-1");

            _vehicleRepository
                .Setup(r => r.GetByIdAsync(input.VehicleId, CancellationToken.None))
                .ReturnsAsync(vehicle);

            _vehicleRepository
                .Setup(r => r.UpdateAsync(It.IsAny<Vehicle>(), CancellationToken.None))
                .Returns(Task.CompletedTask);

            _unitOfWork
                .Setup(u => u.Save())
                .ThrowsAsync(new InvalidOperationException("Database error"));

            var sut = CreateSut();

            var act = () => sut.Execute(input, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();

            _vehicleRepository.Verify(r => r.GetByIdAsync(input.VehicleId, CancellationToken.None), Times.Once);
            _vehicleRepository.Verify(r => r.UpdateAsync(It.IsAny<Vehicle>(), CancellationToken.None), Times.Once);
            _unitOfWork.Verify(u => u.Save(), Times.Once);

            VerifyNoOtherCalls();
        }

        private ReturnVehicleUseCase CreateSut() =>
            new(
                _vehicleRepository.Object,
                _unitOfWork.Object,
                _outputPort.Object,
                _logger.Object,
                _telemetry.Object,
                _busFactory.Object);

        private void VerifyNoOtherCalls()
        {
            _vehicleRepository.VerifyNoOtherCalls();
            _unitOfWork.VerifyNoOtherCalls();
            _outputPort.VerifyNoOtherCalls();
            _telemetry.VerifyNoOtherCalls();
            _busFactory.VerifyNoOtherCalls();
            _bus.VerifyNoOtherCalls();
        }
    }
}
