using GtMotive.Estimate.IdentityServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var issuerUri = builder.Configuration.GetValue<string>("IdentityServer:IssuerUri");

// Read allowed origins from appsettings.json
var allowedCorsOrigins = builder.Configuration
    .GetSection("IdentityServer:AllowedCorsOrigins")
    .Get<string[]>() ?? [];

// Add named CORS policy targeting allowed origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("IdentityServerCorsPolicy", policy =>
    {
        policy.WithOrigins(allowedCorsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services
    .AddIdentityServer(options =>
    {
        // When configured (e.g. docker-compose sets IdentityServer__IssuerUri), the issuer
        // is fixed so the browser, the API container and local runs all see the same iss.
        if (!string.IsNullOrWhiteSpace(issuerUri))
        {
            options.IssuerUri = issuerUri;
        }
    })
    .AddInMemoryIdentityResources(IdentityServerConfig.IdentityResources)
    .AddInMemoryApiScopes(IdentityServerConfig.ApiScopes)
    .AddInMemoryApiResources(IdentityServerConfig.ApiResources)
    .AddInMemoryClients(IdentityServerConfig.GetClients(builder.Configuration))
    .AddTestUsers(IdentityServerConfig.GetUsers(builder.Configuration))
    .AddDeveloperSigningCredential();

var app = builder.Build();
app.UseCors("IdentityServerCorsPolicy");

app.UseIdentityServer();

await app.RunAsync();
