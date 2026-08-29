using Acheve.AspNetCore.TestHost.Security;
using Acheve.TestHost;
using GtMotive.Estimate.Microservice.Api;
using GtMotive.Estimate.Microservice.Api.Endpoints;
using GtMotive.Estimate.Microservice.Infrastructure;
using GtMotive.Estimate.Microservice.Infrastructure.MongoDb.Settings;
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
            services.AddControllers(ApiConfiguration.ConfigureControllers)
                .WithApiControllers();
            services.AddBaseInfrastructure(true);
        }
    }
}
