namespace GtMotive.Estimate.Microservice.Domain.ValueObjects
{
    /// <summary>
    /// Represents a vehicle license plate. Immutable by design.
    /// </summary>
    public sealed record LicensePlate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LicensePlate"/> class with the specified value.
        /// </summary>
        /// <param name="value">The value of the license plate.</param>
        private LicensePlate(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the value of the license plate.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Creates a new instance of <see cref="LicensePlate"/> with the specified value.
        /// </summary>
        /// <param name="value">The value of the license plate.</param>
        /// <returns>The created <see cref="LicensePlate"/> instance.</returns>
        /// <exception cref="DomainException">Thrown when the specified value is null or whitespace.</exception>
        public static LicensePlate Create(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? throw new DomainException("Vehicle license plate is required.") : new LicensePlate(value);
        }

        /// <summary>
        /// Returns a string representation of the license plate.
        /// </summary>
        /// <returns>The string representation of the license plate.</returns>
        public override string ToString() => Value;
    }
}
