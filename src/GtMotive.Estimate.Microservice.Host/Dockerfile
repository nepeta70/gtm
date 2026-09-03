FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 53573

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY *.sln ./
COPY Directory.Build.* ./
COPY src/GtMotive.Estimate.Microservice.Domain/*.csproj src/GtMotive.Estimate.Microservice.Domain/
COPY src/GtMotive.Estimate.Microservice.ApplicationCore/*.csproj src/GtMotive.Estimate.Microservice.ApplicationCore/
COPY src/GtMotive.Estimate.Microservice.Infrastructure/*.csproj src/GtMotive.Estimate.Microservice.Infrastructure/
COPY src/GtMotive.Estimate.Microservice.Api/*.csproj src/GtMotive.Estimate.Microservice.Api/
COPY src/GtMotive.Estimate.Microservice.Host/*.csproj src/GtMotive.Estimate.Microservice.Host/

RUN dotnet restore src/GtMotive.Estimate.Microservice.Host/GtMotive.Estimate.Microservice.Host.csproj

COPY . .

RUN dotnet publish src/GtMotive.Estimate.Microservice.Host/GtMotive.Estimate.Microservice.Host.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app

USER app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:53573

ENTRYPOINT ["dotnet", "GtMotive.Estimate.Microservice.Host.dll"]