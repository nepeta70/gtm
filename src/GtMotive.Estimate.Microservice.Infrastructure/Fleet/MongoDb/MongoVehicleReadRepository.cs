using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GtMotive.Estimate.Microservice.Domain.Entities;
using GtMotive.Estimate.Microservice.Domain.Interfaces;
using GtMotive.Estimate.Microservice.Infrastructure.MongoDb;
using GtMotive.Estimate.Microservice.Infrastructure.Resilience;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Polly;

namespace GtMotive.Estimate.Microservice.Infrastructure.Fleet.MongoDb
{
    /// <summary>
    /// MongoDB implementation of <see cref="IVehicleReadRepository"/>.
    /// Applies the shared MongoDB resilience pipeline to every query.
    /// </summary>
    public sealed class MongoVehicleReadRepository(
        MongoService mongoService,
        IAppLogger<MongoVehicleReadRepository> logger,
        [FromKeyedServices(ResiliencePipelineNames.Mongo)] ResiliencePipeline mongoPipeline) : IVehicleReadRepository
    {
        private readonly IMongoCollection<VehicleDocument> _collection = mongoService.Database.GetCollection<VehicleDocument>("vehicles");

        public async Task<IReadOnlyCollection<VehicleReadModel>> GetAvailableAsync(CancellationToken cancellationToken)
        {
            var minimumDate = DateTime.Today.AddYears(-Vehicle.MaxManufactureAgeInYears);

            logger.LogInformation("Retrieving available vehicles manufactured on or after {MinimumDate}", minimumDate);

            var cursor = await mongoPipeline
                .ExecuteAsync(
                    async ct => await _collection
                        .Find(d => d.Status == VehicleStatus.Available &&
                                   d.ManufactureDate >= minimumDate)
                        .Project(d => new VehicleReadModel(
                            d.Id,
                            d.Brand,
                            d.Model,
                            d.LicensePlate,
                            d.ManufactureDate))
                        .ToListAsync(ct),
                    cancellationToken);

            logger.LogInformation("Found {Count} available vehicles", cursor.Count);

            return cursor;
        }

        public async Task<bool> HasActiveRentalAsync(string renterId, CancellationToken cancellationToken)
        {
            logger.LogInformation("Checking for active rentals for renter {RenterId}", renterId);

            var count = await mongoPipeline
                .ExecuteAsync(
                    async ct => await _collection
                        .Find(d => d.Status == VehicleStatus.Rented && d.RenterId == renterId)
                        .CountDocumentsAsync(ct),
                    cancellationToken);

            var hasActiveRental = count > 0;

            logger.LogInformation("Renter {RenterId} active rental check returned {HasActiveRental}", renterId, hasActiveRental);

            return hasActiveRental;
        }
    }
}
