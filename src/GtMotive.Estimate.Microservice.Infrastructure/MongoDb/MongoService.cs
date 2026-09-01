using GtMotive.Estimate.Microservice.Infrastructure.MongoDb.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace GtMotive.Estimate.Microservice.Infrastructure.MongoDb
{
    public class MongoService(IOptions<MongoDbSettings> options)
    {
        // Add call to RegisterBsonClasses() method.
        public MongoClient MongoClient { get; } = new MongoClient(options.Value.ConnectionString);
    }
}
