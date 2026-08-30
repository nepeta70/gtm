using System;
using System.Diagnostics.CodeAnalysis;
using GtMotive.Estimate.Microservice.Domain.Interfaces;
using GtMotive.Estimate.Microservice.Infrastructure.Authorization;
using GtMotive.Estimate.Microservice.Infrastructure.Bus;
using GtMotive.Estimate.Microservice.Infrastructure.Bus.Settings;
using GtMotive.Estimate.Microservice.Infrastructure.Fleet.MongoDb;
using GtMotive.Estimate.Microservice.Infrastructure.Interfaces;
using GtMotive.Estimate.Microservice.Infrastructure.Logging;
using GtMotive.Estimate.Microservice.Infrastructure.MongoDb.Settings;
using GtMotive.Estimate.Microservice.Infrastructure.Persistence;
using GtMotive.Estimate.Microservice.Infrastructure.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

[assembly: CLSCompliant(false)]

namespace GtMotive.Estimate.Microservice.Infrastructure
{
    public static class InfrastructureConfiguration
    {
        [ExcludeFromCodeCoverage]
        public static IInfrastructureBuilder AddBaseInfrastructure(
            this IServiceCollection services,
            bool isDevelopment)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddScoped(typeof(IAppLogger<>), typeof(LoggerAdapter<>));
            services.AddAutoMapper(cfg => cfg.AddProfile<VehicleMappingProfile>());

            services.AddSingleton<IMongoClient>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
                return new MongoClient(settings.ConnectionString);
            });

            services.AddSingleton(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
                var client = sp.GetRequiredService<IMongoClient>();
                var database = client.GetDatabase(settings.MongoDbDatabaseName);

                VehicleCollectionSetup.EnsureIndexes(database);

                return database;
            });
            services.AddScoped<IVehicleReadRepository, MongoVehicleReadRepository>();
            services.AddScoped<IVehicleWriteRepository, MongoVehicleWriteRepository>();

            // Bus configuration: provider is selected from BusSettings.
            services.AddOptions<BusSettings>();
            services.AddScoped<IBusFactory, BusFactory>();
            services.AddScoped<IBus>(sp => sp.GetRequiredService<IBusFactory>().GetClient(typeof(object)));
            services.AddScoped<NoOpBus>();
            services.AddScoped<InMemoryBus>();
            services.AddScoped<AzureServiceBus>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<BusSettings>>().Value;
                var logger = sp.GetRequiredService<IAppLogger<AzureServiceBus>>();

                return new AzureServiceBus(settings.ConnectionString, settings.DefaultQueueOrTopicName, logger);
            });

            if (!isDevelopment)
            {
                services.AddScoped<ITelemetry, AppTelemetry>();
                services.AddScoped<IUnitOfWork, MongoUnitOfWork>();
                services.AddScoped<IAuthorizationService, NoOpAuthorizationService>();
            }
            else
            {
                services.AddScoped<IAuthorizationService, NoOpAuthorizationService>();
                services.AddScoped<ITelemetry, NoOpTelemetry>();
                services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();
            }

            return new InfrastructureBuilder(services);
        }

        private sealed class InfrastructureBuilder(IServiceCollection services) : IInfrastructureBuilder
        {
            public IServiceCollection Services { get; } = services;
        }
    }
}
