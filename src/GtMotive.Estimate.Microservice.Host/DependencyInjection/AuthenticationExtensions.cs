using System;
using GtMotive.Estimate.Microservice.Host.Configuration;
using GtMotive.Estimate.Microservice.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GtMotive.Estimate.Microservice.Host.DependencyInjection
{
    /// <summary>
    /// Wires up authentication for the Host. Prefers a Docker/dev-friendly symmetric JWT
    /// (see <see cref="JwtBearerAuthenticationExtensions"/>) when a Jwt:Secret is configured,
    /// falling back to an OpenID Connect identity provider (the docker-compose stack points
    /// JwtAuthority to the bundled GtMotive.Estimate.IdentityServer container) whenever a real
    /// authority is configured, and finally to a scheme-less setup for tests/local runs with neither.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal static class AuthenticationExtensions
    {
        internal static IServiceCollection AddHostAuthentication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment, AppSettings appSettings)
        {
            if (services.TryAddJwtBearerAuthentication(configuration, requireHttpsMetadata: !environment.IsDevelopment()))
            {
                return services;
            }

            var authority = appSettings?.JwtAuthority;
            if (!string.IsNullOrWhiteSpace(authority) &&
                !authority.StartsWith("Set by environment", StringComparison.OrdinalIgnoreCase))
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.Authority = authority;
                    options.Audience = "estimate-api";

                    // The bundled IdentityServer container serves plain HTTP.
                    options.RequireHttpsMetadata = !environment.IsDevelopment();
                });

                return services;
            }

            // Development/Test without a Jwt secret or authority: leave authentication to callers
            // (tests use TestServerDefaults scheme; no protected endpoints require a real scheme locally).
            services.AddAuthentication();
            return services;
        }
    }
}
