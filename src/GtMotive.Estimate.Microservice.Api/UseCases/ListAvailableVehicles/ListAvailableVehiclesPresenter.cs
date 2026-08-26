using System;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ListAvailableVehicles.Models;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ListAvailableVehicles.Ports;
using Microsoft.AspNetCore.Http;

namespace GtMotive.Estimate.Microservice.Api.UseCases.ListAvailableVehicles
{
    public sealed class ListAvailableVehiclesPresenter : IListAvailableVehiclesOutputPort, IWebApiPresenter
    {
        public IResult Result { get; private set; }

        public void StandardHandle(ListAvailableVehiclesOutput response)
        {
            ArgumentNullException.ThrowIfNull(response);
            Result = TypedResults.Ok(response.Vehicles);
        }

        public void NotFoundHandle(string message)
        {
            Result = TypedResults.NotFound(message);
        }
    }
}
