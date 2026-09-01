using System;
using System.Collections.Generic;
using System.Threading;
using GtMotive.Estimate.Microservice.Api.Authorization;
using GtMotive.Estimate.Microservice.Api.Resilience;
using GtMotive.Estimate.Microservice.Api.UseCases.CreateVehicle;
using GtMotive.Estimate.Microservice.Api.UseCases.ListAvailableVehicles;
using GtMotive.Estimate.Microservice.Api.UseCases.RentVehicle;
using GtMotive.Estimate.Microservice.Api.UseCases.ReturnVehicle;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.CreateVehicle.Models;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ListAvailableVehicles.Models;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.RentVehicle.Models;
using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ReturnVehicle.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GtMotive.Estimate.Microservice.Api.Endpoints
{
    public static class VehicleEndpoints
    {
        public static IEndpointRouteBuilder MapVehicles(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("api/vehicles")
                           .WithTags("Vehicles")
                           .RequireAuthorization()
                           .RequireRateLimiting(ApiResiliencePolicyNames.EndpointConcurrency);

            group.MapPost(string.Empty, async (
                CreateVehicleRequest request,
                IUseCase<CreateVehicleInput> useCase,
                CreateVehiclePresenter presenter,
                CancellationToken ct) =>
            {
                var input = new CreateVehicleInput(
                    request.Brand,
                    request.Model,
                    request.LicensePlate,
                    request.ManufactureDate);

                await useCase.Execute(input, ct);

                return presenter.Result;
            })
            .RequireAuthorization(AuthorizationPolicies.CanCreateVehicle)
            .WithName("CreateVehicle")
            .WithSummary("Registers a new vehicle in the fleet")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

            group.MapGet("available", async (
                IUseCase<ListAvailableVehiclesInput> useCase,
                ListAvailableVehiclesPresenter presenter,
                CancellationToken ct) =>
            {
                await useCase.Execute(new ListAvailableVehiclesInput(), ct);

                return presenter.Result;
            })
            .WithName("ListAvailableVehicles")
            .WithSummary("Lists vehicles currently available for rent")
            .Produces<IReadOnlyCollection<AvailableVehicleDto>>(StatusCodes.Status200OK);

            group.MapPost("{id:guid}/rent", async (
                Guid id,
                RentVehicleRequest request,
                IUseCase<RentVehicleInput> useCase,
                RentVehiclePresenter presenter,
                CancellationToken ct) =>
            {
                var input = new RentVehicleInput(id, request.RenterId);

                await useCase.Execute(input, ct);

                return presenter.Result;
            })
            .RequireAuthorization(AuthorizationPolicies.CanRentVehicle)
            .WithName("RentVehicle")
            .WithSummary("Rents an available vehicle")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

            group.MapPost("{id:guid}/return", async (
                Guid id,
                IUseCase<ReturnVehicleInput> useCase,
                ReturnVehiclePresenter presenter,
                CancellationToken ct) =>
            {
                var input = new ReturnVehicleInput(id);

                await useCase.Execute(input, ct);

                return presenter.Result;
            })
            .RequireAuthorization(AuthorizationPolicies.CanRentVehicle)
            .WithName("ReturnVehicle")
            .WithSummary("Returns a previously rented vehicle")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

            return app;
        }
    }
}
