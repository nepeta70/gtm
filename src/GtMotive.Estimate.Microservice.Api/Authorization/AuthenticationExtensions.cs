using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GtMotive.Estimate.Microservice.Api.Authorization
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static class AuthenticationExtensions
    {
        // Intentionally minimal; Host project owns full authentication wiring.
        public static IServiceCollection AddJwtOrIdentityServerAuthentication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment, object appSettings)
        {
            // Parameters referenced to satisfy repository style rules; actual wiring lives in Host project.
            _ = services;
            _ = configuration;
            _ = environment;
            _ = appSettings;
            return services;
        }
    }
}
