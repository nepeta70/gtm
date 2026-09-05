using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using GtMotive.Estimate.Microservice.Api.UseCases.CreateVehicle;
using GtMotive.Estimate.Microservice.Api.UseCases.ListAvailableVehicles;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.CreateVehicle.Models;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ListAvailableVehicles.Models;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.RentVehicle.Models;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ReturnVehicle.Models;
using GtMotive.Estimate.Microservice.Domain.Exceptions;
using GtMotive.Estimate.Microservice.FunctionalTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GtMotive.Estimate.Microservice.FunctionalTests.Specs
{
    [Collection(TestCollections.Functional)]
    public sealed class VehicleFleetFunctionalTests(CompositionRootTestFixture fixture) : FunctionalTestBase(fixture)
    {
        [Fact]
        public async Task CreatingAVehicleMakesItAvailableForRent()
        {
            await Fixture.UsingScope(async sp =>
            {
                var createUseCase = sp.GetRequiredService<IUseCase<CreateVehicleInput>>();
                await createUseCase.Execute(new CreateVehicleInput("Toyota", "Corolla", "1234-ABC", DateTime.UtcNow.AddYears(-1)));

                var listUseCase = sp.GetRequiredService<IUseCase<ListAvailableVehiclesInput>>();
                var listPresenter = sp.GetRequiredService<ListAvailableVehiclesPresenter>();
                await listUseCase.Execute(new ListAvailableVehiclesInput());

                var vehicles = GetOkValue<IEnumerable<AvailableVehicleDto>>(listPresenter.Result);
                vehicles.Should().Contain(v => v.LicensePlate == "1234-ABC");
            });
        }

        [Fact]
        public async Task SamePersonCannotRentMoreThanOneVehicleAtTheSameTime()
        {
            await Fixture.UsingScope(async sp =>
            {
                var createUseCase = sp.GetRequiredService<IUseCase<CreateVehicleInput>>();
                var createPresenter = sp.GetRequiredService<CreateVehiclePresenter>();

                await createUseCase.Execute(new CreateVehicleInput("Toyota", "Corolla", "AAA-111", DateTime.UtcNow.AddYears(-1)));
                var firstVehicleId = GetOkValue<Guid>(createPresenter.Result);

                await createUseCase.Execute(new CreateVehicleInput("Seat", "Leon", "BBB-222", DateTime.UtcNow.AddYears(-1)));
                var secondVehicleId = GetOkValue<Guid>(createPresenter.Result);

                var rentUseCase = sp.GetRequiredService<IUseCase<RentVehicleInput>>();

                await rentUseCase.Execute(new RentVehicleInput(firstVehicleId, "renter-1"));

                Func<Task> rentingSecondVehicle = () => rentUseCase.Execute(new RentVehicleInput(secondVehicleId, "renter-1"));

                await rentingSecondVehicle.Should().ThrowAsync<DomainException>();
            });
        }

        [Fact]
        public async Task ReturningAVehicleMakesItAvailableAgain()
        {
            await Fixture.UsingScope(async sp =>
            {
                var createUseCase = sp.GetRequiredService<IUseCase<CreateVehicleInput>>();
                var createPresenter = sp.GetRequiredService<CreateVehiclePresenter>();
                await createUseCase.Execute(new CreateVehicleInput("Toyota", "Corolla", "CCC-333", DateTime.UtcNow.AddYears(-1)));
                var vehicleId = GetOkValue<Guid>(createPresenter.Result);

                var rentUseCase = sp.GetRequiredService<IUseCase<RentVehicleInput>>();
                await rentUseCase.Execute(new RentVehicleInput(vehicleId, "renter-2"));

                var returnUseCase = sp.GetRequiredService<IUseCase<ReturnVehicleInput>>();
                await returnUseCase.Execute(new ReturnVehicleInput(vehicleId, "renter-2"));

                var listUseCase = sp.GetRequiredService<IUseCase<ListAvailableVehiclesInput>>();
                var listPresenter = sp.GetRequiredService<ListAvailableVehiclesPresenter>();
                await listUseCase.Execute(new ListAvailableVehiclesInput());

                var vehicles = GetOkValue<IEnumerable<AvailableVehicleDto>>(listPresenter.Result);
                vehicles.Should().Contain(v => v.Id == vehicleId);
            });
        }

        [Fact]
        public async Task DifferentPersonCannotReturnVehicleRentedByAnotherRenter()
        {
            await Fixture.UsingScope(async sp =>
            {
                var createUseCase = sp.GetRequiredService<IUseCase<CreateVehicleInput>>();
                var createPresenter = sp.GetRequiredService<CreateVehiclePresenter>();
                await createUseCase.Execute(new CreateVehicleInput("Toyota", "Corolla", "DDD-444", DateTime.UtcNow.AddYears(-1)));
                var vehicleId = GetOkValue<Guid>(createPresenter.Result);

                var rentUseCase = sp.GetRequiredService<IUseCase<RentVehicleInput>>();
                await rentUseCase.Execute(new RentVehicleInput(vehicleId, "renter-original"));

                var returnUseCase = sp.GetRequiredService<IUseCase<ReturnVehicleInput>>();
                Func<Task> returnByDifferentRenter = () => returnUseCase.Execute(new ReturnVehicleInput(vehicleId, "renter-different"));

                await returnByDifferentRenter.Should().ThrowAsync<DomainException>();
            });
        }

        private static T GetOkValue<T>(IResult actionResult)
        {
            if (actionResult is ObjectResult objectResult)
            {
                return (T)objectResult.Value;
            }

            var valueProperty = actionResult.GetType().GetProperty("Value");
            return valueProperty is not null
                ? (T)valueProperty.GetValue(actionResult)
                : throw new InvalidOperationException($"Cannot extract value from result type '{actionResult.GetType().FullName}'.");
        }
    }
}
