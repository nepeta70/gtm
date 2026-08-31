using System;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace GtMotive.Estimate.Microservice.Infrastructure.Authorization
{
    /// <summary>
    /// Configures a Docker/dev-friendly JWT Bearer authentication scheme backed by a shared
    /// symmetric secret (HS256). This avoids requiring an external identity provider (such as
    /// IdentityServer) for local development and containerized environments, while still
    /// supporting issuer/audience validation when those values are configured.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class JwtBearerAuthenticationExtensions
    {
        /// <summary>
        /// Configures JWT Bearer authentication when a symmetric secret is present in configuration
        /// under "Jwt:Secret" (or the "Jwt__Secret" environment variable convention used by Docker Compose).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="requireHttpsMetadata">Whether HTTPS metadata is required. Should be false only for local/Docker development.</param>
        /// <returns>True when JWT Bearer authentication was configured; false when no secret was found in configuration.</returns>
        public static bool TryAddJwtBearerAuthentication(this IServiceCollection services, IConfiguration configuration, bool requireHttpsMetadata)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            var jwtSecret = configuration["Jwt:Secret"] ?? configuration["Jwt__Secret"];
            if (string.IsNullOrWhiteSpace(jwtSecret))
            {
                return false;
            }

            var jwtIssuer = configuration["Jwt:Issuer"];
            var jwtAudience = configuration["Jwt:Audience"];
            var validateIssuer = !string.IsNullOrEmpty(jwtIssuer);
            var validateAudience = !string.IsNullOrEmpty(jwtAudience);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = requireHttpsMetadata;
                options.SaveToken = true;

                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidateLifetime = true,
                    ValidateIssuer = validateIssuer,
                    ValidateAudience = validateAudience,
                };

                if (validateIssuer)
                {
                    tokenValidationParameters.ValidIssuer = jwtIssuer;
                }

                if (validateAudience)
                {
                    tokenValidationParameters.ValidAudience = jwtAudience;
                }

                options.TokenValidationParameters = tokenValidationParameters;
            });

            return true;
        }
    }
}
