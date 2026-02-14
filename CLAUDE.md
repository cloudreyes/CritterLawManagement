# CLAUDE.md - Context for Claude Code Sessions

## Project Overview

**Apex Legal** is a legal case management system built as a Critter Stack (Marten + Wolverine) showcase. It demonstrates Event Sourcing, CQRS, and Vertical Slice Architecture in a .NET Aspire-orchestrated environment.

The system handles: matter intake with auto-priority tagging, status lifecycle management, workflow automation (discovery tasks, client notifications), event history tracking, and a dashboard with aggregated statistics.

## Tech Stack

- **.NET 10** (Preview) - `dotnet` may not be on PATH; use `/usr/local/share/dotnet/dotnet` if needed
- **Marten 8.x** (8.21+) - Event store & document DB on PostgreSQL
- **Wolverine 4.x** - Message bus & handler framework
- **.NET Aspire 9.x** - Service orchestration & discovery
- **PostgreSQL** - Provisioned by Aspire (container: `apexlegaldb`)

## Solution Structure

```
CritterLawManagement/
├── ApexLegal.AppHost/          # Aspire orchestrator (entry point: dotnet run)
│   ├── AppHost.cs              # Topology: Postgres -> API -> Web
│   └── Properties/launchSettings.json
├── ApexLegal.Api/              # Core API with event sourcing
│   ├── Domain/
│   │   ├── Matter.cs           # Aggregate root (Apply methods for event replay)
│   │   └── Events/MatterEvents.cs  # All event records + CaseType/MatterStatus enums
│   ├── Features/               # Vertical slices by business capability
│   │   ├── Intake/             # POST /api/intake (OpenMatterCommand -> handler)
│   │   ├── MatterManagement/   # GET matter, GET history, POST status
│   │   ├── Dashboard/          # GET /api/dashboard (async projection)
│   │   └── Workflows/          # DiscoveryWorkflowHandler (reacts to StatusChanged)
│   ├── Infrastructure/         # Cross-cutting only
│   │   ├── MartenConfiguration.cs    # Event store + projections + async daemon
│   │   ├── WolverineConfiguration.cs # AutoApplyTransactions enabled
│   │   └── EndpointDiscovery.cs      # Reflection-based endpoint mapping
│   └── Program.cs              # Must stay < 30 lines (see DEVELOPMENT_GUIDE.md)
├── ApexLegal.Web/              # Razor Pages frontend
├── ApexLegal.ServiceDefaults/  # Aspire shared config (health, telemetry)
├── docs/architecture/decisions/ # ADRs 0000-0005
├── DEVELOPMENT_GUIDE.md        # Non-negotiable architecture rules - READ THIS FIRST
└── README.md                   # Learning guide with Mermaid diagrams
```

## Key Architectural Patterns

### Event Sourcing (Marten)
- Events are immutable `record` types in `Domain/Events/MatterEvents.cs`
- Aggregate `Matter.cs` rebuilds state via `Apply()` methods - never mutate directly
- Streams keyed by `Guid` (`StreamIdentity.AsGuid`)
- **Inline projections** (`MatterDetailsProjection`): immediate consistency, updated in same transaction
- **Async projections** (`DashboardStatisticsProjection`): eventual consistency, needs `AddAsyncDaemon(DaemonMode.Solo)`

### Wolverine Messaging
- **Convention-based handler discovery**: methods named `Handle()` or `Consume()` on any class
- `IMessageBus.InvokeAsync<T>()` - request/response (waits for result)
- `IMessageBus.PublishAsync()` - fire-and-forget (triggers side-effect handlers)
- **AutoApplyTransactions**: Wolverine auto-commits Marten sessions after handlers complete - do NOT call `SaveChangesAsync()` in handlers that use this
- Status endpoint must explicitly `PublishAsync(@event)` for Wolverine handlers to fire

### Vertical Slice Architecture
- Features organized by business capability, NOT technical layer
- No Controllers/, Services/, Repositories/ folders
- Each feature is self-contained and deletable
- See `DEVELOPMENT_GUIDE.md` for exhaustive rules

## API Endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/api/intake` | Open new matter (auto-tags high priority if claim > $1M) |
| GET | `/api/matters/{id}` | Get matter details (inline projection) |
| GET | `/api/matters/{id}/history` | Get full event stream |
| POST | `/api/matters/{id}/status` | Update matter status (triggers workflow) |
| GET | `/api/dashboard` | Aggregated statistics (async projection) |

## Running the Application

```bash
# From repo root - starts Postgres, API, and Web via Aspire
dotnet run --project ApexLegal.AppHost

# Aspire dashboard: https://localhost:17023
# API and Web ports are dynamically assigned by Aspire - check dashboard for URLs
```

**Important runtime notes:**
- Aspire assigns dynamic ports to API and Web services
- Dashboard auth is disabled via `DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true`
- Dev certs must be trusted: `dotnet dev-certs https --trust`
- Do NOT add `UseHttpsRedirection()` to API - it breaks under Aspire dynamic ports
- Postgres data persists across runs in Docker volume; use different test data or recreate container to avoid conflict-of-interest violations

## Marten 8.x Gotchas (Critical)

These broke the build during initial setup and will bite again if not careful:

| What Changed | Old (Marten 7.x) | New (Marten 8.x) |
|---|---|---|
| Async daemon namespace | `Marten.Events.Daemon.Resiliency` | `JasperFx.Events.Daemon` |
| SingleStreamProjection namespace | `Marten.Events.Projections` | `Marten.Events.Aggregation` |
| SingleStreamProjection signature | `SingleStreamProjection<TDoc>` | `SingleStreamProjection<TDoc, TId>` (e.g., `<MatterDetails, Guid>`) |
| JSON serialization config | `UseDefaultSerialization(EnumStorage.AsString)` | `UseSystemTextJsonForSerialization(EnumStorage.AsString)` |
| AddMarten lambda | `AddMarten(options => ...)` | `AddMarten((StoreOptions options) => ...)` (explicit type needed to avoid overload ambiguity) |
| Projection lifecycle namespace | `Marten.Events.Projections` | `JasperFx.Events.Projections` |

## Serialization Notes

- API uses `JsonStringEnumConverter` in `ConfigureHttpJsonOptions` so enums serialize as strings
- Marten uses `UseSystemTextJsonForSerialization(EnumStorage.AsString)` for event/document storage
- `IEvent.EventType` is `System.Type` and cannot be serialized directly - map to `.Name` string in DTOs

## Development Rules

See `DEVELOPMENT_GUIDE.md` for the full set of non-negotiable rules. Key points:
- Program.cs must stay < 30 lines - use extension methods in Infrastructure/
- Handlers go in separate classes, never in endpoint files
- Events are past-tense immutable records
- New architectural patterns require an ADR in `docs/architecture/decisions/`
- Code review checklist in DEVELOPMENT_GUIDE.md Section 7
