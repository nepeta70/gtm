using System;
using System.Threading;
using System.Threading.Tasks;
using GtMotive.Estimate.Microservice.Domain;
using GtMotive.Estimate.Microservice.Domain.Interfaces;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace GtMotive.Estimate.Microservice.Api.DependencyInjection
{
    public sealed class DomainExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(httpContext);
            ArgumentNullException.ThrowIfNull(exception);

            var appLogger = httpContext.RequestServices.GetRequiredService<IAppLogger<DomainExceptionHandler>>();

            appLogger.LogError(exception, "Exception captured in DomainExceptionHandler.");

            if (exception is DomainException domainException)
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
            else
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
