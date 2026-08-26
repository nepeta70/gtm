using System;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.CreateVehicle.Models;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.CreateVehicle.Ports;
using Microsoft.AspNetCore.Http;

namespace GtMotive.Estimate.Microservice.Api.UseCases.CreateVehicle
{
    public sealed class CreateVehiclePresenter : ICreateVehicleOutputPort, IWebApiPresenter
    {
        public IResult Result { get; private set; } = TypedResults.Problem("Unexpected error.");

        public void StandardHandle(CreateVehicleOutput response)
        {
            ArgumentNullException.ThrowIfNull(response);
            Result = TypedResults.Created($"/api/vehicles/{response.Id}", response.Id);
        }

        public void NotFoundHandle(string message)
        {
            Result = TypedResults.NotFound(message);
        }
    }
}
