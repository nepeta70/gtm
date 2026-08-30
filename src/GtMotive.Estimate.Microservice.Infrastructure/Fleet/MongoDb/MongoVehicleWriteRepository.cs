using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using GtMotive.Estimate.Microservice.Domain.Entities;
using GtMotive.Estimate.Microservice.Domain.Exceptions;
using GtMotive.Estimate.Microservice.Domain.Interfaces;
using GtMotive.Estimate.Microservice.Infrastructure.Persistence;
using MongoDB.Driver;

namespace GtMotive.Estimate.Microservice.Infrastructure.Fleet.MongoDb
{
    /// <summary>
    /// MongoDB implementation of <see cref="IVehicleWriteRepository"/>.
    /// Participates in the active <see cref="IUnitOfWork"/> session when a transaction is available.
    /// </summary>
    public sealed class MongoVehicleWriteRepository : IVehicleWriteRepository
    {
        private const string CollectionName = "vehicles";

        private readonly IMongoCollection<VehicleDocument> _collection;
        private readonly IMapper _mapper;
        private readonly IAppLogger<MongoVehicleWriteRepository> _logger;
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoVehicleWriteRepository"/> class.
        /// </summary>
        /// <param name="database">The MongoDB database.</param>
        /// <param name="mapper">The AutoMapper instance.</param>
        /// <param name="logger">The application logger.</param>
        /// <param name="unitOfWork">The current unit of work.</param>
        public MongoVehicleWriteRepository(
            IMongoDatabase database,
            IMapper mapper,
            IAppLogger<MongoVehicleWriteRepository> logger,
            IUnitOfWork unitOfWork)
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(unitOfWork);

            _collection = database.GetCollection<VehicleDocument>(CollectionName);
            _mapper = mapper;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        /// <inheritdoc />
        public async Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(vehicle);

            _logger.LogInformation("Adding new vehicle with ID {VehicleId}", vehicle.Id);
            var document = _mapper.Map<VehicleDocument>(vehicle);
            var session = GetSession();

            try
            {
                if (session is not null && session.IsInTransaction)
                {
                    await _collection
                        .InsertOneAsync(session, document, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    await _collection
                        .InsertOneAsync(document, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Code == 11000)
            {
                _logger.LogError(ex, "A vehicle with license plate '{LicensePlate}' already exists.", vehicle.LicensePlate);
                throw new ConflictException($"A vehicle with license plate '{vehicle.LicensePlate}' already exists.", ex);
            }

            _logger.LogInformation("Successfully added vehicle with ID {VehicleId}", vehicle.Id);
        }

        /// <inheritdoc />
        public async Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(vehicle);

            _logger.LogInformation("Updating vehicle with ID {VehicleId}", vehicle.Id);

            var document = _mapper.Map<VehicleDocument>(vehicle);
            var session = GetSession();

            if (session is not null && session.IsInTransaction)
            {
                await _collection
                    .ReplaceOneAsync(session, d => d.Id == vehicle.Id, document, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await _collection
                    .ReplaceOneAsync(d => d.Id == vehicle.Id, document, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }

            _logger.LogInformation("Successfully updated vehicle with ID {VehicleId}", vehicle.Id);
        }

        /// <inheritdoc />
        public async Task<Vehicle> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Retrieving vehicle with ID {VehicleId}", id);

            var document = await _collection
                .Find(d => d.Id == id)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (document is null)
            {
                _logger.LogWarning("Vehicle with ID {VehicleId} was not found", id);
                return null;
            }

            _logger.LogInformation("Successfully retrieved vehicle with ID {VehicleId}", id);

            return _mapper.Map<Vehicle>(document);
        }

        private IClientSessionHandle GetSession()
        {
            return (_unitOfWork as IMongoSessionProvider)?.GetSession();
        }
    }
}
