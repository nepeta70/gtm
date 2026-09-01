using MongoDB.Driver;

namespace GtMotive.Estimate.Microservice.Infrastructure.Persistence
{
    /// <summary>
    /// Provides access to the current MongoDB client session.
    /// </summary>
    internal interface IMongoSessionProvider
    {
        /// <summary>
        /// Gets the current MongoDB client session, starting a transaction when possible.
        /// </summary>
        /// <returns>The current <see cref="IClientSessionHandle"/>.</returns>
        IClientSessionHandle GetSession();
    }
}
