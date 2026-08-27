using GtMotive.Estimate.Microservice.Infrastructure.MongoDb.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace GtMotive.Estimate.Microservice.Infrastructure.MongoDb
{
    public class MongoService(IOptions<MongoDbSettings> options)
    {
        public MongoClient MongoClient { get; } = new MongoClient(options.Value.ConnectionString);
    }
}
