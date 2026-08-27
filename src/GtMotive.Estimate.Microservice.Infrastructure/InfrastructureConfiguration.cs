using System;
using System.Diagnostics.CodeAnalysis;
using GtMotive.Estimate.Microservice.Domain.Interfaces;
using GtMotive.Estimate.Microservice.Infrastructure.Fleet.MongoDb;
using GtMotive.Estimate.Microservice.Infrastructure.Interfaces;
using GtMotive.Estimate.Microservice.Infrastructure.Logging;
using GtMotive.Estimate.Microservice.Infrastructure.MongoDb.Settings;
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

            if (!isDevelopment)
            {
                services.AddScoped<ITelemetry, AppTelemetry>();

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

                services.AddScoped<IVehicleWriteRepository, MongoVehicleRepository>();
            }
            else
            {
                services.AddScoped<ITelemetry, NoOpTelemetry>();
            }

            return new InfrastructureBuilder(services);
        }

        private sealed class InfrastructureBuilder(IServiceCollection services) : IInfrastructureBuilder
        {
            public IServiceCollection Services { get; } = services;
        }
    }
}
