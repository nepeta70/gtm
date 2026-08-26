using System.Threading.Tasks;
using GtMotive.Estimate.Microservice.Domain.Interfaces;

namespace GtMotive.Estimate.Microservice.Infrastructure.Persistence
{
    /// <summary>
    /// No-operation implementation of <see cref="IUnitOfWork"/> for development or testing.
    /// </summary>
    public sealed class NoOpUnitOfWork : IUnitOfWork
    {
        /// <inheritdoc />
        public Task<int> Save() => Task.FromResult(0);
    }
}
