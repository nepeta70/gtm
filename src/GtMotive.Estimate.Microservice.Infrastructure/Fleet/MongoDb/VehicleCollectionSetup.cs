using System;
using System.Collections.Generic;
using MongoDB.Driver;
using Polly;

namespace GtMotive.Estimate.Microservice.Infrastructure.Fleet.MongoDb
{
    public static class VehicleCollectionSetup
    {
        public static void EnsureIndexes(IMongoDatabase database, ResiliencePipeline resiliencePipeline)
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentNullException.ThrowIfNull(resiliencePipeline);

            var collection = database.GetCollection<VehicleDocument>("vehicles");

            var indexModels = new List<CreateIndexModel<VehicleDocument>>
            {
                // 1. LicensePlate must be unique across the fleet.
                new(
                    Builders<VehicleDocument>.IndexKeys.Ascending(d => d.LicensePlate),
                    new CreateIndexOptions
                    {
                        Unique = true,
                        Name = "licensePlate_unique"
                    }),

                // 2. RenterId must be unique WHEN it exists (i.e. when the vehicle is rented).
                //    Sparse = true skips documents where the field is missing entirely.
                //    Combined with [BsonIgnoreIfNull], null values are omitted from the index.
                new(
                    Builders<VehicleDocument>.IndexKeys.Ascending(d => d.RenterId),
                    new CreateIndexOptions
                    {
                        Unique = true,
                        Sparse = true,
                        Name = "renterId_unique_sparse"
                    })
            };

            // Runs at startup, when MongoDB (e.g. a fresh Docker container) may not be
            // accepting connections yet, so it goes through the shared retry pipeline.
            resiliencePipeline.Execute(() => collection.Indexes.CreateMany(indexModels));
        }
    }
}
