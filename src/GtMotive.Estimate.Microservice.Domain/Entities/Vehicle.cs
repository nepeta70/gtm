using System;
using GtMotive.Estimate.Microservice.Domain.Exceptions;
using GtMotive.Estimate.Microservice.Domain.ValueObjects;

namespace GtMotive.Estimate.Microservice.Domain.Entities
{
    /// <summary>
    /// Represents a vehicle that belongs to the rental fleet.
    /// This is the aggregate root for the fleet management bounded context.
    /// </summary>
    public class Vehicle
    {
        /// <summary>
        /// Maximum age, in years, that a vehicle is allowed to have in order to belong to the fleet.
        /// </summary>
        public const int MaxManufactureAgeInYears = 5;

        /// <summary>
        /// Initializes a new instance of the <see cref="Vehicle"/> class.
        /// </summary>
        /// <param name="id">Unique identifier.</param>
        /// <param name="brand">Vehicle brand.</param>
        /// <param name="model">Vehicle model.</param>
        /// <param name="licensePlate">Vehicle license plate.</param>
        /// <param name="manufactureDate">Vehicle manufacture date.</param>
        public Vehicle(Guid id, string brand, string model, string licensePlate, DateTime manufactureDate)
        {
            if (id == Guid.Empty)
            {
                throw new DomainException("Vehicle id cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(brand))
            {
                throw new DomainException("Vehicle brand is required.");
            }

            if (string.IsNullOrWhiteSpace(model))
            {
                throw new DomainException("Vehicle model is required.");
            }

            ArgumentNullException.ThrowIfNull(licensePlate);

            EnsureManufactureDateIsValid(manufactureDate);

            Id = id;
            Brand = brand;
            Model = model;
            LicensePlate = LicensePlate.Create(licensePlate);
            ManufactureDate = manufactureDate.Date;
            Status = VehicleStatus.Available;
            RenterId = null;
            RentedAt = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vehicle"/> class.
        /// Reserved for persistence frameworks.
        /// </summary>
        protected Vehicle()
        {
        }

        /// <summary>
        /// Gets the unique identifier of the vehicle.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets the vehicle brand.
        /// </summary>
        public string Brand { get; private set; }

        /// <summary>
        /// Gets the vehicle model.
        /// </summary>
        public string Model { get; private set; }

        /// <summary>
        /// Gets the vehicle license plate.
        /// </summary>
        public LicensePlate LicensePlate { get; private set; }

        /// <summary>
        /// Gets the vehicle manufacture date.
        /// </summary>
        public DateTime ManufactureDate { get; private set; }

        /// <summary>
        /// Gets the current availability status of the vehicle.
        /// </summary>
        public VehicleStatus Status { get; private set; }

        /// <summary>
        /// Gets the identifier of the person currently renting the vehicle, if any.
        /// </summary>
        public string RenterId { get; private set; }

        /// <summary>
        /// Gets the date and time the vehicle was rented, if any.
        /// </summary>
        public DateTime? RentedAt { get; private set; }

        /// <summary>
        /// Marks the vehicle as rented by the given renter.
        /// </summary>
        /// <param name="renterId">Identifier of the person renting the vehicle.</param>
        public void Rent(string renterId)
        {
            if (string.IsNullOrWhiteSpace(renterId))
            {
                throw new DomainException("RenterId is required to rent a vehicle.");
            }

            if (Status == VehicleStatus.Rented)
            {
                throw new DomainException($"Vehicle '{Id}' is already rented.");
            }

            Status = VehicleStatus.Rented;
            RenterId = renterId;
            RentedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks the vehicle as returned and available again.
        /// </summary>
        /// <param name="renterId">Identifier of the person renting the vehicle.</param>
        public void Return(string renterId)
        {
            if (Status == VehicleStatus.Available)
            {
                throw new DomainException($"Vehicle '{Id}' is not currently rented.");
            }

            if (string.IsNullOrWhiteSpace(renterId))
            {
                throw new DomainException("RenterId is required to return a vehicle.");
            }

            if (RenterId != renterId)
            {
                throw new DomainException($"Vehicle '{Id}' is rented by another renter.");
            }

            Status = VehicleStatus.Available;
            RenterId = null;
            RentedAt = null;
        }

        private static void EnsureManufactureDateIsValid(DateTime manufactureDate)
        {
            if (manufactureDate.Date > DateTime.Today)
            {
                throw new DomainException("Vehicle manufacture date cannot be in the future.");
            }

            var minimumAllowedDate = DateTime.Today.AddYears(-MaxManufactureAgeInYears);

            if (manufactureDate.Date < minimumAllowedDate)
            {
                throw new DomainException(
                    $"Vehicle manufacture date cannot be older than {MaxManufactureAgeInYears} years.");
            }
        }
    }
}
