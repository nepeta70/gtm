using System;
using System.Threading;
using System.Threading.Tasks;
using GtMotive.Estimate.Microservice.Domain.Interfaces;
using GtMotive.Estimate.Microservice.Infrastructure.Resilience;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Polly;

namespace GtMotive.Estimate.Microservice.Infrastructure.Persistence
{
    /// <summary>
    /// MongoDB implementation of <see cref="IUnitOfWork"/>.
    /// Starts a client session and transaction on first use, commits on <see cref="Save"/>,
    /// and gracefully falls back to single-document atomicity when transactions are unavailable.
    /// The commit goes through a retry pipeline because MongoDB flags failed commits with
    /// UnknownTransactionCommitResult, which is explicitly safe to retry.
    /// </summary>
    public sealed class MongoUnitOfWork : IUnitOfWork, IMongoSessionProvider, IDisposable
    {
        private readonly IMongoClient _client;
        private readonly IAppLogger<MongoUnitOfWork> _logger;
        private readonly ResiliencePipeline _commitPipeline;
        private IClientSessionHandle _session;
        private bool _transactionStarted;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoUnitOfWork"/> class.
        /// </summary>
        /// <param name="client">The MongoDB client.</param>
        /// <param name="logger">The application logger.</param>
        /// <param name="commitPipeline">Resilience pipeline applied to the transaction commit.</param>
        public MongoUnitOfWork(
            IMongoClient client,
            IAppLogger<MongoUnitOfWork> logger,
            [FromKeyedServices(ResiliencePipelineNames.MongoTransactionCommit)] ResiliencePipeline commitPipeline)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(commitPipeline);

            _client = client;
            _logger = logger;
            _commitPipeline = commitPipeline;
        }

        /// <inheritdoc />
        public IClientSessionHandle GetSession()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_session is not null)
            {
                return _session;
            }

            _session = _client.StartSession();

            try
            {
                _session.StartTransaction();
                _transactionStarted = true;
                _logger.LogInformation("MongoDB transaction started.");
            }
            catch (MongoException ex)
            {
                var message = $"MongoDB transactions are not available in the current deployment: {ex.Message}. Falling back to single-document atomicity.";
                _logger.LogWarning(message);
            }
            catch (NotSupportedException ex)
            {
                var message = $"MongoDB transactions are not supported: {ex.Message}. Falling back to single-document atomicity.";
                _logger.LogWarning(message);
            }

            return _session;
        }

        /// <inheritdoc />
        public async Task<int> Save(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_session is null || !_transactionStarted)
            {
                return 0;
            }

            await _commitPipeline
                .ExecuteAsync(
                    static async (session, ct) => await session.CommitTransactionAsync(ct).ConfigureAwait(false),
                    _session,
                    cancellationToken);

            _logger.LogInformation("MongoDB transaction committed.");

            return 1;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_transactionStarted && _session is { IsInTransaction: true })
            {
                _session.AbortTransaction();
            }

            _session?.Dispose();
        }
    }
}
