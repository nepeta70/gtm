using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GtMotive.Estimate.Microservice.Domain.Entities;
using GtMotive.Estimate.Microservice.Domain.Exceptions;
using GtMotive.Estimate.Microservice.Domain.Interfaces;
using GtMotive.Estimate.Microservice.FunctionalTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GtMotive.Estimate.Microservice.FunctionalTests.Specs
{
    [Collection(TestCollections.Functional)]
    public sealed class MongoVehicleRepositoryTests(CompositionRootTestFixture fixture) : FunctionalTestBase(fixture)
    {
        [Fact]
        public async Task AddAndGetAvailableIncludesRecentlyAddedVehicle()
        {
            await Fixture.UsingScope(async sp =>
            {
                var writeRepo = sp.GetRequiredService<IVehicleWriteRepository>();
                var readRepo = sp.GetRequiredService<IVehicleReadRepository>();

                var vehicle = new Vehicle(Guid.NewGuid(), "Toyota", "Corolla", "PLT-123", DateTime.Today.AddYears(-1));
                await writeRepo.AddAsync(vehicle, CancellationToken.None);

                var available = await readRepo.GetAvailableAsync(CancellationToken.None);
                available.Should().Contain(v => v.LicensePlate == "PLT-123");
            });
        }

        [Fact]
        public async Task RentedVehicleIsNotReturnedByGetAvailable()
        {
            await Fixture.UsingScope(async sp =>
            {
                var writeRepo = sp.GetRequiredService<IVehicleWriteRepository>();
                var readRepo = sp.GetRequiredService<IVehicleReadRepository>();

                var vehicle = new Vehicle(Guid.NewGuid(), "Toyota", "Corolla", "VALID-001", DateTime.Today.AddYears(-1));

                vehicle.Rent("renter-xyz");

                await writeRepo.AddAsync(vehicle, CancellationToken.None);

                var available = await readRepo.GetAvailableAsync(CancellationToken.None);
                available.Should().NotContain(v => v.LicensePlate == "VALID-001");
            });
        }

        [Fact]
        public async Task HasActiveRentalReturnsTrueAfterRent()
        {
            await Fixture.UsingScope(async sp =>
            {
                var writeRepo = sp.GetRequiredService<IVehicleWriteRepository>();
                var readRepo = sp.GetRequiredService<IVehicleReadRepository>();

                var vehicle = new Vehicle(Guid.NewGuid(), "Toyota", "Corolla", "9293-HGT", DateTime.Today.AddYears(-1));
                await writeRepo.AddAsync(vehicle, CancellationToken.None);

                // Rent the vehicle using domain operation and persist
                vehicle.Rent("renter-abc");
                await writeRepo.UpdateAsync(vehicle, CancellationToken.None);

                var hasActive = await readRepo.HasActiveRentalAsync("renter-abc", CancellationToken.None);
                hasActive.Should().BeTrue();
            });
        }

        [Fact]
        public async Task AddVehicleWithExistingLicensePlateThrowsConflictException()
        {
            await Fixture.UsingScope(async sp =>
            {
                var writeRepo = sp.GetRequiredService<IVehicleWriteRepository>();

                var licensePlate = "DUP-001";
                var vehicle1 = new Vehicle(Guid.NewGuid(), "Toyota", "Corolla", licensePlate, DateTime.Today.AddYears(-1));
                await writeRepo.AddAsync(vehicle1, CancellationToken.None);

                var vehicle2 = new Vehicle(Guid.NewGuid(), "Honda", "Civic", licensePlate, DateTime.Today.AddYears(-1));

                Func<Task> act = async () => await writeRepo.AddAsync(vehicle2, CancellationToken.None);

                await act.Should().ThrowAsync<ConflictException>()
                    .WithMessage($"A vehicle with license plate '{vehicle2.LicensePlate}' already exists.");
            });
        }
    }
}
