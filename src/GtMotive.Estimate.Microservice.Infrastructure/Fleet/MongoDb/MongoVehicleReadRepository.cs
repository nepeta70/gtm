using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GtMotive.Estimate.Microservice.Domain.Entities;
using GtMotive.Estimate.Microservice.Domain.Interfaces;
using MongoDB.Driver;

namespace GtMotive.Estimate.Microservice.Infrastructure.Fleet.MongoDb
{
    /// <summary>
    /// MongoDB implementation of the <see cref="IVehicleReadRepository"/> interface.
    /// </summary>
    /// <param name="database">The MongoDB database instance.</param>
    public sealed class MongoVehicleReadRepository(IMongoDatabase database) : IVehicleReadRepository
    {
        private readonly IMongoCollection<VehicleDocument> _collection = database.GetCollection<VehicleDocument>("vehicles");

        public async Task<IReadOnlyCollection<VehicleReadModel>> GetAvailableAsync(CancellationToken cancellationToken)
        {
            var minimumDate = DateTime.UtcNow.Date.AddYears(-Vehicle.MaxManufactureAgeInYears);

            var cursor = await _collection
                .Find(d => d.Status == VehicleStatus.Available &&
                           d.ManufactureDate >= minimumDate)
                .Project(d => new VehicleReadModel(
                    d.Id,
                    d.Brand,
                    d.Model,
                    d.LicensePlate,
                    d.ManufactureDate))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return cursor;
        }

        public async Task<bool> HasActiveRentalAsync(string renterId, CancellationToken cancellationToken)
        {
            var count = await _collection
                .Find(d => d.Status == VehicleStatus.Rented && d.RenterId == renterId)
                .CountDocumentsAsync(cancellationToken)
                .ConfigureAwait(false);

            return count > 0;
        }
    }
}
