using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GtMotive.Estimate.Microservice.Api;
using GtMotive.Estimate.Microservice.Infrastructure;
using GtMotive.Estimate.Microservice.Infrastructure.MongoDb.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Xunit;

[assembly: CLSCompliant(false)]

namespace GtMotive.Estimate.Microservice.FunctionalTests.Infrastructure
{
    public sealed class CompositionRootTestFixture : IDisposable, IAsyncLifetime
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly IMongoClient _mongoClient;
        private readonly string _testDbName;

        public CompositionRootTestFixture()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var services = new ServiceCollection();
            Configuration = configuration;
            ConfigureServices(services);
            services.AddSingleton<IConfiguration>(configuration);
            services.Configure<MongoDbSettings>(Configuration.GetSection("MongoDb"));

            _testDbName = $"GtMotive_Test_{Guid.NewGuid():N}";

            var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMongoDatabase));
            if (dbDescriptor is not null)
            {
                services.Remove(dbDescriptor);
            }

            services.AddSingleton(sp =>
                sp.GetRequiredService<IMongoClient>().GetDatabase(_testDbName));

            _serviceProvider = services.BuildServiceProvider();
            _mongoClient = _serviceProvider.GetRequiredService<IMongoClient>();
        }

        public IConfiguration Configuration { get; }

        public async Task InitializeAsync()
        {
            await _mongoClient.DropDatabaseAsync(_testDbName);
        }

        public async Task DisposeAsync()
        {
            await _mongoClient.DropDatabaseAsync(_testDbName);
        }

        public async Task UsingRepository<TRepository>(Func<TRepository, Task> handlerAction)
        {
            ArgumentNullException.ThrowIfNull(handlerAction);

            using var scope = _serviceProvider.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<TRepository>();

            if (handler == null)
            {
                Debug.Fail("The requested handler has not been registered");
            }

            await handlerAction.Invoke(handler);
        }

        public async Task UsingScope(Func<IServiceProvider, Task> scopedAction)
        {
            ArgumentNullException.ThrowIfNull(scopedAction);

            using var scope = _serviceProvider.CreateScope();

            await scopedAction.Invoke(scope.ServiceProvider);
        }

        public void Dispose()
        {
            _serviceProvider.Dispose();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddApiDependencies();
            services.AddLogging();
            services.AddBaseInfrastructure(true);
        }
    }
}
