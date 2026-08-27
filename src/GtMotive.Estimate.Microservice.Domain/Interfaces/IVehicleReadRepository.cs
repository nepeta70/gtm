using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GtMotive.Estimate.Microservice.Domain.Entities;

namespace GtMotive.Estimate.Microservice.Domain.Interfaces
{
    /// <summary>
    /// Query repository for the vehicle fleet. Used exclusively by queries.
    /// Returns projections, never full aggregates.
    /// </summary>
    public interface IVehicleReadRepository
    {
        /// <summary>
        /// Retrieves all vehicles that are currently available for rent.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The collection of available vehicles.</returns>
        Task<IReadOnlyCollection<VehicleReadModel>> GetAvailableAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Checks whether the given renter currently has an active (non-returned) rental.
        /// </summary>
        /// <param name="renterId">Renter identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the renter has an active rental; otherwise false.</returns>
        Task<bool> HasActiveRentalAsync(string renterId, CancellationToken cancellationToken);
    }
}
