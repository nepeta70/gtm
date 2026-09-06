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
├── GtMotive.Estimate.Microservice.Infrastructure    # MongoDB adapters, logging, telemetry, bus, resilience
├── GtMotive.Estimate.Microservice.Api               # Minimal API endpoints, MediatR wiring, presenters, resilience middleware
├── GtMotive.Estimate.Microservice.Host              # Composition root, Docker entry point
└── GtMotive.Estimate.IdentityServer                 # Dev/demo IdentityServer (Duende) for real JWT issuance

test/
├── unit/            # Domain and use-case tests (mocked dependencies)
├── functional/      # Integration tests without HTTP host
├── infrastructure/  # REST tests through TestServer
└── load/            # JMeter load test plan (rate limiting / resilience validation)
```

## Architecture

The solution follows the template's ports-and-adapters style:

- **Domain** — `Vehicle` aggregate root, `VehicleReadModel` query projection, `LicensePlate` value object, `IVehicleWriteRepository` / `IVehicleReadRepository`, domain events (`IDomainEvent`)
- **Application** — Four use cases wired through `IUseCase<TInput>` with output ports; domain events collected per-request via `IDomainEventEnvelope`
- **Infrastructure** — MongoDB persistence with AutoMapper mapping; `MongoUnitOfWork` (session/transaction-aware); event store (`IEventStore` / `MongoEventStore`); message bus abstraction (`IBus` / `IBusFactory`); Polly resilience pipelines
- **API** — Minimal API endpoints (`VehicleEndpoints`) with presenter adapters implementing output ports; MediatR pipeline behaviors for telemetry and domain-event publishing; endpoint-level rate limiting and circuit breaking

### Read / write separation

Persistence is split into command and query repositories:

| Port | Responsibility |
|------|----------------|
| `IVehicleWriteRepository` | `AddAsync`, `UpdateAsync`, `GetByIdAsync` — returns full `Vehicle` aggregates |
| `IVehicleReadRepository` | `GetAvailableAsync`, `HasActiveRentalAsync` — returns `VehicleReadModel` projections |

## API endpoints

All vehicle endpoints are under `api/vehicles` and exposed via minimal APIs in `VehicleEndpoints.cs`. Health endpoints are exposed via `HealthEndpoints.cs`.

| Method | Route | Description | Success |
|--------|-------|-------------|---------|
| `POST` | `/api/vehicles` | Register a new vehicle (requires `Admin` role) | `201 Created` (vehicle id) |
| `GET` | `/api/vehicles/available` | List available vehicles | `200 OK` |
| `POST` | `/api/vehicles/{id}/rent` | Rent a vehicle (renter id comes from the JWT claims) | `200 OK` / `404` / `400` |
| `POST` | `/api/vehicles/{id}/return` | Return a rented vehicle (renter id comes from the JWT claims) | `200 OK` / `404` / `400` |
| `GET` | `/health/live` | Kubernetes liveness probe | `200 OK` / `503` |
| `GET` | `/health/ready` | Kubernetes readiness probe | `200 OK` / `503` |

All vehicle endpoints require authentication (`RequireAuthorization()`), plus role-specific policies (`CanCreateVehicle` → `Admin`, `CanRentVehicle` → `User`) and per-endpoint rate limiting (see [Resilience](#resilience) below).

Domain rule violations (`DomainException`) are translated to HTTP `400 Bad Request`, conflicts (`ConflictException`, e.g. duplicate license plate or double-booked renter) to `409 Conflict`, cancellations/timeouts to `504 Gateway Timeout`, and an open circuit breaker to `503 Service Unavailable`, all via `BusinessExceptionHandler` (`IExceptionHandler`).

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

`MongoUnitOfWork` opens a client session and (when the deployment supports it) a transaction on first repository access within a request, and commits it in `Save()`; if MongoDB transactions aren't available (e.g. a single-node dev instance without a replica set), it logs a warning and falls back to single-document atomicity. Commit failures flagged with `UnknownTransactionCommitResult` are retried through a dedicated resilience pipeline, since MongoDB guidance treats that outcome as "may have succeeded — retry the commit."

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

## Domain events, outbox and messaging

Each use case collects at most one domain event per request in `IDomainEventEnvelope` (e.g. `VehicleCreatedEvent`, `VehicleRentedEvent`, `VehicleReturnedEvent`). A MediatR pipeline behavior, `DomainEventPublishingBehavior<TInput>`, wraps every use case execution:

- On success, if an event was collected it is appended to the event store (`IEventStore` / `MongoEventStore`, backed by an `events` MongoDB collection) and then sent through the configured bus (`IBusFactory.GetClient`).
- On failure, a `UseCaseFailedEvent` (use case name, exception type, message, timestamp) is appended and published instead, so failures are observable through the same pipeline.

`UseCaseTelemetryBehavior<TInput>` is a second, outer pipeline behavior that measures execution time for every use case and reports it via `ITelemetry` (`TrackMetric` on success, `TrackEvent` with exception details on failure), backed by `AppTelemetry` (Application Insights) outside Development and `NoOpTelemetry` in Development.

### Bus providers

`IBusFactory` resolves the active `IBus` implementation from the `Bus:Provider` configuration value (`BusNames.InMemory`, `BusNames.Azure`):

| Provider | Implementation | Use |
|----------|-----------------|-----|
| `InMemory` | `InMemoryBus` | Local dev, functional/infrastructure tests (in-process, inspectable `SentMessages`) |
| `Azure` | `AzureServiceBus` | Docker Compose (Azure Service Bus emulator), production |

Azure Service Bus sends go through a dedicated resilience pipeline (retry + circuit breaker + timeout — see below).

## Resilience

Outbound and inbound resilience is implemented with Polly v8 and registered as keyed singletons (`ResiliencePipelineNames.Mongo`, `MongoTransactionCommit`, `ServiceBus`):

| Pipeline | Strategy | Applies to |
|----------|----------|------------|
| `Mongo` | Retry (exponential backoff + jitter) on transient connection/wait-queue errors, timeout | All MongoDB CRUD operations (read/write repositories, event store, index creation) |
| `MongoTransactionCommit` | Retry on `UnknownTransactionCommitResult` and connection errors | `MongoUnitOfWork.Save()` |
| `ServiceBus` | Retry on transient `ServiceBusException`, circuit breaker, timeout | `AzureServiceBus.Send()` |

At the API layer (`ApiResilienceExtensions`):

- **Request timeouts** — a default policy (5s) applied via `UseRequestTimeouts()`.
- **Rate limiting** — a per-endpoint-path concurrency limiter (`EndpointConcurrency` policy) applied to the `api/vehicles` group.
- **Circuit breaker** — `CircuitBreakerMiddleware` wraps the request pipeline in a Polly circuit breaker; any downstream `5xx` response counts as a failure, and once the circuit opens, subsequent requests fail fast with `503 Service Unavailable` via `CircuitBrokenException`.

Tuning values live in `ResilienceDefaults` and `ApiResilienceDefaults` as constants; in a production-grade setup these would be sourced from configuration instead.

## Running locally

### Option 1 — Docker Compose (recommended)

No external dependencies need to be installed on the host. Docker Compose starts the API, MongoDB, the Service Bus emulator (with its SQL Edge backing store) and the IdentityServer:

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
| `docker-compose.yaml` | API + IdentityServer + MongoDB 7.0 + Azure Service Bus emulator (backed by Azure SQL Edge) |
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

Authorization on top of authentication is policy-based (`AuthorizationPolicies.CanCreateVehicle`, `CanRentVehicle`), mapped to role requirements (`Admin`, `User`) in `AuthorizationOptionsExtensions`. `IAuthorizationService` has two implementations selected in `InfrastructureConfiguration`: `JwtAuthorizationService` (evaluates role claims against policies) outside Development when a JWT secret is configured, and `NoOpAuthorizationService` (authorizes everything) otherwise.

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
| **Unit** | `test/unit` | Domain rules, presenters, use-cases and resilience pipeline registration with mocked dependencies | Implemented and passing locally |
| **Functional** | `test/functional` | Full stack (use cases + presenters + Mongo repos) without HTTP; repository integration tests use Testcontainers and share a container per test collection | Implemented and running (shared Testcontainers MongoDB fixture) |
| **Infrastructure** | `test/infrastructure` | End-to-end host-level tests through TestServer (POST /api/vehicles, rent, return, conflict / bad request / JWT authentication scenarios) | Implemented and running (TestServer + Testcontainers) |
| **Load** | `test/load` | JMeter plan (`RequestsAndLimitsPlan.jmx`) exercising API rate limits and resilience under concurrent load | Implemented; run manually via JMeter (see `test/load/README.md`) |

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
| `CreateVehicleUseCase` | Brand, model, license plate, manufacture date | Creates aggregate, persists, collects `VehicleCreatedEvent` |
| `ListAvailableVehiclesUseCase` | (none) | Queries read repo, maps to `AvailableVehicleDto` |
| `RentVehicleUseCase` | Vehicle id, renter id | Checks active rental, calls `vehicle.Rent()`, persists, collects `VehicleRentedEvent` |
| `ReturnVehicleUseCase` | Vehicle id, renter id | Calls `vehicle.Return()`, persists, collects `VehicleReturnedEvent` |

Every use case runs inside two MediatR pipeline behaviors (`UseCaseTelemetryBehavior`, `DomainEventPublishingBehavior`) that add telemetry and outbox/event-publishing behaviour without touching the use case code itself (see [Domain events, outbox and messaging](#domain-events-outbox-and-messaging)).

## Domain model

**`Vehicle`** (aggregate root) — owns rent/return lifecycle and validation:

- Rejects manufacture dates older than 5 years or in the future
- Rejects a missing/empty brand, model or license plate
- `Rent(renterId)` — transitions `Available → Rented`, rejects renting an already-rented vehicle
- `Return(renterId)` — transitions `Rented → Available`, rejects returning an available vehicle or a return by a different renter than the one who rented it

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
| Functional integration test (no host) | Implemented and running (Testcontainers MongoDB) |
| Infrastructure REST test (host level) | Implemented and running (TestServer + Testcontainers) |
| Run locally without external deps | Satisfied via Docker Compose |
| Dockerization + Visual Studio support | Implemented |
| Resilience (retries, circuit breaker, timeouts, rate limiting) | Implemented (Polly pipelines, request timeouts, endpoint rate limiting) |
| Domain event outbox / messaging | Implemented (event store + configurable bus: InMemory / Azure Service Bus) |
