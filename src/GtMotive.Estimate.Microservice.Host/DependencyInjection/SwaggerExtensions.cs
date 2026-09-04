using System;
using System.Collections.Generic;
using System.Reflection;
using GtMotive.Estimate.Microservice.Host.Configuration;
using GtMotive.Estimate.Microservice.Host.Infrastructure.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GtMotive.Estimate.Microservice.Host.DependencyInjection
{
    internal static class SwaggerExtensions
    {
        internal const string JwtSecuritySchemeName = "bearer";
        internal const string OAuth2SecuritySchemeName = "oauth2";

        private static string AssemblyName => Assembly.GetEntryAssembly().GetName().Name;

        private static string AssemblyVersion => Assembly.GetEntryAssembly().GetName().Version.ToString();

        public static IServiceCollection AddSwagger(
            this IServiceCollection services,
            AppSettings settings,
            IConfiguration configuration)
        {
            var securitySchemeName = ResolveSecuritySchemeName(configuration);

            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(
                options =>
                {
                    options.CustomSchemaIds(type => type.ToString());
                    options.SwaggerDoc($"v{AssemblyVersion}", new OpenApiInfo
                    {
                        Title = $"{AssemblyName} API",
                        Version = $"v{AssemblyVersion}",
                    });

                    if (configuration.GetValue<string>("Swagger:EnableTryIt") == "Yes")
                    {
                        AddSecurityDefinition(options, securitySchemeName, settings, configuration);
                        options.OperationFilter<IdentityServerApiSecurityOperationFilter>(securitySchemeName);
                    }
                });

            return services;
        }

        public static IApplicationBuilder UseSwaggerInApplication(
            this IApplicationBuilder app,
            PathBase pathBase,
            IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(pathBase);

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger(options =>
            {
                if (!pathBase.IsDefault)
                {
                    options.RouteTemplate = "swagger/{documentName}/swagger.json";
                    options.PreSerializeFilters.Add((document, request) =>
                    {
                        document.Servers =
                        [
                            new OpenApiServer
                            {
                                Url = $"{request.Scheme}://{request.Host.Value}{pathBase.CurrentWithoutTrailingSlash}"
                            }
                        ];
                    });
                }
            });

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
            var url = pathBase.IsDefault
                ? $"/swagger/v{AssemblyVersion}/swagger.json"
                : $"{pathBase.CurrentWithoutTrailingSlash}/swagger/v{AssemblyVersion}/swagger.json";

            app.UseSwaggerUI(
                options =>
                {
                    options.SwaggerEndpoint(url, $"{AssemblyName} API V{AssemblyVersion}");

                    if (configuration.GetValue<string>("Swagger:EnableTryIt") == "No")
                    {
                        options.SupportedSubmitMethods();
                    }

                    if (ResolveSecuritySchemeName(configuration) == OAuth2SecuritySchemeName)
                    {
                        options.OAuthClientId("client-gtestimate-swagger");
                        options.OAuthClientSecret("gtmotive");
                        options.OAuthScopeSeparator(" ");
                        options.OAuthScopes("estimate-public-scope");
                    }
                });

            return app;
        }

        private static string ResolveSecuritySchemeName(IConfiguration configuration)
        {
            var jwtSecret = configuration["Jwt:Secret"] ?? configuration["Jwt__Secret"];
            var hasUserJwts = configuration.GetSection("Authentication:Schemes:Bearer").Exists();

            return !string.IsNullOrWhiteSpace(jwtSecret) || hasUserJwts
                ? JwtSecuritySchemeName
                : OAuth2SecuritySchemeName;
        }

        private static void AddSecurityDefinition(
            SwaggerGenOptions options,
            string securitySchemeName,
            AppSettings settings,
            IConfiguration configuration)
        {
            if (securitySchemeName == JwtSecuritySchemeName)
            {
                options.AddSecurityDefinition(JwtSecuritySchemeName, new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Description = "Enter a JWT token",
                });

                return;
            }

            // Define the OAuth2.0 scheme that's in use (i.e. Implicit Flow)
            options.AddSecurityDefinition(OAuth2SecuritySchemeName, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Name = OAuth2SecuritySchemeName,
                Flows = configuration.GetValue<string>("Swagger:AuthFlow") == "AuthorizationCode"
                    ? new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri($"{settings.JwtAuthority}/connect/authorize"),
                            Scopes = new Dictionary<string, string>
                            {
                                ["estimate-public-scope"] = "estimate-api"
                            },
                            TokenUrl = new Uri($"{settings.JwtAuthority}/connect/token")
                        }
                    }
                    : new OpenApiOAuthFlows()
                    {
                        ClientCredentials = new OpenApiOAuthFlow()
                        {
                            Scopes = new Dictionary<string, string>
                            {
                                ["estimate-public-scope"] = "estimate-api"
                            },
                            TokenUrl = new Uri($"{settings.JwtAuthority}/connect/token")
                        }
                    }
            });
        }
    }
}
