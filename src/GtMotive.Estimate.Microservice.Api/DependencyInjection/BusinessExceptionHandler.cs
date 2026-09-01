using System;
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
    public sealed class BusinessExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(httpContext);
            ArgumentNullException.ThrowIfNull(exception);

            var appLogger = httpContext.RequestServices.GetRequiredService<IAppLogger<BusinessExceptionHandler>>();

            appLogger.LogError(exception, "Exception captured in DomainExceptionHandler.");

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

                        appLogger.LogWarning("Domain Exception: {status} - {detail}", problemDetails.Status, problemDetails.Detail);

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

                        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
                        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
                        return true;
                    }

                case OperationCanceledException:
                    {
                        // We use 504 Gateway Timeout because the server timed out waiting for the downstream operation (e.g. MongoDB)
                        var problemDetails = new ProblemDetails
                        {
                            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.5",
                            Status = StatusCodes.Status504GatewayTimeout,
                            Title = "Gateway Timeout",
                            Detail = "The request was canceled due to a timeout or client disconnect.",
                            Instance = httpContext.Request.Path,
                        };

                        appLogger.LogWarning("Request canceled due to timeout or client disconnect.");

                        // Note: We check httpContext.Response.HasStarted because if the client disconnected,
                        // we might not be able to write to the response stream.
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

                        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

                        return true;
                    }
            }
        }
    }
}
