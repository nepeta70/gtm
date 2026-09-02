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
using GtMotive.Estimate.Microservice.Infrastructure.Resilience;
using GtMotive.Estimate.Microservice.Infrastructure.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Polly;

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

            return new InfrastructureBuilder(services, isDevelopment)
                .AddCoreServices()
                .AddResilience()
                .AddMongoDb()
                .AddBuses()
                .AddAuthorizationServices()
                .AddTelemetryServices();
        }

        private sealed class InfrastructureBuilder(IServiceCollection services, bool isDevelopment) : IInfrastructureBuilder
        {
            public IServiceCollection Services { get; } = services;

            public bool IsDevelopment { get; } = isDevelopment;

            public InfrastructureBuilder AddCoreServices()
            {
                Services.AddScoped(typeof(IAppLogger<>), typeof(LoggerAdapter<>));
                Services.AddAutoMapper(cfg => cfg.AddProfile<VehicleMappingProfile>());
                return this;
            }

            public InfrastructureBuilder AddResilience()
            {
                Services.AddResiliencePipelines();
                return this;
            }

            public InfrastructureBuilder AddMongoDb()
            {
                Services.AddSingleton<IMongoClient>(sp =>
                {
                    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
                    return new MongoClient(settings.ConnectionString);
                });

                Services.AddSingleton(sp =>
                {
                    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
                    var client = sp.GetRequiredService<IMongoClient>();

                    if (IsDevelopment)
                    {
                        client.DropDatabase(settings.MongoDbDatabaseName);
                    }

                    var database = client.GetDatabase(settings.MongoDbDatabaseName);
                    var pipeline = sp.GetRequiredKeyedService<ResiliencePipeline>(ResiliencePipelineNames.Mongo);
                    VehicleCollectionSetup.EnsureIndexes(database, pipeline);

                    return database;
                });

                Services.AddScoped<IUnitOfWork, MongoUnitOfWork>();
                Services.AddScoped<IVehicleReadRepository, MongoVehicleReadRepository>();
                Services.AddScoped<IVehicleWriteRepository, MongoVehicleWriteRepository>();
                return this;
            }

            public InfrastructureBuilder AddBuses()
            {
                Services.AddOptions<BusSettings>();
                Services.AddScoped<IBusFactory, BusFactory>();

                Services.AddKeyedScoped<IBus, InMemoryBus>(BusNames.InMemory);
                Services.AddKeyedScoped<IBus>(BusNames.Azure, (sp, key) =>
                {
                    var settings = sp.GetRequiredService<IOptions<BusSettings>>().Value;
                    var logger = sp.GetRequiredService<IAppLogger<AzureServiceBus>>();
                    var pipeline = sp.GetRequiredKeyedService<ResiliencePipeline>(ResiliencePipelineNames.ServiceBus);

                    return new AzureServiceBus(settings.ConnectionString, settings.DefaultQueueOrTopicName, logger, pipeline);
                });

                return this;
            }

            public InfrastructureBuilder AddAuthorizationServices()
            {
                var jwtSecret = Environment.GetEnvironmentVariable("Jwt__Secret")
                                ?? Environment.GetEnvironmentVariable("Jwt:Secret");

                var useJwtAuth = !IsDevelopment && !string.IsNullOrEmpty(jwtSecret);

                if (useJwtAuth)
                {
                    Services.AddScoped<IAuthorizationService, JwtAuthorizationService>();
                }
                else
                {
                    Services.AddScoped<IAuthorizationService, NoOpAuthorizationService>();
                }

                return this;
            }

            public InfrastructureBuilder AddTelemetryServices()
            {
                if (IsDevelopment)
                {
                    Services.AddScoped<ITelemetry, NoOpTelemetry>();
                }
                else
                {
                    Services.AddScoped<ITelemetry, AppTelemetry>();
                }

                return this;
            }
        }
    }
}
