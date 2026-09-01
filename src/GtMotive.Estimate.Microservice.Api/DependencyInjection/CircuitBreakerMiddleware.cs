using System;
using System.Threading.Tasks;
using GtMotive.Estimate.Microservice.Api.Exceptions;
using Microsoft.AspNetCore.Http;
using Polly;
using Polly.CircuitBreaker;

namespace GtMotive.Estimate.Microservice.Host.DependencyInjection
{
    public class CircuitBreakerMiddleware(RequestDelegate next, ResiliencePipeline pipeline)
    {
        private readonly RequestDelegate _next = next;
        private readonly ResiliencePipeline _pipeline = pipeline;

        public async Task InvokeAsync(HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            try
            {
                await _pipeline.ExecuteAsync(
                    async token =>
                    {
                        await _next(context);

                        // If the downstream endpoint resulted in a 5xx error, throw an exception
                        // to tell Polly this request was a failure.
                        if (context.Response.StatusCode >= 500)
                        {
                            throw new ServerErrorException(context.Response.StatusCode);
                        }
                    },
                    context.RequestAborted);
            }
            catch (BrokenCircuitException)
            {
                // The circuit is open. Throw our domain-agnostic exception so the
                // BusinessExceptionHandler can format the 503 ProblemDetails response.
                throw new CircuitBrokenException();
            }
            catch (ServerErrorException)
            {
                // The 5xx response was already written to the context by the BusinessExceptionHandler.
                // We swallow the internal tracking exception here so it doesn't crash the process.
            }
        }
    }
}
