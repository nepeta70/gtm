namespace GtMotive.Estimate.Microservice.Api.UseCases.RentVehicle
{
    /// <summary>
    /// Request body to rent a vehicle.
    /// </summary>
    public sealed class RentVehicleRequest
    {
        public string RenterId { get; set; }
    }
}
