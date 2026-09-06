using System;
using System.Diagnostics.CodeAnalysis;
using GtMotive.Estimate.Microservice.Api.Authorization;
using GtMotive.Estimate.Microservice.Api.Behaviors;
using GtMotive.Estimate.Microservice.Api.DependencyInjection;
using GtMotive.Estimate.Microservice.Api.UseCases;
using GtMotive.Estimate.Microservice.ApplicationCore;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases;
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
            RegisterUseCase<CreateVehicleInput>(services);
            RegisterUseCase<ListAvailableVehiclesInput>(services);
            RegisterUseCase<RentVehicleInput>(services);
            RegisterUseCase<ReturnVehicleInput>(services);

            services.AddUseCases()
                .AddPresenters();
            services.AddExceptionHandler<BusinessExceptionHandler>()
                .AddProblemDetails();
        }

        private static void RegisterUseCase<TInput>(IServiceCollection services)
            where TInput : IUseCaseInput
        {
            services.AddTransient<IRequestHandler<UseCaseRequest<TInput>>, UseCaseRequestHandler<TInput>>();

            services.AddTransient<IPipelineBehavior<UseCaseRequest<TInput>, Unit>, UseCaseTelemetryBehavior<TInput>>();
            services.AddTransient<IPipelineBehavior<UseCaseRequest<TInput>, Unit>, DomainEventPublishingBehavior<TInput>>();
        }
    }
}
