using System;
using GtMotive.Estimate.Microservice.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GtMotive.Estimate.Microservice.InfrastructureTests.Infrastructure
{
    /// <summary>
    /// Minimal TestServer startup used to exercise the production JWT Bearer authentication
    /// wiring (<see cref="JwtBearerAuthenticationExtensions"/>) end-to-end, without depending on
    /// Mongo/Docker. It maps two test-only endpoints that are never exposed by the real API.
    /// </summary>
    internal sealed class JwtTestStartup(IWebHostEnvironment environment, IConfiguration configuration)
    {
        public IWebHostEnvironment Environment { get; } = environment;

        public IConfiguration Configuration { get; } = configuration;

        public static void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/_secure-ping", () => Results.Ok("pong"))
                    .RequireAuthorization();

                endpoints.MapGet("/_secure-admin", () => Results.Ok("admin-pong"))
                    .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });
            });
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
            services.AddAuthorization();
            services.TryAddJwtBearerAuthentication(Configuration, requireHttpsMetadata: false);

            // Test-only: disable the default 5-minute clock skew tolerance so expired-token
            // assertions do not need to wait several minutes for a real expiry to register.
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options => options.TokenValidationParameters.ClockSkew = TimeSpan.Zero);
        }
    }
}
