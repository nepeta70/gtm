using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GtMotive.Estimate.Microservice.ApplicationCore.Events.Ports;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.CreateVehicle;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.CreateVehicle.Models;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.CreateVehicle.Ports;
using GtMotive.Estimate.Microservice.Domain.Entities;
using GtMotive.Estimate.Microservice.Domain.Events;
using GtMotive.Estimate.Microservice.Domain.Interfaces;
using Moq;
using Xunit;

namespace GtMotive.Estimate.Microservice.UnitTests.ApplicationCore.UseCases
{
    /// <summary>
    /// Unit tests for <see cref="CreateVehicleUseCase"/>.
    /// </summary>
    public sealed class CreateVehicleUseCaseTests
    {
        private readonly Mock<IVehicleWriteRepository> _vehicleRepository = new(MockBehavior.Strict);
        private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);
        private readonly Mock<ICreateVehicleOutputPort> _outputPort = new(MockBehavior.Strict);
        private readonly Mock<IAppLogger<CreateVehicleUseCase>> _logger = new();
        private readonly Mock<IDomainEventEnvelope> _eventCollector = new(MockBehavior.Strict);

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
        /// Verifies that executing the use case with valid input persists the vehicle, adds the domain event to the envelope, and notifies the output port.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test execution.</returns>
        [Fact]
        public async Task ExecuteWithValidInputCreatesVehicleAndPresentsOutput()
        {
            var manufactureDate = DateTime.Today.AddYears(-1);
            var input = new CreateVehicleInput("Toyota", "Corolla", "1234ABC", manufactureDate);

            _vehicleRepository
                .Setup(r => r.AddAsync(
                    It.Is<Vehicle>(v =>
                        v.Brand == input.Brand &&
                        v.Model == input.Model &&
                        v.LicensePlate.Value == input.LicensePlate &&
                        v.ManufactureDate == input.ManufactureDate &&
                        v.Id != Guid.Empty),
                    CancellationToken.None))
                .Returns(Task.CompletedTask);

            _unitOfWork
                .Setup(u => u.Save(CancellationToken.None))
                .ReturnsAsync(1);

            _eventCollector
                .Setup(c => c.Add(It.Is<VehicleCreatedEvent>(e =>
                    e.LicensePlate == input.LicensePlate &&
                    e.VehicleId != Guid.Empty &&
                    e.CreatedOn != default)))
                .Verifiable();

            _outputPort
                .Setup(p => p.StandardHandle(It.Is<CreateVehicleOutput>(output =>
                    output.Brand == input.Brand &&
                    output.Model == input.Model &&
                    output.LicensePlate == input.LicensePlate &&
                    output.ManufactureDate == input.ManufactureDate &&
                    output.Id != Guid.Empty)))
                .Verifiable();

            var sut = CreateSut();

            await sut.Execute(input, CancellationToken.None);

            _vehicleRepository.Verify(r => r.AddAsync(It.IsAny<Vehicle>(), CancellationToken.None), Times.Once);
            _unitOfWork.Verify(u => u.Save(CancellationToken.None), Times.Once);
            _eventCollector.Verify(c => c.Add(It.IsAny<VehicleCreatedEvent>()), Times.Once);
            _outputPort.Verify(p => p.StandardHandle(It.IsAny<CreateVehicleOutput>()), Times.Once);

            VerifyNoOtherCalls();
        }

        /// <summary>
        /// Verifies that when unit of work save fails, event collection and output port operations are skipped.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test execution.</returns>
        [Fact]
        public async Task ExecuteWhenUnitOfWorkFailsDoesNotCallOutputPortOrEventCollector()
        {
            var manufactureDate = DateTime.Today.AddYears(-1);
            var input = new CreateVehicleInput("Toyota", "Corolla", "1234ABC", manufactureDate);

            _vehicleRepository
                .Setup(r => r.AddAsync(It.IsAny<Vehicle>(), CancellationToken.None))
                .Returns(Task.CompletedTask);

            _unitOfWork
                .Setup(u => u.Save(CancellationToken.None))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            var sut = CreateSut();

            var act = () => sut.Execute(input, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();

            _vehicleRepository.Verify(r => r.AddAsync(It.IsAny<Vehicle>(), CancellationToken.None), Times.Once);
            _unitOfWork.Verify(u => u.Save(CancellationToken.None), Times.Once);

            VerifyNoOtherCalls();
        }

        private CreateVehicleUseCase CreateSut() =>
            new(
                _vehicleRepository.Object,
                _unitOfWork.Object,
                _outputPort.Object,
                _logger.Object,
                _eventCollector.Object);

        private void VerifyNoOtherCalls()
        {
            _vehicleRepository.VerifyNoOtherCalls();
            _unitOfWork.VerifyNoOtherCalls();
            _outputPort.VerifyNoOtherCalls();
            _eventCollector.VerifyNoOtherCalls();
        }
    }
}
