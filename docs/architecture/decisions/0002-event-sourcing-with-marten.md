# ADR-0002: Event Sourcing with Marten for Matters

## Status
Accepted

## Context
Legal case management requires a full audit trail of every change to a matter — who changed what, when, and why. Traditional CRUD models overwrite state and lose history. Regulatory and compliance requirements in legal systems make audit trails non-negotiable.

## Decision
We use **Event Sourcing** via **Marten** (PostgreSQL-backed) as the persistence strategy for the `Matter` aggregate. All state changes are captured as immutable events appended to a stream. Current state is derived by replaying events through the aggregate's `Apply()` methods.

Key conventions:
- Events are immutable `record` types with past-tense names (`MatterOpened`, `StatusChanged`)
- Each matter is an event stream identified by a `Guid`
- The `Matter` aggregate root applies events — never mutates directly
- Read models are rebuilt from events via Marten projections

## Consequences
**Positive:**
- Complete audit trail for every matter — built into the architecture, not bolted on
- Temporal queries: reconstruct state at any point in time
- Natural fit for the legal domain where history matters
- Projections provide optimized read models without sacrificing write integrity

**Negative:**
- Higher complexity than CRUD for simple operations
- Eventual consistency for async projections (dashboard)
- Schema evolution requires event versioning strategy for future changes

## Alternatives Considered
- **CRUD with audit log table**: Rejected — audit trail is a second-class citizen, easy to forget or bypass
- **Event Sourcing with EventStoreDB**: Rejected — Marten keeps everything in PostgreSQL, reducing infrastructure complexity; Aspire has first-class Postgres support
