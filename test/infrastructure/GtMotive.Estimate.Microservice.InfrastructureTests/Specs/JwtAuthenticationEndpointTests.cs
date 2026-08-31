using System;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FluentAssertions;
using GtMotive.Estimate.Microservice.Infrastructure.Authorization;
using GtMotive.Estimate.Microservice.InfrastructureTests.Infrastructure;
using Xunit;

namespace GtMotive.Estimate.Microservice.InfrastructureTests.Specs
{
    /// <summary>
    /// Exercises the real JWT Bearer authentication wiring
    /// (<see cref="JwtBearerAuthenticationExtensions.TryAddJwtBearerAuthentication"/>) end-to-end
    /// through the ASP.NET Core pipeline, using tokens minted by
    /// <see cref="DevJwtTokenGenerator"/> (the same generator shipped as the dev CLI tool).
    /// </summary>
    [Collection(TestCollections.JwtTestServer)]
    public sealed class JwtAuthenticationEndpointTests(JwtTestServerFixture fixture)
        : JwtInfrastructureTestBase(fixture)
    {
        [Fact]
        public async Task RequestWithoutTokenIsUnauthorized()
        {
            using var client = Fixture.Server.CreateClient();

            using var response = await client.GetAsync(new Uri("/_secure-ping", UriKind.Relative));

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task RequestWithValidTokenIsAuthorized()
        {
            using var client = Fixture.Server.CreateClient();
            var token = DevJwtTokenGenerator.GenerateToken(
                JwtTestServerFixture.Secret,
                subject: "test-user",
                issuer: JwtTestServerFixture.Issuer,
                audience: JwtTestServerFixture.Audience);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await client.GetAsync(new Uri("/_secure-ping", UriKind.Relative));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task RequestWithTokenSignedByWrongSecretIsUnauthorized()
        {
            using var client = Fixture.Server.CreateClient();
            var token = DevJwtTokenGenerator.GenerateToken(
                "a-completely-different-secret-0123456789",
                subject: "test-user",
                issuer: JwtTestServerFixture.Issuer,
                audience: JwtTestServerFixture.Audience);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await client.GetAsync(new Uri("/_secure-ping", UriKind.Relative));

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task RequestWithExpiredTokenIsUnauthorized()
        {
            using var client = Fixture.Server.CreateClient();
            var token = DevJwtTokenGenerator.GenerateToken(
                JwtTestServerFixture.Secret,
                subject: "test-user",
                issuer: JwtTestServerFixture.Issuer,
                audience: JwtTestServerFixture.Audience,
                lifetime: TimeSpan.FromMilliseconds(1));

            await Task.Delay(TimeSpan.FromMilliseconds(50));

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await client.GetAsync(new Uri("/_secure-ping", UriKind.Relative));

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task RequestWithWrongAudienceIsUnauthorized()
        {
            using var client = Fixture.Server.CreateClient();
            var token = DevJwtTokenGenerator.GenerateToken(
                JwtTestServerFixture.Secret,
                subject: "test-user",
                issuer: JwtTestServerFixture.Issuer,
                audience: "some-other-audience");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await client.GetAsync(new Uri("/_secure-ping", UriKind.Relative));

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task RequestWithoutRoleClaimIsForbiddenOnAdminEndpoint()
        {
            using var client = Fixture.Server.CreateClient();
            var token = DevJwtTokenGenerator.GenerateToken(
                JwtTestServerFixture.Secret,
                subject: "test-user",
                issuer: JwtTestServerFixture.Issuer,
                audience: JwtTestServerFixture.Audience);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await client.GetAsync(new Uri("/_secure-admin", UriKind.Relative));

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task RequestWithAdminRoleClaimIsAuthorizedOnAdminEndpoint()
        {
            using var client = Fixture.Server.CreateClient();
            var token = DevJwtTokenGenerator.GenerateToken(
                JwtTestServerFixture.Secret,
                subject: "test-admin",
                roles: ["Admin"],
                issuer: JwtTestServerFixture.Issuer,
                audience: JwtTestServerFixture.Audience);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await client.GetAsync(new Uri("/_secure-admin", UriKind.Relative));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
