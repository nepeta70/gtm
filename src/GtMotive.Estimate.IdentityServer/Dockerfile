FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY *.sln ./
COPY Directory.Build.* ./
COPY src/GtMotive.Estimate.IdentityServer/*.csproj src/GtMotive.Estimate.IdentityServer/

RUN dotnet restore src/GtMotive.Estimate.IdentityServer/GtMotive.Estimate.IdentityServer.csproj

COPY . .

RUN dotnet publish src/GtMotive.Estimate.IdentityServer/GtMotive.Estimate.IdentityServer.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=build --chown=app /app/publish .

# The developer signing credential (tempkey.jwk) is written to the content root.
RUN chown app /app

USER app

ENV ASPNETCORE_URLS=http://+:5001

ENTRYPOINT ["dotnet", "GtMotive.Estimate.IdentityServer.dll"]
