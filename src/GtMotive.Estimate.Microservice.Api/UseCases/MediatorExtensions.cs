using System;
using System.Threading;
using System.Threading.Tasks;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases;
using MediatR;

namespace GtMotive.Estimate.Microservice.Api.UseCases
{
    /// <summary>
    /// Extension methods for <see cref="IMediator"/> to dispatch use cases.
    /// </summary>
    public static class MediatorExtensions
    {
        /// <summary>
        /// Sends a use-case input through MediatR.
        /// </summary>
        /// <typeparam name="TInput">The type of the use-case input.</typeparam>
        /// <param name="mediator">The mediator instance.</param>
        /// <param name="input">The use-case input.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static Task SendUseCase<TInput>(
            this IMediator mediator,
            TInput input,
            CancellationToken cancellationToken)
            where TInput : IUseCaseInput
        {
            ArgumentNullException.ThrowIfNull(mediator);
            return mediator.Send(new UseCaseRequest<TInput>(input), cancellationToken);
        }
    }
}
