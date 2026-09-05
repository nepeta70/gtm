using GtMotive.Estimate.Microservice.ApplicationCore.UseCases;
using MediatR;

namespace GtMotive.Estimate.Microservice.Api.UseCases
{
    /// <summary>
    /// MediatR wrapper for a void use-case input.
    /// </summary>
    /// <typeparam name="TInput">The type of the use-case input.</typeparam>
    public sealed record UseCaseRequest<TInput>(TInput Input) : IRequest
        where TInput : IUseCaseInput;
}
