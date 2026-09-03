# GT Motive — Vehicle Fleet Rental Microservice

Microservice for managing a vehicle rental fleet, built on the Clean Architecture / Hexagonal Architecture template provided for the GT Motive technical test.

## Objective

Implement a microservice that allows a rental company to:

- Register new vehicles in the fleet
- List available vehicles
- Rent a vehicle
- Return a vehicle

### Business rules

| Rule | Enforcement |
|------|-------------|
| A person cannot have more than one active rental at a time | `RentVehicleUseCase` checks `IVehicleReadRepository.HasActiveRentalAsync`; MongoDB sparse unique index on `renterId` |
| Fleet vehicles cannot be older than 5 years | `Vehicle` aggregate constructor; `MongoVehicleReadRepository` filters aged vehicles from the available list |

## Solution structure

```
src/
├── GtMotive.Estimate.Microservice.Domain          # Entities, value objects, repository ports
├── GtMotive.Estimate.Microservice.ApplicationCore # Use cases (application services)
├── GtMotive.Estimate.Microservice.Infrastructure    # MongoDB adapters, logging, telemetry, bus
├── GtMotive.Estimate.Microservice.Api               # Minimal API endpoints and presenters
├── GtMotive.Estimate.Microservice.Host              # Composition root, Docker entry point
└── GtMotive.Estimate.IdentityServer                 # Dev/demo IdentityServer (Duende) for real JWT issuance

test/
├── unit/            # Domain and use-case tests (mocked dependencies)
├── functional/      # Integration tests without HTTP host
└── infrastructure/  # REST tests through TestServer
```

## Architecture

The solution follows the template's ports-and-adapters style:

- **Domain** — `Vehicle` aggregate root, `VehicleReadModel` query projection, `LicensePlate` value object, `IVehicleWriteRepository` / `IVehicleReadRepository`
- **Application** — Four use cases wired through `IUseCase<TInput>` with output ports
- **Infrastructure** — MongoDB persistence with AutoMapper mapping; `NoOpUnitOfWork` / `MongoUnitOfWork`; `NoOpBus` for domain events
- **API** — Minimal API endpoints (`VehicleEndpoints`) with presenter adapters implementing output ports

### Read / write separation

Persistence is split into command and query repositories:

| Port | Responsibility |
|------|----------------|
| `IVehicleWriteRepository` | `AddAsync`, `UpdateAsync`, `GetByIdAsync` — returns full `Vehicle` aggregates |
| `IVehicleReadRepository` | `GetAvailableAsync`, `HasActiveRentalAsync` — returns `VehicleReadModel` projections |

## API endpoints

All endpoints are under `api/vehicles` and exposed via minimal APIs in `VehicleEndpoints.cs`.

| Method | Route | Description | Success |
|--------|-------|-------------|---------|
| `POST` | `/api/vehicles` | Register a new vehicle | `201 Created` (vehicle id) |
| `GET` | `/api/vehicles/available` | List available vehicles | `200 OK` |
| `POST` | `/api/vehicles/{id}/rent` | Rent a vehicle (`{ "renterId": "..." }`) | `200 OK` / `404` / `400` |
| `POST` | `/api/vehicles/{id}/return` | Return a rented vehicle | `200 OK` / `404` / `400` |

Domain rule violations (`DomainException`) are translated to HTTP `400 Bad Request` by `DomainExceptionHandler`.

### Create vehicle request body

```json
{
  "brand": "Toyota",
  "model": "Corolla",
  "licensePlate": "1234-ABC",
  "manufactureDate": "2024-01-15T00:00:00Z"
}
```

## Persistence

Vehicles are stored in MongoDB (`vehicles` collection). Indexes are created at startup:

- Unique index on `licensePlate`
- Sparse unique index on `renterId` (enforces one active rental per person at DB level)

Configuration via `MongoDb` settings:

```json
{
  "MongoDb": {
    "ConnectionString": "mongodb://localhost:27017/gtmotive-estimate",
    "MongoDbDatabaseName": "gtmotive-estimate"
  }
}
```

In Development, `appsettings.Development.json` does not include Mongo settings — use environment variables, user secrets, or Docker Compose (see below).

## Running locally

### Option 1 — Docker Compose (recommended)

No external dependencies need to be installed on the host. Docker Compose starts the API, MongoDB, the Service Bus emulator and the IdentityServer:

```bash
docker compose up --build
```

- API: `http://localhost:53573`
- Swagger: `http://localhost:53573/swagger`
- IdentityServer: `http://host.docker.internal:5001` (discovery: `/.well-known/openid-configuration`)
- MongoDB: `localhost:27017`

### Option 2 — dotnet run

Requires a running MongoDB instance. Set connection settings via environment variables or user secrets, then:

```bash
dotnet run --project src/GtMotive.Estimate.Microservice.Host
```

### Visual Studio

The Host project references `docker-compose.dcproj`, allowing Docker Compose to be selected as the startup project.

## Docker

| File | Purpose |
|------|---------|
| `src/GtMotive.Estimate.Microservice.Host/Dockerfile` | Multi-stage build on official `mcr.microsoft.com/dotnet` images (.NET 10) |
| `src/GtMotive.Estimate.IdentityServer/Dockerfile` | Multi-stage build for the demo IdentityServer |
| `docker-compose.yaml` | API + IdentityServer + MongoDB 7.0 + Azure Service Bus emulator |
| `docker-compose.dcproj` | Visual Studio Docker Compose integration |
| `.dockerignore` | Excludes build artifacts, IDE files, and secrets from the image context |

The API container listens on port **53573** (`ASPNETCORE_URLS=http://+:53573`); the IdentityServer container listens on **5001**.

## Authentication (JWT)

The API is a **resource server**: it only validates JWT access tokens. Token issuance is delegated
to an identity provider, selected in `AddHostAuthentication` in this order:

1. **Symmetric HS256 JWT** when `Jwt:Secret` is configured (lightweight local option).
2. **IdentityServer (OIDC discovery + JWT validation)** when `AppSettings:JwtAuthority` points to a
   real authority. This is the default in Docker Compose.
3. **No scheme** (tests/local tooling set their own scheme) when neither is configured.

### IdentityServer in Docker (default)

Docker Compose starts `src/GtMotive.Estimate.IdentityServer`, a Duende IdentityServer with
in-memory clients and test users, listening on `http://host.docker.internal:5001`. The issuer is
fixed via `IdentityServer__IssuerUri` to `host.docker.internal`, which Docker Desktop resolves both
from the host (browser/Swagger) and from inside the API container (back-channel), so the `iss`
claim matches everywhere.

Configuration lives in `src/GtMotive.Estimate.IdentityServer/appsettings.json` and
`IdentityServerConfig.cs`:

| Setting | Value |
|---------|-------|
| Swagger client | `client-gtestimate-swagger` / `gtmotive` (authorization code **and** client credentials) |
| Scope | `estimate-public-scope` (API resource `estimate-api`) |
| Test users | `admin` / `admin` (roles `Admin`, `User`) and `user` / `user` (role `User`) |

Roles map to endpoint authorization policies: creating vehicles requires `Admin`; renting,
returning and listing require an authenticated user (`User` for rent/return).

**Try it from Swagger UI**: open `http://localhost:53573/swagger`, click *Authorize*, keep client id
`client-gtestimate-swagger`, sign in as `admin`/`admin` on the IdentityServer login page.

**Try it with curl (client credentials)**:

```bash
TOKEN=$(curl -s -X POST http://host.docker.internal:5001/connect/token \
  -d grant_type=client_credentials \
  -d client_id=client-gtestimate-swagger \
  -d client_secret=gtmotive \
  -d scope=estimate-public-scope | jq -r .access_token)

curl -H "Authorization: Bearer $TOKEN" http://localhost:53573/api/vehicles/available
```

### Alternative: symmetric HS256 JWT

For quick local runs without the IdentityServer container, set the following environment variables
(Docker Compose double-underscore convention, matching the `Jwt:Secret` / `Jwt:Issuer` /
`Jwt:Audience` configuration keys) and remove `AppSettings__JwtAuthority`:

| Variable | Required | Purpose |
|----------|----------|---------|
| `Jwt__Secret` | Yes (enables JWT auth) | Symmetric key used to sign and validate HS256 tokens. Use a long random string; never reuse a real production secret locally. |
| `Jwt__Issuer` | No | Expected `iss` claim. When omitted, issuer validation is skipped. |
| `Jwt__Audience` | No | Expected `aud` claim. When omitted, audience validation is skipped. |

### Minting a dev token

Use the `GtMotive.Estimate.Microservice.DevTokenGenerator` CLI tool (under `tools/`) to generate a
token signed with the same secret, for manual testing via curl or Swagger's "Authorize" button:

```bash
dotnet run --project tools/GtMotive.Estimate.Microservice.DevTokenGenerator -- \
  --secret replace-with-a-long-random-development-secret \
  --issuer gtmotive-estimate-api \
  --audience gtmotive-estimate-clients \
  --subject local-dev-user \
  --role Admin
```

Or supply the secret via the `Jwt__Secret` environment variable instead of `--secret` to avoid
having it echoed/redacted on the command line:

```bash
Jwt__Secret=replace-with-a-long-random-development-secret \
  dotnet run --project tools/GtMotive.Estimate.Microservice.DevTokenGenerator -- --subject local-dev-user
```

Run `--help` for the full list of flags (`--minutes` for token lifetime, repeatable `--role`, etc.).
This tool is for local/dev use only and must never be used to issue production tokens.

## Tests

| Type | Project | What it covers | Status |
|------|---------|----------------|--------|
| **Unit** | `test/unit` | Domain rules, presenters and use-cases with mocked dependencies | Implemented and passing locally (presenters + use-cases + domain tests) |
| **Functional** | `test/functional` | Full stack (use cases + presenters + Mongo repos) without HTTP; repository integration tests use Testcontainers and share a container per test collection | Implemented and running (uses shared Testcontainers MongoDB fixture) |
| **Infrastructure** | `test/infrastructure` | End-to-end host-level tests through TestServer (POST /api/vehicles, rent, return, conflict / bad request scenarios) | Implemented and running (TestServer + Testcontainers) |

### Running tests

Unit tests:

```bash
dotnet test test/unit/GtMotive.Estimate.Microservice.UnitTests
```

Functional (integration) tests:

```bash
dotnet test test/functional/GtMotive.Estimate.Microservice.FunctionalTests
```

Infrastructure (host) tests:

```bash
dotnet test test/infrastructure/GtMotive.Estimate.Microservice.InfrastructureTests
```

Notes:

- Functional and infrastructure tests use Testcontainers.MongoDb and are organized to reuse a single container per test collection to reduce startup cost.
- The JWT authentication tests (`DevJwtTokenGeneratorTests` in Functional, `JwtAuthenticationEndpointTests` in Infrastructure) do not require Docker/Mongo — they spin up an in-memory `TestServer`/`ServiceCollection` wired only with the JWT bearer scheme.
- Tests follow the repository's existing coding conventions and the project's .editorconfig.


## Use cases

| Use case | Input | Key behaviour |
|----------|-------|---------------|
| `CreateVehicleUseCase` | Brand, model, license plate, manufacture date | Creates aggregate, persists, publishes `VehicleCreatedEvent` |
| `ListAvailableVehiclesUseCase` | (none) | Queries read repo, maps to `AvailableVehicleDto` |
| `RentVehicleUseCase` | Vehicle id, renter id | Checks active rental, calls `vehicle.Rent()`, persists |
| `ReturnVehicleUseCase` | Vehicle id | Calls `vehicle.Return()`, persists |

## Domain model

**`Vehicle`** (aggregate root) — owns rent/return lifecycle and validation:

- Rejects manufacture dates older than 5 years or in the future
- `Rent(renterId)` — transitions `Available → Rented`
- `Return()` — transitions `Rented → Available`

**`VehicleReadModel`** — flat projection for queries (id, brand, model, license plate, manufacture date).

**`LicensePlate`** — value object with required/non-empty validation.

## PDF compliance summary

| Requirement | Status |
|-------------|--------|
| Create vehicles | Implemented |
| List available vehicles | Implemented |
| Rent a vehicle | Implemented |
| Return a vehicle | Implemented |
| One active rental per person | Implemented (use case + DB index) |
| Vehicles ≤ 5 years old | Implemented (domain + read query filter) |
| Clean Architecture template | Implemented |
| Unit test without dependencies | Implemented and passing |
| Functional integration test (no host) | Written but not running |
| Infrastructure REST test (host level) | Written but not running |
| Run locally without external deps | Satisfied via Docker Compose |
| Dockerization + Visual Studio support | Implemented |
