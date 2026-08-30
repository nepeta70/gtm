using System;
using FluentAssertions;
using GtMotive.Estimate.Microservice.Api.UseCases.RentVehicle;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.RentVehicle.Models;
using Xunit;

namespace GtMotive.Estimate.Microservice.UnitTests.ApplicationCore
{
    public class RentVehiclePresenterTests
    {
        [Fact]
        public void StandardHandleSetsOkResult()
        {
            var presenter = new RentVehiclePresenter();
            var output = new RentVehicleOutput(Guid.NewGuid(), "renter-1", DateTime.UtcNow);

            presenter.StandardHandle(output);

            presenter.Result.Should().NotBeNull();
            presenter.Result.GetType().Name.Should().Contain("Ok");
        }

        [Fact]
        public void NotFoundHandleSetsNotFound()
        {
            var presenter = new RentVehiclePresenter();

            presenter.NotFoundHandle("not-found");

            presenter.Result.Should().NotBeNull();
            presenter.Result.GetType().Name.Should().Contain("NotFound");
        }
    }
}
