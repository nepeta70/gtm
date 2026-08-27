using System;
using MongoDB.Driver;

namespace GtMotive.Estimate.Microservice.Infrastructure.Fleet.MongoDb
{
    public static class VehicleCollectionSetup
    {
        public static void EnsureIndexes(IMongoDatabase database)
        {
            ArgumentNullException.ThrowIfNull(database);
            var collection = database.GetCollection<VehicleDocument>("vehicles");

            // 1. LicensePlate must be unique across the fleet.
            var licensePlateIndex = new CreateIndexModel<VehicleDocument>(
                Builders<VehicleDocument>.IndexKeys.Ascending(d => d.LicensePlate),
                new CreateIndexOptions
                {
                    Unique = true,
                    Name = "licensePlate_unique"
                });

            // 2. RenterId must be unique WHEN it exists (i.e. when the vehicle is rented).
            //    Sparse = true skips documents where the field is missing entirely.
            //    Combined with [BsonIgnoreIfNull], null values are omitted from the index.
            var renterIdIndex = new CreateIndexModel<VehicleDocument>(
                Builders<VehicleDocument>.IndexKeys.Ascending(d => d.RenterId),
                new CreateIndexOptions
                {
                    Unique = true,
                    Sparse = true,
                    Name = "renterId_unique_sparse"
                });

            collection.Indexes.CreateOne(licensePlateIndex);
            collection.Indexes.CreateOne(renterIdIndex);
        }
    }
}
