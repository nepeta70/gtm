using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GtMotive.Estimate.Microservice.Domain.Entities;
using GtMotive.Estimate.Microservice.Domain.Interfaces;
using MongoDB.Driver;

namespace GtMotive.Estimate.Microservice.Infrastructure.Fleet.MongoDb
{
    public sealed class MongoVehicleReadRepository(IMongoDatabase database, IAppLogger<MongoVehicleReadRepository> logger) : IVehicleReadRepository
    {
        private readonly IMongoCollection<VehicleDocument> _collection = database.GetCollection<VehicleDocument>("vehicles");

        public async Task<IReadOnlyCollection<VehicleReadModel>> GetAvailableAsync(CancellationToken cancellationToken)
        {
            var minimumDate = DateTime.UtcNow.Date.AddYears(-Vehicle.MaxManufactureAgeInYears);

            logger.LogInformation("Retrieving available vehicles manufactured on or after {MinimumDate}", minimumDate);

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

            logger.LogInformation("Found {Count} available vehicles", cursor.Count);

            return cursor;
        }

        public async Task<bool> HasActiveRentalAsync(string renterId, CancellationToken cancellationToken)
        {
            logger.LogInformation("Checking for active rentals for renter {RenterId}", renterId);

            var count = await _collection
                .Find(d => d.Status == VehicleStatus.Rented && d.RenterId == renterId)
                .CountDocumentsAsync(cancellationToken)
                .ConfigureAwait(false);

            var hasActiveRental = count > 0;

            logger.LogInformation("Renter {RenterId} active rental check returned {HasActiveRental}", renterId, hasActiveRental);

            return hasActiveRental;
        }
    }
}
