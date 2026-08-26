using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ReturnVehicle.Models;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ReturnVehicle.Ports;
using Microsoft.AspNetCore.Http;

namespace GtMotive.Estimate.Microservice.Api.UseCases.Vehicles
{
    public sealed class ReturnVehiclePresenter : IReturnVehicleOutputPort, IWebApiPresenter
    {
        public IResult Result { get; private set; }

        public void StandardHandle(ReturnVehicleOutput response)
        {
            Result = TypedResults.Ok(response);
        }

        public void NotFoundHandle(string message)
        {
            Result = TypedResults.NotFound(message);
        }
    }
}
