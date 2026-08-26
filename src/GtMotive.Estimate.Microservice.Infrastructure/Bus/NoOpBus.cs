using System.Threading.Tasks;
using GtMotive.Estimate.Microservice.Domain.Interfaces;

namespace GtMotive.Estimate.Microservice.Infrastructure.Bus
{
    /// <summary>
    /// No-operation implementation of <see cref="IBus"/> for development or testing.
    /// </summary>
    public sealed class NoOpBus : IBus
    {
        /// <inheritdoc />
        public Task Send(object message) => Task.CompletedTask;
    }
}
