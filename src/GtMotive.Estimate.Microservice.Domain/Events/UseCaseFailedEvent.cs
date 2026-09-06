using System;

namespace GtMotive.Estimate.Microservice.Domain.Events
{
    /// <summary>
    /// Event that is published when a use case fails. It contains information about the use case name, exception type, error message, and the time of failure.
    /// </summary>
    /// <param name="UseCaseName">The name of the use case that failed.</param>
    /// <param name="ExceptionType">The type of the exception that caused the failure.</param>
    /// <param name="ErrorMessage">The error message associated with the failure.</param>
    /// <param name="FailedAt">The UTC timestamp when the failure occurred.</param>
    public sealed record UseCaseFailedEvent(
    string UseCaseName,
    string ExceptionType,
    string ErrorMessage,
    DateTime FailedAt);
}
