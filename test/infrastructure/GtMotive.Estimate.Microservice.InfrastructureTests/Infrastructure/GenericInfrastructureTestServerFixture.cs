using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        public GenericInfrastructureTestServerFixture()
        {
            _testDbName = $"GtMotive_Test_{Guid.NewGuid():N}";
            _mongoContainer = new MongoDbBuilder().Build();
        }

        public TestServer Server { get; private set; }

        public async Task InitializeAsync()
        {
            await _mongoContainer.StartAsync();

            var hostBuilder = new WebHostBuilder()
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
                        ["MongoDb:MongoDbDatabaseName"] = _testDbName
                    });
                })
                .UseStartup<Startup>();

            Server = new TestServer(hostBuilder);

            _mongoClient = Server.Host.Services.GetRequiredService<IMongoClient>();
            await _mongoClient.DropDatabaseAsync(_testDbName);
        }

        public async Task DisposeAsync()
        {
            if (_mongoClient is not null)
            {
                await _mongoClient.DropDatabaseAsync(_testDbName);
            }

            await _mongoContainer.DisposeAsync();
        }

        public void Dispose()
        {
            Server?.Dispose();
        }
    }
}
