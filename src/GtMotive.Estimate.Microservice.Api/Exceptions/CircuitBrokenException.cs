using System;

namespace GtMotive.Estimate.Microservice.Api.Exceptions
{
    public sealed class CircuitBrokenException : Exception
    {
        public CircuitBrokenException()
            : base("The service is currently unavailable due to a high rate of errors.")
        {
        }

        public CircuitBrokenException(string message)
            : base(message)
        {
        }

        public CircuitBrokenException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
