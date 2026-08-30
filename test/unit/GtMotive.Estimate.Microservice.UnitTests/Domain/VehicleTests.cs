using System;
using FluentAssertions;
using GtMotive.Estimate.Microservice.Domain.Entities;
using GtMotive.Estimate.Microservice.Domain.Exceptions;
using Xunit;

namespace GtMotive.Estimate.Microservice.UnitTests.Domain
{
    /// <summary>
    /// Pure unit tests for the <see cref="Vehicle"/> aggregate: no mocks, no infrastructure, no dependencies.
    /// </summary>
    public class VehicleTests
    {
        /// <summary>
        /// Verifies that creating a vehicle with valid data sets its initial status to available.
        /// </summary>
        [Fact]
        public void ConstructorWithValidDataCreatesAnAvailableVehicle()
        {
            var manufactureDate = DateTime.UtcNow.AddYears(-1);

            var vehicle = new Vehicle(Guid.NewGuid(), "Toyota", "Corolla", "1234ABC", manufactureDate);

            vehicle.Status.Should().Be(VehicleStatus.Available);
            vehicle.RenterId.Should().BeNull();
            vehicle.RentedAt.Should().BeNull();
        }

        /// <summary>
        /// Verifies that constructing a vehicle older than five years throws a <see cref="DomainException"/>.
        /// </summary>
        [Fact]
        public void ConstructorWhenManufactureDateIsOlderThanFiveYearsThrowsDomainException()
        {
            var manufactureDate = DateTime.UtcNow.AddYears(-6);

            var act = () => new Vehicle(Guid.NewGuid(), "Toyota", "Corolla", "1234ABC", manufactureDate);

            act.Should().Throw<DomainException>();
        }

        /// <summary>
        /// Verifies that constructing a vehicle with a future manufacture date throws a <see cref="DomainException"/>.
        /// </summary>
        [Fact]
        public void ConstructorWhenManufactureDateIsInTheFutureThrowsDomainException()
        {
            var manufactureDate = DateTime.UtcNow.AddDays(1);

            var act = () => new Vehicle(Guid.NewGuid(), "Toyota", "Corolla", "1234ABC", manufactureDate);

            act.Should().Throw<DomainException>();
        }

        /// <summary>
        /// Verifies that constructing a vehicle with a missing or empty brand throws a <see cref="DomainException"/>.
        /// </summary>
        /// <param name="brand">The invalid brand string test case.</param>
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void ConstructorWhenBrandIsMissingThrowsDomainException(string brand)
        {
            var act = () => new Vehicle(Guid.NewGuid(), brand, "Corolla", "1234ABC", DateTime.UtcNow.AddYears(-1));

            act.Should().Throw<DomainException>();
        }

        /// <summary>
        /// Verifies that renting an available vehicle transitions its status to rented and assigns the renter ID.
        /// </summary>
        [Fact]
        public void RentWhenVehicleIsAvailableMarksItAsRented()
        {
            var vehicle = new Vehicle(Guid.NewGuid(), "Toyota", "Corolla", "1234ABC", DateTime.UtcNow.AddYears(-1));

            vehicle.Rent("renter-1");

            vehicle.Status.Should().Be(VehicleStatus.Rented);
            vehicle.RenterId.Should().Be("renter-1");
            vehicle.RentedAt.Should().NotBeNull();
        }

        /// <summary>
        /// Verifies that attempting to rent an already rented vehicle throws a <see cref="DomainException"/>.
        /// </summary>
        [Fact]
        public void RentWhenVehicleIsAlreadyRentedThrowsDomainException()
        {
            var vehicle = new Vehicle(Guid.NewGuid(), "Toyota", "Corolla", "1234ABC", DateTime.UtcNow.AddYears(-1));
            vehicle.Rent("renter-1");

            var act = () => vehicle.Rent("renter-2");

            act.Should().Throw<DomainException>();
        }

        /// <summary>
        /// Verifies that returning a rented vehicle transitions its status back to available and clears rental data.
        /// </summary>
        [Fact]
        public void ReturnWhenVehicleIsRentedMakesItAvailableAgain()
        {
            var vehicle = new Vehicle(Guid.NewGuid(), "Toyota", "Corolla", "1234ABC", DateTime.UtcNow.AddYears(-1));
            vehicle.Rent("renter-1");

            vehicle.Return();

            vehicle.Status.Should().Be(VehicleStatus.Available);
            vehicle.RenterId.Should().BeNull();
            vehicle.RentedAt.Should().BeNull();
        }

        /// <summary>
        /// Verifies that attempting to return a vehicle that is already available throws a <see cref="DomainException"/>.
        /// </summary>
        [Fact]
        public void ReturnWhenVehicleIsAlreadyAvailableThrowsDomainException()
        {
            var vehicle = new Vehicle(Guid.NewGuid(), "Toyota", "Corolla", "1234ABC", DateTime.UtcNow.AddYears(-1));

            var act = vehicle.Return;

            act.Should().Throw<DomainException>();
        }
    }
}
