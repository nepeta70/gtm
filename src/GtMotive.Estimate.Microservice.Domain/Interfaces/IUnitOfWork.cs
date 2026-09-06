using System.Threading;
using System.Threading.Tasks;

namespace GtMotive.Estimate.Microservice.Domain.Interfaces
{
    /// <summary>
    /// Unit Of Work. Should only be used by Use Cases.
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Applies all database changes.
        /// </summary>
        /// <returns>Number of affected rows.</returns>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        Task<int> Save(CancellationToken cancellationToken = default);
    }
}
