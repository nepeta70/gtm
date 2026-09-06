using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases;
using GtMotive.Estimate.Microservice.Domain.Interfaces;
using MediatR;

namespace GtMotive.Estimate.Microservice.Api.UseCases
{
    /// <summary>
    /// A MediatR pipeline behavior that tracks the execution time and success/failure rate
    /// of all use cases without polluting the use case code itself.
    /// </summary>
    /// <typeparam name="TInput">The type of the use-case input.</typeparam>
    public sealed class UseCaseTelemetryBehavior<TInput>(ITelemetry telemetry, IAppLogger<UseCaseTelemetryBehavior<TInput>> logger) : IPipelineBehavior<UseCaseRequest<TInput>, Unit>
        where TInput : IUseCaseInput
    {
        public async Task<Unit> Handle(
            UseCaseRequest<TInput> request,
            RequestHandlerDelegate<Unit> next,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(next);

            var useCaseName = typeof(TInput).Name;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var result = await next(cancellationToken);
                stopwatch.Stop();

                logger.LogInformation("Use case {UseCaseName} executed successfully in {ExecutionTimeMs} ms.", useCaseName, stopwatch.ElapsedMilliseconds);
                telemetry.TrackMetric($"{useCaseName}_ExecutionTimeMs", stopwatch.ElapsedMilliseconds, null);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                logger.LogError(ex, "Use case {UseCaseName} failed after {ExecutionTimeMs} ms.", useCaseName, stopwatch.ElapsedMilliseconds);
                telemetry.TrackEvent($"{useCaseName}_Failed", new Dictionary<string, string>
                {
                    { "ExceptionType", ex.GetType().Name },
                    { "ErrorMessage", ex.Message },
                    { "ExecutionTimeMs", stopwatch.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture) }
                });

                throw;
            }
        }
    }
}
