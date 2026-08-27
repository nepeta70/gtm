using System;
using System.Threading.Tasks;
using GtMotive.Estimate.Microservice.Domain.Interfaces;
using MongoDB.Driver;

namespace GtMotive.Estimate.Microservice.Infrastructure.Persistence
{
    public sealed class MongoUnitOfWork(IMongoClient client) : IUnitOfWork, IDisposable
    {
        private IClientSessionHandle _session;

        public IClientSessionHandle Session =>
            _session ??= client.StartSession();

        public void Dispose() => _session?.Dispose();

        public Task<int> Save()
        {
            // In a real transaction scenario, you would call Session.CommitTransaction() here.
            // For this challenge, single-document atomicity is sufficient.
            return Task.FromResult(0);
        }
    }
}
