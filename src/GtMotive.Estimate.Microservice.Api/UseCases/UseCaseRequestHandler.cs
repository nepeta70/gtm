using System;
using System.Threading;
using System.Threading.Tasks;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases;
using MediatR;

namespace GtMotive.Estimate.Microservice.Api.UseCases
{
    /// <summary>
    /// MediatR handler that delegates to your existing <see cref="IUseCase{TUseCaseInput}"/>.
    /// </summary>
    /// <typeparam name="TInput">The type of the use-case input.</typeparam>
    /// <remarks>
    /// Initializes a new instance of the <see cref="UseCaseRequestHandler{TInput}"/> class.
    /// </remarks>
    /// <param name="useCase">The use case to delegate to.</param>
    public sealed class UseCaseRequestHandler<TInput>(IUseCase<TInput> useCase) : IRequestHandler<UseCaseRequest<TInput>>
        where TInput : IUseCaseInput
    {
        private readonly IUseCase<TInput> _useCase = useCase;

        /// <inheritdoc />
        public async Task Handle(UseCaseRequest<TInput> request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            await _useCase.Execute(request.Input, cancellationToken);
        }
    }
}
