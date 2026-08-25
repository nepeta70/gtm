namespace GtMotive.Estimate.Microservice.Domain.Entities
{
    /// <summary>
    /// Availability status of a vehicle in the fleet.
    /// </summary>
    public enum VehicleStatus
    {
        /// <summary>
        /// The vehicle is in the fleet and can be rented.
        /// </summary>
        Available = 0,

        /// <summary>
        /// The vehicle is currently rented by a customer.
        /// </summary>
        Rented = 1
    }
}
