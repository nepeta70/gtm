using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using GtMotive.Estimate.Microservice.Domain.Entities;
using GtMotive.Estimate.Microservice.Domain.Exceptions;
using GtMotive.Estimate.Microservice.Domain.Interfaces;
using GtMotive.Estimate.Microservice.Infrastructure.Persistence;
using GtMotive.Estimate.Microservice.Infrastructure.Resilience;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Polly;

namespace GtMotive.Estimate.Microservice.Infrastructure.Fleet.MongoDb
{
    /// <summary>
    /// MongoDB implementation of <see cref="IVehicleWriteRepository"/>.
    /// Participates in the active <see cref="IUnitOfWork"/> session when a transaction is available
    /// and applies the shared MongoDB resilience pipeline to every operation.
    /// </summary>
    public sealed class MongoVehicleWriteRepository : IVehicleWriteRepository
    {
        private const string CollectionName = "vehicles";

        private readonly IMongoCollection<VehicleDocument> _collection;
        private readonly IMapper _mapper;
        private readonly IAppLogger<MongoVehicleWriteRepository> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ResiliencePipeline _mongoPipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoVehicleWriteRepository"/> class.
        /// </summary>
        /// <param name="database">The MongoDB database.</param>
        /// <param name="mapper">The AutoMapper instance.</param>
        /// <param name="logger">The application logger.</param>
        /// <param name="unitOfWork">The current unit of work.</param>
        /// <param name="mongoPipeline">The MongoDB resilience pipeline.</param>
        public MongoVehicleWriteRepository(
            IMongoDatabase database,
            IMapper mapper,
            IAppLogger<MongoVehicleWriteRepository> logger,
            IUnitOfWork unitOfWork,
            [FromKeyedServices(ResiliencePipelineNames.Mongo)] ResiliencePipeline mongoPipeline)
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(unitOfWork);
            ArgumentNullException.ThrowIfNull(mongoPipeline);

            _collection = database.GetCollection<VehicleDocument>(CollectionName);
            _mapper = mapper;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _mongoPipeline = mongoPipeline;
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
                // The duplicate-key to ConflictException mapping stays outside the retry
                // callback: the pipeline never retries business conflicts.
                await _mongoPipeline
                    .ExecuteAsync(
                        async ct =>
                        {
                            if (session is not null && session.IsInTransaction)
                            {
                                await _collection
                                    .InsertOneAsync(session, document, cancellationToken: ct)
                                    .ConfigureAwait(false);
                            }
                            else
                            {
                                await _collection
                                    .InsertOneAsync(document, cancellationToken: ct)
                                    .ConfigureAwait(false);
                            }
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
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

            try
            {
                await _mongoPipeline
                    .ExecuteAsync(
                        async ct =>
                        {
                            if (session is not null && session.IsInTransaction)
                            {
                                await _collection
                                    .ReplaceOneAsync(session, d => d.Id == vehicle.Id, document, cancellationToken: ct)
                                    .ConfigureAwait(false);
                            }
                            else
                            {
                                await _collection
                                    .ReplaceOneAsync(d => d.Id == vehicle.Id, document, cancellationToken: ct)
                                    .ConfigureAwait(false);
                            }
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Code == 11000)
            {
                // Duplicate key on the renterId sparse unique index: the renter picked up
                // another rental concurrently between the HasActiveRentalAsync check and this update.
                _logger.LogError(ex, "Renter '{RenterId}' already has an active vehicle rental.", vehicle.RenterId);
                throw new ConflictException($"Renter '{vehicle.RenterId}' already has an active vehicle rental.", ex);
            }

            _logger.LogInformation("Successfully updated vehicle with ID {VehicleId}", vehicle.Id);
        }

        /// <inheritdoc />
        public async Task<Vehicle> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Retrieving vehicle with ID {VehicleId}", id);

            var document = await _mongoPipeline
                .ExecuteAsync(
                    async ct => await _collection
                        .Find(d => d.Id == id)
                        .FirstOrDefaultAsync(ct)
                        .ConfigureAwait(false),
                    cancellationToken)
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
