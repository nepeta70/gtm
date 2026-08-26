using System;

namespace GtMotive.Estimate.Microservice.Api.UseCases.CreateVehicle
{
    /// <summary>
    /// Request body to register a new vehicle in the fleet.
    /// </summary>
    public sealed record CreateVehicleRequest
    {
        public string Brand { get; set; }

        public string Model { get; set; }

        public string LicensePlate { get; set; }

        public DateTime ManufactureDate { get; set; }
    }
}
