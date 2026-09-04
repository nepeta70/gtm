using System;
using System.Collections.Generic;
using System.Security.Claims;
using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Test;
using Microsoft.Extensions.Configuration;

namespace GtMotive.Estimate.IdentityServer
{
    /// <summary>
    /// In-memory IdentityServer configuration matching the expectations of the Estimate
    /// microservice host: the <c>estimate-api</c> API (see ApiName in the host
    /// authentication setup), the <c>estimate-public-scope</c> scope and the
    /// <c>client-gtestimate-swagger</c> client used by Swagger UI.
    /// </summary>
    internal static class IdentityServerConfig
    {
        public static IReadOnlyList<IdentityResource> IdentityResources =>
        [
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResource("roles", "User roles", ["role"]),
        ];

        public static IReadOnlyList<ApiScope> ApiScopes =>
        [
            new ApiScope("estimate-public-scope", "Estimate API")
            {
                UserClaims = { "role" },
            },
        ];

        public static IReadOnlyList<ApiResource> ApiResources =>
        [
            new ApiResource("estimate-api", "Estimate API")
            {
                Scopes = { "estimate-public-scope" },
                UserClaims = { "role" },
            },
        ];

        public static IReadOnlyList<Client> GetClients(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var swaggerClientSecret = GetRequiredValue(configuration, "IdentityServer:SwaggerClientSecret");
            var redirectUris = GetRequiredValues(configuration, "IdentityServer:RedirectUris");
            var allowedCorsOrigins = GetRequiredValues(configuration, "IdentityServer:AllowedCorsOrigins");

            return
            [
                new Client
                {
                    ClientId = "client-gtestimate-swagger",
                    ClientName = "Estimate API Swagger UI",
                    ClientSecrets = { new Secret(swaggerClientSecret.Sha256()) },

                    AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
                    RequirePkce = false,

                    RedirectUris = redirectUris,
                    AllowedCorsOrigins = allowedCorsOrigins,

                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        "roles",
                        "estimate-public-scope",
                    },

                    ClientClaimsPrefix = string.Empty,
                    Claims =
                    {
                        new ClientClaim("role", "Admin"),
                        new ClientClaim("role", "User"),
                    },
                },
            ];
        }

        public static List<TestUser> GetUsers(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var adminPassword = GetRequiredValue(configuration, "IdentityServer:AdminPassword");
            var userPassword = GetRequiredValue(configuration, "IdentityServer:UserPassword");

            return
            [
                new TestUser
                {
                    SubjectId = "8f4d2b1e-0000-4000-8000-000000000001",
                    Username = "admin",
                    Password = adminPassword,
                    Claims =
                    {
                        new Claim("role", "Admin"),
                        new Claim("role", "User"),
                    },
                },
                new TestUser
                {
                    SubjectId = "8f4d2b1e-0000-4000-8000-000000000002",
                    Username = "user",
                    Password = userPassword,
                    Claims =
                    {
                        new Claim("role", "User"),
                    },
                },
            ];
        }

        private static string GetRequiredValue(IConfiguration configuration, string key)
        {
            var value = configuration.GetValue<string>(key);
            return string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException($"Configuration value '{key}' is required.") : value;
        }

        private static List<string> GetRequiredValues(IConfiguration configuration, string key)
        {
            var values = configuration.GetSection(key).Get<List<string>>();
            return values is null || values.Count == 0
                ? throw new InvalidOperationException($"Configuration value '{key}' is required.")
                : values;
        }
    }
}
