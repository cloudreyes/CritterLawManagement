# ADR-0003: Wolverine for Workflow Automation

## Status
Accepted

## Context
Legal case management involves automated workflows triggered by state changes. For example, when a matter enters the "Discovery" phase, the system must auto-create tasks and send notifications. These side effects should be decoupled from the endpoints that trigger them.

We need a messaging/mediator framework that integrates tightly with Marten's event sourcing and supports transactional outbox patterns.

## Decision
We use **Wolverine** (part of the Critter Stack alongside Marten) as our in-process messaging and workflow framework.

- Wolverine handlers react to domain events published via `IMessageBus`
- `AutoApplyTransactions()` ensures Marten sessions are committed automatically after handler execution
- `IntegrateWithWolverine()` on Marten enables transactional inbox/outbox for reliable messaging
- Handlers are plain classes discovered by convention — no marker interfaces required

Example: `DiscoveryWorkflowHandler` listens for `StatusChanged` events and auto-creates tasks when a matter enters Discovery.

## Consequences
**Positive:**
- Decoupled workflows: endpoints don't know about side effects
- Transactional safety via Marten integration (outbox pattern)
- Convention-based handler discovery — minimal boilerplate
- Local queues with configurable parallelism

**Negative:**
- Tight coupling to the Critter Stack ecosystem
- Implicit handler discovery can be surprising for developers unfamiliar with Wolverine
- Debugging message flow requires understanding Wolverine's pipeline

## Alternatives Considered
- **MediatR**: Rejected — no built-in transactional outbox, no Marten integration, more manual wiring
- **MassTransit**: Rejected — heavier infrastructure (requires RabbitMQ/Azure Service Bus); overkill for a single-process demo
- **Manual event dispatching**: Rejected — error-prone, no retry/resilience built in
