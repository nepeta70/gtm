using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using GtMotive.Estimate.Microservice.Infrastructure.Authorization;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace GtMotive.Estimate.Microservice.FunctionalTests.Specs
{
    /// <summary>
    /// Functional coverage for <see cref="DevJwtTokenGenerator"/>, the dev-only token minting
    /// utility shared by the InfrastructureTests JWT suite and the
    /// GtMotive.Estimate.Microservice.DevTokenGenerator CLI tool. Does not require Docker/Mongo.
    /// </summary>
    public sealed class DevJwtTokenGeneratorTests
    {
        private const string Secret = "functional-test-secret-please-change-0123456789";

        [Fact]
        public void GenerateTokenProducesTokenValidatedByThePublishedSecret()
        {
            var token = DevJwtTokenGenerator.GenerateToken(
                Secret,
                subject: "functional-test-user",
                roles: ["Admin", "Fleet-Manager"],
                issuer: "gtmotive-functional-tests",
                audience: "gtmotive-estimate-api");

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),
                ValidateIssuer = true,
                ValidIssuer = "gtmotive-functional-tests",
                ValidateAudience = true,
                ValidAudience = "gtmotive-estimate-api",
                ValidateLifetime = true,
            };

            var principal = new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);

            principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value.Should().Be("functional-test-user");
            principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value).Should().BeEquivalentTo("Admin", "Fleet-Manager");
        }

        [Fact]
        public void GenerateTokenRejectsAnEmptySecret()
        {
            var act = () => DevJwtTokenGenerator.GenerateToken(string.Empty, "functional-test-user");

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GenerateTokenSignedWithADifferentSecretFailsValidation()
        {
            var token = DevJwtTokenGenerator.GenerateToken(
                Secret,
                subject: "functional-test-user",
                issuer: "gtmotive-functional-tests",
                audience: "gtmotive-estimate-api");

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("a-completely-different-secret-0123456789")),
                ValidateIssuer = true,
                ValidIssuer = "gtmotive-functional-tests",
                ValidateAudience = true,
                ValidAudience = "gtmotive-estimate-api",
                ValidateLifetime = true,
            };

            var act = () => new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);

            act.Should().Throw<SecurityTokenException>();
        }
    }
}
