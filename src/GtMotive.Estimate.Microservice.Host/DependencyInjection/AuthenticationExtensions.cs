using GtMotive.Estimate.Microservice.Domain.Interfaces;
using GtMotive.Estimate.Microservice.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace GtMotive.Estimate.Microservice.Host.DependencyInjection
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal static class AuthenticationExtensions
    {
        internal static IServiceCollection AddHostAuthentication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment, object appSettings)
        {
            // Parameters referenced to satisfy style analyzers; appSettings is intentionally unused here.
            _ = appSettings;

            // If Jwt secret provided, prefer simple HS256 JWT for Docker/dev scenarios
            var jwtSecret = configuration["Jwt:Secret"] ?? configuration["Jwt__Secret"];
            if (!string.IsNullOrWhiteSpace(jwtSecret))
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = !environment.IsDevelopment();
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = key,
                        ValidateIssuer = !string.IsNullOrWhiteSpace(configuration["Jwt:Issuer"]),
                        ValidateAudience = !string.IsNullOrWhiteSpace(configuration["Jwt:Audience"]),
                        ValidIssuer = configuration["Jwt:Issuer"],
                        ValidAudience = configuration["Jwt:Audience"]
                    };
                });

                // Use JwtAuthorizationService from Infrastructure
                services.AddSingleton<IAuthorizationService, JwtAuthorizationService>();
                return services;
            }

            // Fallback to IdentityServer (existing behavior)
            // IdentityServer types are referenced by the Host project; preserve prior behavior.
            if (!string.IsNullOrWhiteSpace(configuration["IdentityServer:Authority"]))
            {
                services.AddAuthentication("Bearer")
                    .AddJwtBearer("Bearer", options =>
                    {
                        options.Authority = configuration["IdentityServer:Authority"];
                        options.RequireHttpsMetadata = !environment.IsDevelopment();
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateAudience = true,
                            ValidAudience = configuration["Jwt:Audience"] ?? "estimate-api",
                        };
                    });

                services.AddSingleton<IAuthorizationService, JwtAuthorizationService>();
                return services;
            }

            // No-op: register default authorization service
            services.AddSingleton<IAuthorizationService, JwtAuthorizationService>();
            return services;
        }
    }
}
