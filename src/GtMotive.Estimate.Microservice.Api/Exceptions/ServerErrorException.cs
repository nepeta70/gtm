using System;

namespace GtMotive.Estimate.Microservice.Api.Exceptions
{
    public sealed class ServerErrorException : Exception
    {
        public ServerErrorException(int statusCode)
            : base($"Server returned status code {statusCode}")
        {
        }

        public ServerErrorException()
        {
        }

        public ServerErrorException(string message)
            : base(message)
        {
        }

        public ServerErrorException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
