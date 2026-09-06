using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Xunit;

[assembly: CLSCompliant(false)]

namespace GtMotive.Estimate.Microservice.InfrastructureTests.Infrastructure
{
    public sealed class GenericInfrastructureTestServerFixture : IDisposable, IAsyncLifetime
    {
        private readonly string _testDbName;
        private readonly MongoDbContainer _mongoContainer;
        private IMongoClient _mongoClient;
        private IHost _host;

        public GenericInfrastructureTestServerFixture()
        {
            _testDbName = $"GtMotive_Test_{Guid.NewGuid():N}";
            _mongoContainer = new MongoDbBuilder("mongo:7.0").Build();
        }

        public TestServer Server { get; private set; }

        public async Task InitializeAsync()
        {
            await _mongoContainer.StartAsync();

            using (var tempClient = new MongoClient(_mongoContainer.GetConnectionString()))
            {
                await tempClient.DropDatabaseAsync(_testDbName);
            }

            var hostBuilder = new HostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseEnvironment("IntegrationTest")
                .UseDefaultServiceProvider(options => { options.ValidateScopes = true; })
                .ConfigureAppConfiguration((context, builder) =>
                {
                    builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    builder.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    builder.AddEnvironmentVariables();
                    builder.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["MongoDb:ConnectionString"] = _mongoContainer.GetConnectionString(),
                        ["MongoDb:MongoDbDatabaseName"] = _testDbName,
                        ["Jwt:Secret"] = JwtTestServerFixture.Secret,
                        ["Jwt:Issuer"] = JwtTestServerFixture.Issuer,
                        ["Jwt:Audience"] = JwtTestServerFixture.Audience
                    });
                })
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder.UseTestServer();
                    webHostBuilder.UseStartup<Startup>();
                });

            _host = hostBuilder.Build();
            await _host.StartAsync();

            Server = _host.GetTestServer();

            _mongoClient = _host.Services.GetRequiredService<IMongoClient>();
            _ = _host.Services.GetRequiredService<IMongoDatabase>();
        }

        public async Task DisposeAsync()
        {
            if (_mongoClient is not null)
            {
                await _mongoClient.DropDatabaseAsync(_testDbName);
            }

            if (_host is not null)
            {
                await _host.StopAsync();
            }

            await _mongoContainer.DisposeAsync();
        }

        public void Dispose()
        {
            _mongoClient?.Dispose();
            Server?.Dispose();
            _host?.Dispose();
        }
    }
}
