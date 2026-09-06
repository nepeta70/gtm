using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GtMotive.Estimate.Microservice.Api;
using GtMotive.Estimate.Microservice.Domain.Interfaces;
using GtMotive.Estimate.Microservice.Infrastructure;
using GtMotive.Estimate.Microservice.Infrastructure.Bus;
using GtMotive.Estimate.Microservice.Infrastructure.MongoDb.Settings;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Xunit;

[assembly: CLSCompliant(false)]

namespace GtMotive.Estimate.Microservice.FunctionalTests.Infrastructure
{
    public sealed class CompositionRootTestFixture : IDisposable, IAsyncLifetime
    {
        private readonly string _testDbName;
        private readonly MongoDbContainer _mongoContainer;

        private ServiceProvider _serviceProvider;
        private IMongoClient _mongoClient;

        public CompositionRootTestFixture()
        {
            _testDbName = $"GtMotive_Test_{Guid.NewGuid():N}";
            _mongoContainer = new MongoDbBuilder("mongo:7.0").Build();
        }

        public IConfiguration Configuration { get; private set; }

        public async Task InitializeAsync()
        {
            await _mongoContainer.StartAsync();

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["MongoDb:ConnectionString"] = _mongoContainer.GetConnectionString(),
                    ["MongoDb:DatabaseName"] = _testDbName,
                    ["MongoDb:MongoDbDatabaseName"] = _testDbName
                })
                .Build();

            Configuration = configuration;

            using (var tempClient = new MongoClient(_mongoContainer.GetConnectionString()))
            {
                await tempClient.DropDatabaseAsync(_testDbName);
            }

            var services = new ServiceCollection();
            ConfigureServices(services);
            services.AddSingleton<IConfiguration>(configuration);
            services.Configure<MongoDbSettings>(Configuration.GetSection("MongoDb"));

            _serviceProvider = services.BuildServiceProvider();

            _mongoClient = _serviceProvider.GetRequiredService<IMongoClient>();
            _ = _serviceProvider.GetRequiredService<IMongoDatabase>();
        }

        public async Task UsingHandlerForRequest<TRequest>(Func<IRequestHandler<TRequest, Unit>, Task> handlerAction)
            where TRequest : IRequest<Unit>
        {
            ArgumentNullException.ThrowIfNull(handlerAction);

            using var scope = _serviceProvider.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<TRequest, Unit>>();

            await handlerAction(handler);
        }

        public async Task UsingHandlerForRequestResponse<TRequest, TResponse>(Func<IRequestHandler<TRequest, TResponse>, Task> handlerAction)
            where TRequest : IRequest<TResponse>
        {
            ArgumentNullException.ThrowIfNull(handlerAction);

            using var scope = _serviceProvider.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

            await handlerAction(handler);
        }

        public async Task UsingRepository<TRepository>(Func<TRepository, Task> handlerAction)
        {
            ArgumentNullException.ThrowIfNull(handlerAction);

            using var scope = _serviceProvider.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<TRepository>();

            await handlerAction(handler);
        }

        public async Task UsingScope(Func<IServiceProvider, Task> scopedAction)
        {
            ArgumentNullException.ThrowIfNull(scopedAction);

            using var scope = _serviceProvider.CreateScope();

            await scopedAction(scope.ServiceProvider);
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
            _mongoClient?.Dispose();
        }

        public async Task DisposeAsync()
        {
            if (_mongoClient is not null)
            {
                await _mongoClient.DropDatabaseAsync(_testDbName);
            }

            if (_serviceProvider is not null)
            {
                await _serviceProvider.DisposeAsync();
            }

            await _mongoContainer.DisposeAsync();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddApiDependencies();
            services.AddLogging();
            services.AddBaseInfrastructure(true);

            services.AddScoped<IBusFactory, BusFactory>();
            services.AddKeyedScoped<IBus, InMemoryBus>(BusNames.InMemory);
        }
    }
}
