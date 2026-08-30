using System;
using FluentAssertions;
using GtMotive.Estimate.Microservice.Api.UseCases.ReturnVehicle;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ReturnVehicle.Models;
using Xunit;

namespace GtMotive.Estimate.Microservice.UnitTests.ApplicationCore
{
    public class ReturnVehiclePresenterTests
    {
        [Fact]
        public void StandardHandleSetsOkResult()
        {
            var presenter = new ReturnVehiclePresenter();
            var output = new ReturnVehicleOutput(Guid.NewGuid());

            presenter.StandardHandle(output);

            presenter.Result.Should().NotBeNull();
            presenter.Result.GetType().Name.Should().Contain("Ok");
        }

        [Fact]
        public void NotFoundHandleSetsNotFound()
        {
            var presenter = new ReturnVehiclePresenter();

            presenter.NotFoundHandle("not-found");

            presenter.Result.Should().NotBeNull();
            presenter.Result.GetType().Name.Should().Contain("NotFound");
        }
    }
}
