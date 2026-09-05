using System;
using System.Diagnostics.CodeAnalysis;
using GtMotive.Estimate.Microservice.Api.Authorization;
using GtMotive.Estimate.Microservice.Api.DependencyInjection;
using GtMotive.Estimate.Microservice.Api.UseCases;
using GtMotive.Estimate.Microservice.ApplicationCore;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.CreateVehicle.Models;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ListAvailableVehicles.Models;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.RentVehicle.Models;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ReturnVehicle.Models;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

[assembly: CLSCompliant(false)]

namespace GtMotive.Estimate.Microservice.Api
{
    [ExcludeFromCodeCoverage]
    public static class ApiConfiguration
    {
        public static void AddApiDependencies(this IServiceCollection services)
        {
            services.AddRouting();
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
