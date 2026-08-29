using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using GtMotive.Estimate.Microservice.InfrastructureTests.Infrastructure;
using Xunit;

namespace GtMotive.Estimate.Microservice.InfrastructureTests.Specs
{
    /// <summary>
    /// REST integration tests at host level: goes through the real ASP.NET Core pipeline
    /// (TestServer), routing, model binding and endpoint exception handling,
    /// without needing a running network host.
    /// </summary>
    public sealed class VehicleEndpointTests(GenericInfrastructureTestServerFixture fixture)
        : InfrastructureTestBase(fixture)
    {
        [Fact]
        public async Task PostVehiclesWithValidDataReturnsCreatedVehicle()
        {
            using var client = Fixture.Server.CreateClient();

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
            using var client = Fixture.Server.CreateClient();

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
    }
}
