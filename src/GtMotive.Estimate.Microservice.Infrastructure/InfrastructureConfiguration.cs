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

            AddCoreServices(services);
            AddMongoDb(services);
            AddBuses(services);
            AddEnvironmentSpecificServices(services, isDevelopment);

            return new InfrastructureBuilder(services);
        }

        private static void AddCoreServices(IServiceCollection services)
        {
            services.AddScoped(typeof(IAppLogger<>), typeof(LoggerAdapter<>));
            services.AddAutoMapper(cfg => cfg.AddProfile<VehicleMappingProfile>());
        }

        private static void AddMongoDb(IServiceCollection services)
        {
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
        }

        private static void AddBuses(IServiceCollection services)
        {
            services.AddOptions<BusSettings>();
            services.AddScoped<IBusFactory, BusFactory>();

            services.AddKeyedScoped<IBus, InMemoryBus>(BusNames.InMemory);
            services.AddKeyedScoped<IBus>(BusNames.Azure, (sp, key) =>
            {
                var settings = sp.GetRequiredService<IOptions<BusSettings>>().Value;
                var logger = sp.GetRequiredService<IAppLogger<AzureServiceBus>>();

                return new AzureServiceBus(settings.ConnectionString, settings.DefaultQueueOrTopicName, logger);
            });
        }

        private static void AddEnvironmentSpecificServices(IServiceCollection services, bool isDevelopment)
        {
            var jwtSecret = Environment.GetEnvironmentVariable("Jwt__Secret") ?? Environment.GetEnvironmentVariable("Jwt:Secret");
            var useJwtAuth = !string.IsNullOrEmpty(jwtSecret);

            if (isDevelopment)
            {
                services.AddScoped<IAuthorizationService, NoOpAuthorizationService>();
                services.AddScoped<ITelemetry, NoOpTelemetry>();
                services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();
            }
            else
            {
                if (useJwtAuth)
                {
                    services.AddScoped<IAuthorizationService, JwtAuthorizationService>();
                }
                else
                {
                    services.AddScoped<IAuthorizationService, NoOpAuthorizationService>();
                }

                services.AddScoped<ITelemetry, AppTelemetry>();
                services.AddScoped<IUnitOfWork, MongoUnitOfWork>();
            }
        }

        private sealed class InfrastructureBuilder(IServiceCollection services) : IInfrastructureBuilder
        {
            public IServiceCollection Services { get; } = services;
        }
    }
}
