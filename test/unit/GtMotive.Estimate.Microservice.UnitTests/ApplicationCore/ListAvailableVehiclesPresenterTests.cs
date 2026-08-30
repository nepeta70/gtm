using System;
using FluentAssertions;
using GtMotive.Estimate.Microservice.Api.UseCases.ListAvailableVehicles;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ListAvailableVehicles.Models;
using Xunit;

namespace GtMotive.Estimate.Microservice.UnitTests.ApplicationCore
{
    public class ListAvailableVehiclesPresenterTests
    {
        [Fact]
        public void StandardHandleSetsOkResult()
        {
            var presenter = new ListAvailableVehiclesPresenter();
            var output = new ListAvailableVehiclesOutput([]);

            presenter.StandardHandle(output);

            presenter.Result.Should().NotBeNull();
            presenter.Result.GetType().Name.Should().Contain("Ok");
        }

        [Fact]
        public void NotFoundHandleSetsNotFoundResult()
        {
            var presenter = new ListAvailableVehiclesPresenter();

            presenter.NotFoundHandle("nope");

            presenter.Result.Should().NotBeNull();
            presenter.Result.GetType().Name.Should().Contain("NotFound");
        }

        [Fact]
        public void StandardHandleNullThrowsArgumentNullException()
        {
            var presenter = new ListAvailableVehiclesPresenter();

            Action act = () => presenter.StandardHandle(null!);

            act.Should().Throw<ArgumentNullException>();
        }
    }
}
