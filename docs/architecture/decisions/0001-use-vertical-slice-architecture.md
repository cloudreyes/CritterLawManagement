# ADR-0001: Use Vertical Slice Architecture

## Status
Accepted

## Context
Traditional layered architecture (Controllers → Services → Repositories) scatters feature code across multiple folders. A single feature change requires touching files in 3+ directories, making it hard to reason about a feature in isolation.

For an event-sourced system with CQRS patterns, the natural unit of work is a feature/slice — a command or query with its handler, endpoint, and projections.

## Decision
We organize code by **business capability** (feature folders) rather than technical layer. Each feature folder contains its endpoint, handler, DTOs, projections, and validators — everything needed for that slice.

```
Features/
  Intake/
    IntakeEndpoint.cs
    IntakeHandler.cs
  MatterManagement/
    MatterManagementEndpoints.cs
    MatterDetailsProjection.cs
```

Cross-cutting infrastructure (Marten config, Wolverine config, endpoint discovery) lives in `Infrastructure/`.

## Consequences
**Positive:**
- Feature locality: all related code in one place
- Easy to delete or modify features without ripple effects
- Natural alignment with CQRS and event sourcing patterns
- New developers understand one feature without learning the entire codebase

**Negative:**
- Deviates from default .NET project templates
- Some code duplication is acceptable (better than premature abstraction)

## Alternatives Considered
- **Clean Architecture**: Rejected — too much ceremony and indirection for this project size
- **Traditional MVC layers**: Rejected — doesn't align with CQRS/Event Sourcing naturally
