using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GtMotive.Estimate.Microservice.Api.Endpoints
{
    public static class HealthEndpoints
    {
        public static IEndpointRouteBuilder MapHealth(this IEndpointRouteBuilder app)
        {
            var healthCheckOptions = new HealthCheckOptions
            {
                AllowCachingResponses = false,
                ResultStatusCodes =
                {
                    [HealthStatus.Healthy] = StatusCodes.Status200OK,
                    [HealthStatus.Degraded] = StatusCodes.Status200OK,
                    [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
                }
            };

            app.MapHealthChecks("/health/live", healthCheckOptions)
               .WithTags("Health")
               .WithName("LivenessProbe")
               .WithSummary("Kubernetes liveness probe endpoint");

            app.MapHealthChecks("/health/ready", healthCheckOptions)
               .WithTags("Health")
               .WithName("ReadinessProbe")
               .WithSummary("Kubernetes readiness probe endpoint");

            return app;
        }
    }
}
