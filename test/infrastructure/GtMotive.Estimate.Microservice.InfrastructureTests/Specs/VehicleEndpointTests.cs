using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using GtMotive.Estimate.Microservice.Api.Authorization;
using GtMotive.Estimate.Microservice.InfrastructureTests.Infrastructure;
using Xunit;

namespace GtMotive.Estimate.Microservice.InfrastructureTests.Specs
{
    /// <summary>
    /// REST integration tests at host level: goes through the real ASP.NET Core pipeline
    /// (TestServer), routing, model binding and endpoint exception handling,
    /// without needing a running network host.
    /// </summary>
    [Collection(TestCollections.TestServer)]
    public sealed class VehicleEndpointTests(GenericInfrastructureTestServerFixture fixture)
        : InfrastructureTestBase(fixture)
    {
        [Fact]
        public async Task PostVehiclesWithValidDataReturnsCreatedVehicle()
        {
            using var client = CreateAuthenticatedClient();

            var request = new
            {
                Brand = "Toyota",
                Model = "Corolla",
                LicensePlate = $"TEST-{Guid.NewGuid():N}"[..12],
                ManufactureDate = DateTime.UtcNow.AddYears(-1)
            };

            using var response = await client.PostAsJsonAsync("/api/vehicles", request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var vehicleId = await response.Content.ReadFromJsonAsync<Guid>();
            vehicleId.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public async Task PostVehiclesWithManufactureDateOlderThanFiveYearsReturnsBadRequest()
        {
            using var client = CreateAuthenticatedClient();

            var request = new
            {
                Brand = "Toyota",
                Model = "Corolla",
                LicensePlate = $"OLD-{Guid.NewGuid():N}"[..12],
                ManufactureDate = DateTime.UtcNow.AddYears(-6)
            };

            using var response = await client.PostAsJsonAsync("/api/vehicles", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task RentAndReturnFlowReturnsOk()
        {
            using var client = CreateAuthenticatedClient();

            var request = new
            {
                Brand = "Toyota",
                Model = "Corolla",
                LicensePlate = $"TEST-{Guid.NewGuid():N}"[..12],
                ManufactureDate = DateTime.Today.AddYears(-1)
            };

            using var createResp = await client.PostAsJsonAsync("/api/vehicles", request);
            createResp.StatusCode.Should().Be(HttpStatusCode.Created);

            var vehicleId = await createResp.Content.ReadFromJsonAsync<Guid>();

            using var rentResp = await client.PostAsJsonAsync($"/api/vehicles/{vehicleId}/rent", new { RenterId = "renter-100" });
            rentResp.StatusCode.Should().Be(HttpStatusCode.OK);

            using var returnResp = await client.PostAsJsonAsync($"/api/vehicles/{vehicleId}/return", new { });
            returnResp.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task DuplicateLicenseReturnsConflict()
        {
            using var client = CreateAuthenticatedClient();

            var plate = $"DUP-{Guid.NewGuid():N}"[..12];

            var request = new
            {
                Brand = "Toyota",
                Model = "Corolla",
                LicensePlate = plate,
                ManufactureDate = DateTime.Today.AddYears(-1)
            };

            using var first = await client.PostAsJsonAsync("/api/vehicles", request);
            first.StatusCode.Should().Be(HttpStatusCode.Created);

            using var second = await client.PostAsJsonAsync("/api/vehicles", request);
            second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task SamePersonCannotRentMoreThanOneVehicleReturnsBadRequest()
        {
            using var client = CreateAuthenticatedClient();

            var plate1 = $"R1-{Guid.NewGuid():N}"[..12];
            var plate2 = $"R2-{Guid.NewGuid():N}"[..12];

            var req1 = new { Brand = "Toyota", Model = "Corolla", LicensePlate = plate1, ManufactureDate = DateTime.Today.AddYears(-1) };
            var req2 = new { Brand = "Seat", Model = "Leon", LicensePlate = plate2, ManufactureDate = DateTime.Today.AddYears(-1) };

            using var c1 = await client.PostAsJsonAsync("/api/vehicles", req1);
            c1.StatusCode.Should().Be(HttpStatusCode.Created);
            var id1 = await c1.Content.ReadFromJsonAsync<Guid>();

            using var c2 = await client.PostAsJsonAsync("/api/vehicles", req2);
            c2.StatusCode.Should().Be(HttpStatusCode.Created);
            var id2 = await c2.Content.ReadFromJsonAsync<Guid>();

            using var rent1 = await client.PostAsJsonAsync($"/api/vehicles/{id1}/rent", new { RenterId = "same-renter" });
            rent1.StatusCode.Should().Be(HttpStatusCode.OK);

            using var rent2 = await client.PostAsJsonAsync($"/api/vehicles/{id2}/rent", new { RenterId = "same-renter" });
            rent2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        private HttpClient CreateAuthenticatedClient()
        {
            // No longer needs IConfiguration. Just pass the roles.
            return Fixture.Server.CreateClient()
                .WithTestToken(AuthorizationRoles.Admin, AuthorizationRoles.User);
        }
    }
}
