using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using GtMotive.Estimate.Microservice.Domain.Entities;
using GtMotive.Estimate.Microservice.Domain.Interfaces;
using MongoDB.Driver;

namespace GtMotive.Estimate.Microservice.Infrastructure.Fleet.MongoDb
{
    public sealed class MongoVehicleRepository : IVehicleWriteRepository
    {
        private const string CollectionName = "vehicles";

        private readonly IMongoCollection<VehicleDocument> _collection;
        private readonly IMapper _mapper;

        public MongoVehicleRepository(IMongoDatabase database, IMapper mapper)
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentNullException.ThrowIfNull(mapper);

            _collection = database.GetCollection<VehicleDocument>(CollectionName);
            _mapper = mapper;
        }

        public async Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(vehicle);

            var document = _mapper.Map<VehicleDocument>(vehicle);
            await _collection
                .InsertOneAsync(document, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(vehicle);

            var document = _mapper.Map<VehicleDocument>(vehicle);
            await _collection
                .ReplaceOneAsync(d => d.Id == vehicle.Id, document, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<Vehicle> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var document = await _collection
                .Find(d => d.Id == id)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return document is null ? null : _mapper.Map<Vehicle>(document);
        }
    }
}
