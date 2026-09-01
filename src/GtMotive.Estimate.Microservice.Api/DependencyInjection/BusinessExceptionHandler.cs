using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GtMotive.Estimate.Microservice.Domain.Exceptions;
using GtMotive.Estimate.Microservice.Domain.Interfaces;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace GtMotive.Estimate.Microservice.Api.DependencyInjection
{
    public sealed class BusinessExceptionHandler(IServiceScopeFactory serviceScopeFactory) : IExceptionHandler
    {
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(httpContext);
            ArgumentNullException.ThrowIfNull(exception);

            using var scope = _serviceScopeFactory.CreateScope();
            var appLogger = scope.ServiceProvider.GetRequiredService<IAppLogger<BusinessExceptionHandler>>();
            var telemetry = scope.ServiceProvider.GetRequiredService<ITelemetry>();

            appLogger.LogError(exception, "Exception captured in BusinessExceptionHandler.");

            switch (exception)
            {
                case DomainException domainException:
                    {
                        var problemDetails = new ProblemDetails
                        {
                            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                            Status = StatusCodes.Status400BadRequest,
                            Title = "Bad Request",
                            Detail = domainException.Message,
                            Instance = httpContext.Request.Path,
                        };

                        appLogger.LogWarning("Handled {ExceptionType} with status {Status} and detail {Detail}", nameof(DomainException), problemDetails.Status, problemDetails.Detail);

                        telemetry.TrackEvent(nameof(DomainException), new Dictionary<string, string>
                        {
                            { nameof(Type), domainException.GetType().Name },
                            { nameof(Exception.Message), domainException.Message },
                            { nameof(HttpContext.Request.Path), httpContext.Request.Path }
                        });

                        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

                        return true;
                    }

                case ConflictException conflictException:
                    {
                        var problemDetails = new ProblemDetails
                        {
                            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                            Status = StatusCodes.Status409Conflict,
                            Title = "Conflict",
                            Detail = conflictException.Message,
                            Instance = httpContext.Request.Path,
                        };

                        telemetry.TrackEvent(nameof(ConflictException), new Dictionary<string, string>
                        {
                            { nameof(Type), conflictException.GetType().Name },
                            { nameof(Exception.Message), conflictException.Message },
                            { nameof(HttpContext.Request.Path), httpContext.Request.Path }
                        });

                        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
                        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
                        return true;
                    }

                case OperationCanceledException:
                    {
                        var problemDetails = new ProblemDetails
                        {
                            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.5",
                            Status = StatusCodes.Status504GatewayTimeout,
                            Title = "Gateway Timeout",
                            Detail = "The request was canceled due to a timeout or client disconnect.",
                            Instance = httpContext.Request.Path,
                        };

                        appLogger.LogWarning("Request canceled due to timeout or client disconnect.");

                        telemetry.TrackEvent(nameof(OperationCanceledException), new Dictionary<string, string>
                        {
                            { nameof(Type), exception.GetType().Name },
                            { nameof(HttpContext.Request.Path), httpContext.Request.Path }
                        });

                        if (!httpContext.Response.HasStarted)
                        {
                            httpContext.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
                            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
                        }

                        return true;
                    }

                default:
                    {
                        var problemDetails = new ProblemDetails
                        {
                            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                            Status = StatusCodes.Status500InternalServerError,
                            Title = "Internal Server Error",
                            Detail = exception.Message,
                            Instance = httpContext.Request.Path,
                        };

                        appLogger.LogError(exception, "Unhandled Exception");

                        telemetry.TrackEvent(nameof(Exception), new Dictionary<string, string>
                        {
                            { nameof(Type), exception.GetType().Name },
                            { nameof(Exception.Message), exception.Message },
                            { nameof(HttpContext.Request.Path), httpContext.Request.Path }
                        });

                        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

                        return true;
                    }
            }
        }
    }
}
