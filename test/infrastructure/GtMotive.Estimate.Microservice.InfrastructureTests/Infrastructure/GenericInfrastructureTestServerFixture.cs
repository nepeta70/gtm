using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Xunit;

[assembly: CLSCompliant(false)]

namespace GtMotive.Estimate.Microservice.InfrastructureTests.Infrastructure
{
    public sealed class GenericInfrastructureTestServerFixture : IDisposable, IAsyncLifetime
    {
        private readonly string _testDbName;
        private IMongoClient _mongoClient;

        public GenericInfrastructureTestServerFixture()
        {
            _testDbName = $"GtMotive_Test_{Guid.NewGuid():N}";

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
                        ["MongoDb:MongoDbDatabaseName"] = _testDbName
                    });
                })
                .UseStartup<Startup>();

            Server = new TestServer(hostBuilder);
        }

        public TestServer Server { get; }

        public async Task InitializeAsync()
        {
            _mongoClient = Server.Host.Services.GetRequiredService<IMongoClient>();
            await _mongoClient.DropDatabaseAsync(_testDbName);
        }

        public async Task DisposeAsync()
        {
            if (_mongoClient is not null)
            {
                await _mongoClient.DropDatabaseAsync(_testDbName);
            }
        }

        public void Dispose()
        {
            Server?.Dispose();
        }
    }
}
