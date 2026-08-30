using System;
using GtMotive.Estimate.Microservice.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GtMotive.Estimate.Microservice.Infrastructure.Fleet.MongoDb
{
    /// <summary>
    /// Persistence model for a <see cref="Vehicle"/> stored in MongoDB.
    /// </summary>
    public sealed class VehicleDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; }

        public string Brand { get; set; }

        public string Model { get; set; }

        [BsonElement("licensePlate")]
        public string LicensePlate { get; set; }

        public DateTime ManufactureDate { get; set; }

        [BsonRepresentation(BsonType.String)]
        public VehicleStatus Status { get; set; }

        [BsonIgnoreIfNull]
        public string RenterId { get; set; }

        [BsonIgnoreIfNull]
        public DateTime? RentedAt { get; set; }
    }
}
