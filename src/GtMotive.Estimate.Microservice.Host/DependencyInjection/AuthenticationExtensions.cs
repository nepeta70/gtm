using GtMotive.Estimate.Microservice.Host.Configuration;
using GtMotive.Estimate.Microservice.Infrastructure.Authorization;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GtMotive.Estimate.Microservice.Host.DependencyInjection
{
    /// <summary>
    /// Wires up authentication for the Host. Prefers a Docker/dev-friendly symmetric JWT
    /// (see <see cref="JwtBearerAuthenticationExtensions"/>) when a Jwt:Secret is configured,
    /// falling back to IdentityServer in environments where no secret is available.
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

            if (!environment.IsDevelopment())
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultScheme = IdentityServerAuthenticationDefaults.AuthenticationScheme;
                })
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = appSettings?.JwtAuthority;
                    options.ApiName = "estimate-api";
                    options.SupportedTokens = SupportedTokens.Jwt;
                });

                return services;
            }

            // Development/Test without a Jwt secret: leave authentication to callers
            // (tests use TestServerDefaults scheme; no protected endpoints require a real scheme locally).
            services.AddAuthentication();
            return services;
        }
    }
}
