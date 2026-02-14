# DEVELOPMENT_GUIDE.md - Project Guidelines & Development Standards

This file serves as the "constitution" for how to build Apex Legal correctly.

## 1. Project Overview
Apex Legal is a legal management system built using the **Critter Stack** (Marten + Wolverine) and **.NET Aspire**. The project follows **Event Sourcing** for core domain logic and **Vertical Slice Architecture** for feature organization.

---

## 2. Core Architectural Principles

### Vertical Slice Architecture - NON-NEGOTIABLE RULES

**What Vertical Slice Architecture IS:**
- Each feature is a self-contained vertical slice from API endpoint → business logic → data access → response
- Features are organized by **business capability**, not technical layer
- A slice contains EVERYTHING needed for that feature: endpoint, handlers, events, projections, DTOs
- Related code lives together in feature folders

**What Vertical Slice Architecture IS NOT:**
- ❌ NO "Controllers" folder with all endpoints
- ❌ NO "Services" folder with business logic
- ❌ NO "Repositories" folder
- ❌ NO shared "Helpers" or "Utilities" dumping ground
- ❌ NO "Managers" or "Coordinators"

**Correct Structure:**
```
ApexLegal.Api/
├── Features/
│   ├── Intake/
│   │   ├── IntakeEndpoint.cs          // Minimal API endpoint
│   │   ├── IntakeRequest.cs           // Request DTO
│   │   ├── IntakeResponse.cs          // Response DTO
│   │   ├── IntakeHandler.cs           // Wolverine message handler
│   │   ├── IntakeValidator.cs         // FluentValidation (if needed)
│   │   └── MatterOpened.cs            // Domain event (if specific to intake)
│   ├── MatterManagement/
│   │   ├── GetMatter/
│   │   │   ├── GetMatterEndpoint.cs
│   │   │   ├── GetMatterQuery.cs
│   │   │   └── MatterDetailsProjection.cs
│   │   ├── GetMatterHistory/
│   │   │   ├── GetMatterHistoryEndpoint.cs
│   │   │   └── GetMatterHistoryQuery.cs
│   │   └── UpdateStatus/
│   │       ├── UpdateStatusEndpoint.cs
│   │       ├── UpdateStatusCommand.cs
│   │       └── UpdateStatusHandler.cs
│   ├── Dashboard/
│   │   ├── GetDashboardEndpoint.cs
│   │   ├── DashboardResponse.cs
│   │   └── DashboardStatisticsProjection.cs
│   └── Workflows/
│       ├── DiscoveryWorkflow/
│       │   └── DiscoveryWorkflowHandler.cs
│       └── EmailNotifications/
│           └── EmailNotificationHandler.cs
├── Domain/
│   ├── Matter.cs                       // Aggregate Root
│   ├── Events/                         // Shared domain events
│   │   ├── MatterOpened.cs
│   │   ├── AttorneyAssigned.cs
│   │   ├── StatusChanged.cs
│   │   └── ... (other events)
│   └── ValueObjects/                   // If needed (e.g., CaseType)
│       └── CaseType.cs
└── Infrastructure/                     // ONLY for cross-cutting concerns
    ├── MartenConfiguration.cs
    ├── WolverineConfiguration.cs
    └── AspireServiceExtensions.cs
```

---

## 3. File Organization Rules

### Rule #1: NO CODE IN Program.cs (Except Configuration Calls)
**BAD:**
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMarten(options => 
{
    options.Connection(connectionString);
    options.Events.StreamIdentity = StreamIdentity.AsGuid;
    // 50 more lines of configuration...
});

builder.Services.AddWolverine(opts => 
{
    // 30 more lines...
});

var app = builder.Build();

app.MapPost("/api/intake", async (IntakeRequest request, IDocumentSession session) =>
{
    // 40 lines of business logic in Program.cs 🤮
});

app.Run();
```

**GOOD:**
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddMartenWithEventSourcing();  // Extension method in Infrastructure/
builder.AddWolverineMessaging();       // Extension method in Infrastructure/

var app = builder.Build();

app.MapFeatureEndpoints();  // Extension method that discovers all endpoints

app.Run();
```

---

### Rule #2: Use Extension Methods for Configuration
Create `Infrastructure/MartenConfiguration.cs`:
```csharp
public static class MartenConfiguration
{
    public static IHostApplicationBuilder AddMartenWithEventSourcing(
        this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration
            .GetConnectionString("postgres");

        builder.Services.AddMarten(options =>
        {
            options.Connection(connectionString);
            options.Events.StreamIdentity = StreamIdentity.AsGuid;
            
            // Register projections
            options.Projections.Add<MatterDetailsProjection>(ProjectionLifecycle.Inline);
            options.Projections.Add<DashboardStatisticsProjection>(ProjectionLifecycle.Async);
        });

        return builder;
    }
}
```

---

### Rule #3: Minimal API Endpoints Stay Minimal
Each endpoint file should ONLY:
1. Define the route
2. Validate input (if not using FluentValidation)
3. Delegate to a handler/command
4. Return a response

**GOOD Example:**
```csharp
// Features/Intake/IntakeEndpoint.cs
public static class IntakeEndpoint
{
    public static void MapIntakeEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/intake", async (
            IntakeRequest request,
            IMessageBus bus,
            CancellationToken ct) =>
        {
            var command = new OpenMatterCommand(
                request.ClientName,
                request.OpposingParty,
                request.CaseType,
                request.InitialClaimAmount
            );

            var result = await bus.InvokeAsync<MatterOpened>(command, ct);
            
            return Results.Created($"/api/matters/{result.MatterId}", result);
        })
        .WithName("OpenMatter")
        .WithTags("Intake");
    }
}
```

---

### Rule #4: Wolverine Handlers are Separate Classes
**NEVER** put handler logic in endpoint files or Program.cs.

```csharp
// Features/Intake/IntakeHandler.cs
public class IntakeHandler
{
    public async Task<MatterOpened> Handle(
        OpenMatterCommand command,
        IDocumentSession session,
        CancellationToken ct)
    {
        // 1. Conflict check
        var conflictExists = await session
            .Query<MatterDetailsProjection>()
            .AnyAsync(m => m.OpposingParty == command.OpposingParty, ct);

        if (conflictExists)
            throw new ConflictException($"Opposing party {command.OpposingParty} is already a client");

        // 2. Create event
        var matterId = Guid.NewGuid();
        var @event = new MatterOpened(
            matterId,
            command.ClientName,
            command.OpposingParty,
            command.CaseType,
            command.InitialClaimAmount,
            DateTimeOffset.UtcNow
        );

        // 3. Append to event stream
        session.Events.StartStream<Matter>(matterId, @event);
        await session.SaveChangesAsync(ct);

        return @event;
    }
}
```

---

## 4. Event Sourcing Guardrails

### Events are Immutable Records
```csharp
// Domain/Events/MatterOpened.cs
public record MatterOpened(
    Guid MatterId,
    string ClientName,
    string OpposingParty,
    CaseType CaseType,
    decimal InitialClaimAmount,
    DateTimeOffset OccurredAt
);
```

### Aggregate Roots Apply Events (Never Mutate Directly)
```csharp
// Domain/Matter.cs
public class Matter
{
    public Guid Id { get; private set; }
    public string ClientName { get; private set; } = default!;
    public string OpposingParty { get; private set; } = default!;
    public MatterStatus Status { get; private set; }
    public bool IsHighPriority { get; private set; }
    
    // Marten requires parameterless constructor for rehydration
    public Matter() { }

    // Apply methods - called during event replay
    public void Apply(MatterOpened @event)
    {
        Id = @event.MatterId;
        ClientName = @event.ClientName;
        OpposingParty = @event.OpposingParty;
        Status = MatterStatus.New;
        IsHighPriority = @event.InitialClaimAmount > 1_000_000;
    }

    public void Apply(StatusChanged @event)
    {
        Status = @event.NewStatus;
    }
}
```

---

## 5. Naming Conventions

| Type | Convention | Example |
|------|-----------|---------|
| Events | Past tense | `MatterOpened`, `AttorneyAssigned`, `StatusChanged` |
| Commands | Imperative | `OpenMatterCommand`, `AssignAttorneyCommand` |
| Queries | Question form | `GetMatterQuery`, `GetDashboardStatisticsQuery` |
| Endpoints | `{Feature}Endpoint.cs` | `IntakeEndpoint.cs`, `GetMatterEndpoint.cs` |
| Handlers | `{Feature}Handler.cs` | `IntakeHandler.cs`, `DiscoveryWorkflowHandler.cs` |
| Projections | `{ReadModel}Projection.cs` | `MatterDetailsProjection.cs` |

---

## 6. Architectural Decision Records (ADRs)

### ADR Process
For EVERY significant architectural decision, create an ADR in `/docs/architecture/decisions/`:

**Template: `NNNN-title-of-decision.md`**
```markdown
# ADR-0001: Use Vertical Slice Architecture

## Status
Accepted

## Context
Traditional layered architecture leads to scattered feature code across Controllers/, Services/, and Repositories/ folders. Changes require touching multiple layers.

## Decision
We will organize code by feature/slice rather than technical layer. Each feature folder contains its endpoint, handler, DTOs, and projections.

## Consequences
**Positive:**
- Feature locality: all related code in one place
- Easier to delete features
- New developers can understand one feature without learning entire codebase

**Negative:**
- Deviation from .NET template defaults
- Some code duplication acceptable (better than wrong abstraction)

## Alternatives Considered
- Clean Architecture (rejected: too much ceremony for this project size)
- Traditional MVC (rejected: doesn't align with CQRS/Event Sourcing)
```

### Required ADRs for This Project:
1. **ADR-0001**: Use of Vertical Slice Architecture
2. **ADR-0002**: Event Sourcing with Marten for Matters
3. **ADR-0003**: Wolverine for Workflow Automation
4. **ADR-0004**: .NET Aspire for Orchestration
5. **ADR-0005**: Inline vs Async Projections Strategy

---

## 7. Code Review Checklist

Before committing ANY code, verify:
- [ ] Is this code in the correct feature folder?
- [ ] Is Program.cs still minimal (< 30 lines)?
- [ ] Are all handlers in separate classes?
- [ ] Are events immutable records with past-tense names?
- [ ] Does the aggregate apply events rather than direct mutation?
- [ ] Is there an ADR if this introduces a new pattern?
- [ ] Can I delete this entire feature folder without breaking others?

---

## 8. Anti-Patterns to Avoid

### ❌ God Classes
Don't create `MatterService`, `MatterManager`, `MatterHelper` that do everything.

### ❌ Anemic Domain Models
Don't use `Matter` as a pure data bag with separate `MatterService` doing all logic.

### ❌ Feature Envy
If a handler in `FeatureA/` is reaching into `FeatureB/` internals, they should communicate via events.

### ❌ Shared Mutable State
Events are the ONLY way to communicate changes. No shared static classes.

---

## 9. Testing Strategy
- Unit tests live next to the feature: `Features/Intake/IntakeHandler.Tests.cs`
- Integration tests use `WebApplicationFactory` with Testcontainers for Postgres
- Event sourcing tests verify event application logic on aggregates

---

## 10. Junie-Specific Instructions

When I ask you to implement a feature:
1. **First**, tell me which feature folder you're creating/modifying
2. **Second**, show me the file structure you'll create
3. **Third**, ask if I need an ADR for any new pattern
4. **Then**, generate code following these guidelines
5. **Finally**, update this DEVELOPMENT_GUIDE.md if you introduce a new pattern

If I ask you to put code in Program.cs, **push back** and suggest the proper location.
