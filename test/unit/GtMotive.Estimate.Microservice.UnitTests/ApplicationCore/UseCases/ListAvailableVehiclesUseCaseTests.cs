using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ListAvailableVehicles;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ListAvailableVehicles.Models;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ListAvailableVehicles.Ports;
using GtMotive.Estimate.Microservice.Domain.Entities;
using GtMotive.Estimate.Microservice.Domain.Interfaces;
using Moq;
using Xunit;

namespace GtMotive.Estimate.Microservice.UnitTests.ApplicationCore.UseCases
{
    /// <summary>
    /// Unit tests for <see cref="ListAvailableVehiclesUseCase"/>.
    /// </summary>
    public sealed class ListAvailableVehiclesUseCaseTests
    {
        private readonly Mock<IVehicleRepository> _vehicleRepository = new(MockBehavior.Strict);
        private readonly Mock<IListAvailableVehiclesOutputPort> _outputPort = new(MockBehavior.Strict);

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
        /// Verifies that executing the use case when no available vehicles exist returns an empty list through the output port.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test execution.</returns>
        [Fact]
        public async Task ExecuteWhenNoAvailableVehiclesExistPresentsEmptyOutput()
        {
            var input = new ListAvailableVehiclesInput();

            _vehicleRepository
                .Setup(r => r.GetAvailableAsync(CancellationToken.None))
                .ReturnsAsync([]);

            _outputPort
                .Setup(p => p.StandardHandle(It.Is<ListAvailableVehiclesOutput>(o => o.Vehicles.Count == 0)))
                .Verifiable();

            var sut = CreateSut();

            await sut.Execute(input, CancellationToken.None);

            _vehicleRepository.Verify(r => r.GetAvailableAsync(CancellationToken.None), Times.Once);
            _outputPort.Verify(p => p.StandardHandle(It.IsAny<ListAvailableVehiclesOutput>()), Times.Once);

            VerifyNoOtherCalls();
        }

        /// <summary>
        /// Verifies that executing the use case with available vehicles retrieves the domain entities, maps them to DTOs, and presents the output.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test execution.</returns>
        [Fact]
        public async Task ExecuteWhenAvailableVehiclesExistMapsAndPresentsOutput()
        {
            var input = new ListAvailableVehiclesInput();
            var vehicles = new List<Vehicle>
            {
                new(Guid.NewGuid(), "Toyota", "Corolla", "1234ABC", DateTime.UtcNow.AddYears(-1)),
                new(Guid.NewGuid(), "Seat", "Leon", "5678DEF", DateTime.UtcNow.AddYears(-2))
            };

            _vehicleRepository
                .Setup(r => r.GetAvailableAsync(CancellationToken.None))
                .ReturnsAsync(vehicles);

            _outputPort
                .Setup(p => p.StandardHandle(It.Is<ListAvailableVehiclesOutput>(o =>
                    o.Vehicles.Count == 2 &&
                    o.Vehicles.Any(v => v.Id == vehicles[0].Id && v.Brand == vehicles[0].Brand && v.Model == vehicles[0].Model && v.LicensePlate == vehicles[0].LicensePlate && v.ManufactureDate == vehicles[0].ManufactureDate) &&
                    o.Vehicles.Any(v => v.Id == vehicles[1].Id && v.Brand == vehicles[1].Brand && v.Model == vehicles[1].Model && v.LicensePlate == vehicles[1].LicensePlate && v.ManufactureDate == vehicles[1].ManufactureDate))))
                .Verifiable();

            var sut = CreateSut();

            await sut.Execute(input, CancellationToken.None);

            _vehicleRepository.Verify(r => r.GetAvailableAsync(CancellationToken.None), Times.Once);
            _outputPort.Verify(p => p.StandardHandle(It.IsAny<ListAvailableVehiclesOutput>()), Times.Once);

            VerifyNoOtherCalls();
        }

        private ListAvailableVehiclesUseCase CreateSut() =>
            new(
                _vehicleRepository.Object,
                _outputPort.Object);

        private void VerifyNoOtherCalls()
        {
            _vehicleRepository.VerifyNoOtherCalls();
            _outputPort.VerifyNoOtherCalls();
        }
    }
}
