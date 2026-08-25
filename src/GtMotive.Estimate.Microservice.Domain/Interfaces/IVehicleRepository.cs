using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GtMotive.Estimate.Microservice.Domain.Entities;

namespace GtMotive.Estimate.Microservice.Domain.Interfaces
{
    /// <summary>
    /// Persistence abstraction for the vehicle fleet.
    /// </summary>
    public interface IVehicleRepository
    {
        /// <summary>
        /// Adds a new vehicle to the fleet.
        /// </summary>
        /// <param name="vehicle">Vehicle to add.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing vehicle.
        /// </summary>
        /// <param name="vehicle">Vehicle to update.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a vehicle by its identifier.
        /// </summary>
        /// <param name="id">Vehicle identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The vehicle, or null if it does not exist.</returns>
        Task<Vehicle> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all vehicles that are currently available for rent.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The collection of available vehicles.</returns>
        Task<IReadOnlyCollection<Vehicle>> GetAvailableAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks whether the given renter currently has an active (non-returned) rental.
        /// </summary>
        /// <param name="renterId">Renter identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the renter has an active rental; otherwise false.</returns>
        Task<bool> HasActiveRentalAsync(string renterId, CancellationToken cancellationToken = default);
    }
}
