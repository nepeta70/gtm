using System;
using GtMotive.Estimate.Microservice.Infrastructure.Fleet.MongoDb;
using GtMotive.Estimate.Microservice.Infrastructure.MongoDb.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace GtMotive.Estimate.Microservice.Infrastructure.MongoDb
{
    public class MongoService
    {
        public MongoService(IOptions<MongoDbSettings> options)
        {
            ArgumentNullException.ThrowIfNull(options);

            var settings = options.Value;

            MongoClient = new MongoClient(settings.ConnectionString);
            Database = MongoClient.GetDatabase(settings.MongoDbDatabaseName);

            RegisterBsonClasses();
        }

        public IMongoClient MongoClient { get; }

        public IMongoDatabase Database { get; }

        private static void RegisterBsonClasses()
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(VehicleDocument)))
            {
                BsonClassMap.RegisterClassMap<VehicleDocument>(map =>
                {
                    map.AutoMap();
                    map.SetIgnoreExtraElements(true);
                });
            }
        }
    }
}
