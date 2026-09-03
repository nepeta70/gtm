using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GtMotive.Estimate.Microservice.Host.Infrastructure.Swagger
{
    /// <summary>
    /// Adds the active OpenAPI security requirement to operations that require authorization.
    /// Supports both controller actions decorated with <see cref="AuthorizeAttribute"/> and
    /// minimal API endpoints configured with RequireAuthorization().
    /// </summary>
    internal sealed class IdentityServerApiSecurityOperationFilter(string securitySchemeName) : IOperationFilter
    {
        internal static readonly string[] OpenApiSecuritySchemesValues = ["estimate-public"];

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            ArgumentNullException.ThrowIfNull(operation);
            ArgumentNullException.ThrowIfNull(context);

            if (!RequiresAuthorization(context))
            {
                return;
            }

            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });

            operation.Security =
            [
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference(securitySchemeName),
                        OpenApiSecuritySchemesValues.ToList()
                    }
                }
            ];
        }

        private static bool RequiresAuthorization(OperationFilterContext context)
        {
            var endpointMetadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

            // Minimal API / endpoint routing metadata.
            var authorizeData = endpointMetadata?.OfType<IAuthorizeData>() ?? [];
            var allowAnonymous = endpointMetadata?.OfType<IAllowAnonymous>()?.Any() == true;

            if (allowAnonymous)
            {
                return false;
            }

            if (authorizeData.Any())
            {
                return true;
            }

            // Controller-based actions.
            var controllerAttributes = context.MethodInfo.DeclaringType is null
                ? []
                : context.MethodInfo.DeclaringType
                    .GetCustomAttributes(true);

            var methodAttributes = context.MethodInfo
                .GetCustomAttributes(true);

            return !methodAttributes.OfType<AllowAnonymousAttribute>().Any() &&
                !controllerAttributes.OfType<AllowAnonymousAttribute>().Any() && (methodAttributes.OfType<AuthorizeAttribute>().Any() ||
                   controllerAttributes.OfType<AuthorizeAttribute>().Any());
        }
    }
}
