using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace GtMotive.Estimate.Microservice.InfrastructureTests.Infrastructure
{
    /// <summary>
    /// Spins up a lightweight TestServer wired with the production JWT Bearer authentication
    /// extension. Unlike <see cref="GenericInfrastructureTestServerFixture"/>, this fixture does
    /// not require Mongo/Docker, since it only exercises the authentication pipeline.
    /// </summary>
    public sealed class JwtTestServerFixture : IDisposable, IAsyncLifetime
    {
        public const string Secret = "dev-integration-test-secret-please-change-1234567890";

        public const string Issuer = "gtmotive-tests";

        public const string Audience = "gtmotive-estimate-api";

        private IHost _host;

        public TestServer Server { get; private set; }

        public Task InitializeAsync()
        {
            var hostBuilder = new HostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseEnvironment("IntegrationTest")
                .ConfigureAppConfiguration((_, builder) =>
                {
                    builder.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["Jwt:Secret"] = Secret,
                        ["Jwt:Issuer"] = Issuer,
                        ["Jwt:Audience"] = Audience
                    });
                })
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder.UseTestServer();
                    webHostBuilder.UseStartup<JwtTestStartup>();
                });

            _host = hostBuilder.Build();

            return InitializeHostAsync();
        }

        public async Task DisposeAsync()
        {
            if (_host is not null)
            {
                await _host.StopAsync();
            }
        }

        public void Dispose()
        {
            Server?.Dispose();
            _host?.Dispose();
        }

        private async Task InitializeHostAsync()
        {
            await _host.StartAsync();
            Server = _host.GetTestServer();
        }
    }
}
