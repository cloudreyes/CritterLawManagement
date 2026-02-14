# ADR-0004: .NET Aspire for Orchestration

## Status
Accepted

## Context
The application consists of multiple components: an API service, a Razor Pages frontend, and a PostgreSQL database. Local development requires coordinating these services, managing connection strings, health checks, and observability. Production-like local development should be achievable without Docker Compose files or manual setup.

## Decision
We use **.NET Aspire** as the orchestration and service defaults layer.

- `ApexLegal.AppHost` defines the distributed application topology (Postgres, API, Web)
- `ApexLegal.ServiceDefaults` provides shared configuration: OpenTelemetry, health checks, service discovery, and resilience
- Service discovery allows the Web frontend to reference the API as `http://api` without hardcoded URLs
- Aspire manages PostgreSQL lifecycle with `AddPostgres().WithDataVolume()`
- `WaitFor()` ensures correct startup ordering (DB before API, API before Web)

## Consequences
**Positive:**
- One-click local development: `dotnet run` on AppHost starts everything
- Built-in observability via OpenTelemetry and the Aspire dashboard
- Service discovery eliminates hardcoded connection strings
- Health checks and resilience configured once via ServiceDefaults

**Negative:**
- Requires .NET Aspire tooling (workload install)
- AppHost is development-only — production deployment requires separate strategy
- Adds a project to the solution that isn't deployed

## Alternatives Considered
- **Docker Compose**: Rejected — requires separate YAML files, no integrated .NET tooling, harder to debug
- **Manual service startup**: Rejected — error-prone, no service discovery, no coordinated health checks
