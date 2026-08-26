using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.RentVehicle.Models;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.RentVehicle.Ports;
using Microsoft.AspNetCore.Http;

namespace GtMotive.Estimate.Microservice.Api.UseCases.RentVehicle
{
    public sealed class RentVehiclePresenter : IRentVehicleOutputPort, IWebApiPresenter
    {
        public IResult Result { get; private set; }

        public void StandardHandle(RentVehicleOutput response)
        {
            Result = TypedResults.Ok(response);
        }

        public void NotFoundHandle(string message)
        {
            Result = TypedResults.NotFound(message);
        }
    }
}
