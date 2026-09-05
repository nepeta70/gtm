using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using GtMotive.Estimate.Microservice.Api.Authorization;
using GtMotive.Estimate.Microservice.Api.DependencyInjection;
using GtMotive.Estimate.Microservice.Api.Filters;
using GtMotive.Estimate.Microservice.Api.UseCases;
using GtMotive.Estimate.Microservice.ApplicationCore;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.CreateVehicle.Models;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ListAvailableVehicles.Models;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.RentVehicle.Models;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ReturnVehicle.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

[assembly: CLSCompliant(false)]

namespace GtMotive.Estimate.Microservice.Api
{
    [ExcludeFromCodeCoverage]
    public static class ApiConfiguration
    {
        public static void ConfigureControllers(MvcOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            options.Filters.Add<BusinessExceptionFilter>();
        }

        public static IMvcBuilder WithApiControllers(this IMvcBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.AddApplicationPart(typeof(ApiConfiguration).GetTypeInfo().Assembly);

            AddApiDependencies(builder.Services);

            return builder;
        }

        public static void AddApiDependencies(this IServiceCollection services)
        {
            services.AddAuthorization(AuthorizationOptionsExtensions.Configure);
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(UseCaseRequest<>).Assembly);
            });
            services.AddTransient<IRequestHandler<UseCaseRequest<CreateVehicleInput>>, UseCaseRequestHandler<CreateVehicleInput>>();
            services.AddTransient<IRequestHandler<UseCaseRequest<ListAvailableVehiclesInput>>, UseCaseRequestHandler<ListAvailableVehiclesInput>>();
            services.AddTransient<IRequestHandler<UseCaseRequest<RentVehicleInput>>, UseCaseRequestHandler<RentVehicleInput>>();
            services.AddTransient<IRequestHandler<UseCaseRequest<ReturnVehicleInput>>, UseCaseRequestHandler<ReturnVehicleInput>>();

            services.AddUseCases()
                .AddPresenters();
            services.AddExceptionHandler<BusinessExceptionHandler>()
                .AddProblemDetails();
        }
    }
}
