using System;
using Acheve.AspNetCore.TestHost.Security;
using Acheve.TestHost;
using GtMotive.Estimate.Microservice.Api;
using GtMotive.Estimate.Microservice.Api.Endpoints;
using GtMotive.Estimate.Microservice.Domain.Interfaces;
using GtMotive.Estimate.Microservice.Infrastructure;
using GtMotive.Estimate.Microservice.Infrastructure.Authorization;
using GtMotive.Estimate.Microservice.Infrastructure.Bus;
using GtMotive.Estimate.Microservice.Infrastructure.MongoDb.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GtMotive.Estimate.Microservice.InfrastructureTests.Infrastructure
{
    internal sealed class Startup(IWebHostEnvironment environment, IConfiguration configuration)
    {
        public IWebHostEnvironment Environment { get; } = environment;

        public IConfiguration Configuration { get; } = configuration;

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public static void Configure(IApplicationBuilder app)
        {
            app.UseExceptionHandler();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapVehicles();
            });
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<MongoDbSettings>(Configuration.GetSection("MongoDb"));
            services.AddAuthentication(TestServerDefaults.AuthenticationScheme)
                .AddTestServer();
            services.AddBaseInfrastructure(true);
            services.AddKeyedScoped<IBus, InMemoryBus>(BusNames.InMemory);
            services.AddApiDependencies();
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
