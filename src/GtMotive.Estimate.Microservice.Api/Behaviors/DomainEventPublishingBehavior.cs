using System;
using System.Threading;
using System.Threading.Tasks;
using GtMotive.Estimate.Microservice.ApplicationCore.Events.Ports;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases;
using GtMotive.Estimate.Microservice.Domain.Events;
using GtMotive.Estimate.Microservice.Domain.Interfaces;
using MediatR;

namespace GtMotive.Estimate.Microservice.Api.UseCases
{
    /// <summary>
    /// MediatR pipeline behavior that publishes collected domain events after a use case
    /// succeeds, and publishes a failure event when a use case throws.
    /// </summary>
    /// <typeparam name="TInput">The type of the use-case input.</typeparam>
    public sealed class DomainEventPublishingBehavior<TInput>(
        IDomainEventEnvelope eventCollector,
        IBusFactory busFactory,
        IAppLogger<DomainEventPublishingBehavior<TInput>> logger)
        : IPipelineBehavior<UseCaseRequest<TInput>, Unit>
        where TInput : IUseCaseInput
    {
        public async Task<Unit> Handle(
            UseCaseRequest<TInput> request,
            RequestHandlerDelegate<Unit> next,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(next);

            try
            {
                var result = await next(cancellationToken);

                if (eventCollector.HasEvent)
                {
                    await busFactory
                        .GetClient(eventCollector.DomainEvent.GetType())
                        .Send(eventCollector.DomainEvent, cancellationToken);
                }

                return result;
            }
            catch (Exception ex)
            {
                var failureEvent = new UseCaseFailedEvent(
                    typeof(TInput).Name,
                    ex.GetType().Name,
                    ex.Message,
                    DateTime.UtcNow);

                try
                {
                    await busFactory.GetClient(typeof(UseCaseFailedEvent)).Send(failureEvent, cancellationToken);
                }
                catch (Exception busEx)
                {
                    logger.LogError(busEx, "Failed to publish failure event for {UseCase}", typeof(TInput).Name);
                    throw;
                }

                throw;
            }
            finally
            {
                eventCollector.Clear();
            }
        }
    }
}
