using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GtMotive.Estimate.Microservice.Domain.Interfaces;
using GtMotive.Estimate.Microservice.Infrastructure.MongoDb;
using GtMotive.Estimate.Microservice.Infrastructure.Resilience;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Polly;

namespace GtMotive.Estimate.Microservice.Infrastructure.EventStore
{
    /// <summary>
    /// MongoDB implementation of <see cref="IEventStore"/>.
    /// Applies the shared MongoDB resilience pipeline to every operation.
    /// </summary>
    public sealed class MongoEventStore(
        MongoService mongoService,
        IAppLogger<MongoEventStore> logger,
        [FromKeyedServices(ResiliencePipelineNames.Mongo)] ResiliencePipeline mongoPipeline) : IEventStore
    {
        private readonly IMongoCollection<EventDocument> _collection = mongoService.Database.GetCollection<EventDocument>("events");

        /// <inheritdoc />
        public async Task AppendAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken)
            where TEvent : notnull
        {
            ArgumentNullException.ThrowIfNull(domainEvent);

            var eventType = domainEvent.GetType().Name;
            logger.LogInformation("Appending event {EventType} to event store", eventType);

            var document = new EventDocument
            {
                Id = Guid.NewGuid(),
                EventType = eventType,
                Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                OccurredOn = DateTime.UtcNow
            };

            await mongoPipeline.ExecuteAsync(
                async ct => await _collection.InsertOneAsync(document, cancellationToken: ct),
                cancellationToken);

            logger.LogInformation("Successfully appended event {EventType} to event store", eventType);
        }
    }
}
