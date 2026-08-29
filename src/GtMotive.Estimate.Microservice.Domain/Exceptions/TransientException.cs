using System;

namespace GtMotive.Estimate.Microservice.Domain.Exceptions
{
    /// <summary>
    /// Exception that is thrown when a transient error occurs, such as a temporary network failure or a timeout.
    /// </summary>
    public class TransientException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransientException"/> class.
        /// </summary>
        public TransientException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransientException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public TransientException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransientException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public TransientException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
