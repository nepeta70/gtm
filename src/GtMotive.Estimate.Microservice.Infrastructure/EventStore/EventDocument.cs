using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GtMotive.Estimate.Microservice.Infrastructure.EventStore
{
    public sealed class EventDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; }

        public string EventType { get; set; }

        public string Payload { get; set; }

        public DateTime OccurredOn { get; set; }
    }
}
