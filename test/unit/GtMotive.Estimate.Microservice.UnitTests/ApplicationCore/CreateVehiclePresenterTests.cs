using System;
using FluentAssertions;
using GtMotive.Estimate.Microservice.Api.UseCases.CreateVehicle;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.CreateVehicle.Models;
using Xunit;

namespace GtMotive.Estimate.Microservice.UnitTests.ApplicationCore
{
    public class CreateVehiclePresenterTests
    {
        [Fact]
        public void StandardHandleSetsCreatedResult()
        {
            var presenter = new CreateVehiclePresenter();
            var output = new CreateVehicleOutput(Guid.NewGuid(), "Toyota", "Corolla", "1234-ABC", DateTime.Today.AddYears(-1));

            presenter.StandardHandle(output);

            presenter.Result.Should().NotBeNull();
            presenter.Result.GetType().Name.Should().Contain("Created");
        }

        [Fact]
        public void NotFoundHandleSetsNotFoundResult()
        {
            var presenter = new CreateVehiclePresenter();

            presenter.NotFoundHandle("not-found");

            presenter.Result.Should().NotBeNull();
            presenter.Result.GetType().Name.Should().Contain("NotFound");
        }

        [Fact]
        public void StandardHandleNullThrowsArgumentNullException()
        {
            var presenter = new CreateVehiclePresenter();

            Action act = () => presenter.StandardHandle(null!);

            act.Should().Throw<ArgumentNullException>();
        }
    }
}
