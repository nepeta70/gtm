using System.Threading.RateLimiting;
using GtMotive.Estimate.Microservice.Api.Resilience;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.DependencyInjection;

namespace GtMotive.Estimate.Microservice.Host.DependencyInjection
{
    public static class ApiResilienceExtensions
    {
        public static IServiceCollection AddApiResilience(this IServiceCollection services)
        {
            // 1. Endpoint Timeouts (Prevent clients from holding threads hostage)
            services.AddRequestTimeouts(options =>
            {
                options.DefaultPolicy = new RequestTimeoutPolicy
                {
                    Timeout = ApiResilienceDefaults.DefaultTimeout
                };
            });

            // 2. Rate Limiting (Overload / Circuit Breaker protection for your own API)
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy(ApiResiliencePolicyNames.EndpointConcurrency, httpContext =>
                {
                    return RateLimitPartition.GetConcurrencyLimiter(
                        partitionKey: httpContext.Request.Path.Value,
                        factory: _ => new ConcurrencyLimiterOptions
                        {
                            PermitLimit = ApiResilienceDefaults.EndpointPermitLimit,
                            QueueLimit = ApiResilienceDefaults.EndpointQueueLimit
                        });
                });
            });

            return services;
        }

        public static WebApplication UseApiResilience(this WebApplication app)
        {
            app.UseRateLimiter();
            app.UseRequestTimeouts();

            return app;
        }
    }
}
