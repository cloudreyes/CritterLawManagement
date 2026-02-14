# ADR-0005: Inline vs Async Projections Strategy

## Status
Accepted

## Context
Marten offers two projection lifecycle modes:
- **Inline**: Projections are updated synchronously within the same transaction as event appends. Guarantees immediate consistency but adds latency to writes.
- **Async**: Projections are updated by a background daemon after events are committed. Provides eventual consistency but keeps writes fast.

We need to decide which mode to use for each projection based on its consistency requirements.

## Decision
We use a **mixed strategy** based on the read model's consistency needs:

| Projection | Lifecycle | Rationale |
|---|---|---|
| `MatterDetailsProjection` | **Inline** | Must be immediately consistent — the status change endpoint reads this projection to get the current status before appending a `StatusChanged` event. Stale data here would cause incorrect `OldStatus` values. |
| `DashboardStatisticsProjection` | **Async** | Dashboard aggregates tolerate brief staleness. Eventual consistency (sub-second in practice) is acceptable for summary statistics. Keeping this async avoids slowing down every write operation. |

The async daemon runs in `DaemonMode.Solo` (single-node), appropriate for this application's deployment model.

## Consequences
**Positive:**
- `MatterDetails` is always consistent — endpoints can trust the read model
- Dashboard writes don't slow down case intake or status changes
- Clear reasoning for each projection's lifecycle choice

**Negative:**
- Dashboard stats may lag briefly (typically milliseconds) after events are appended
- Async daemon must be running for dashboard to update — configured via `AddAsyncDaemon(DaemonMode.Solo)`
- Mixed strategy requires developers to consciously choose the right lifecycle for new projections

## Alternatives Considered
- **All Inline**: Rejected — dashboard projection touching every write would add unnecessary latency
- **All Async**: Rejected — `MatterDetails` must be immediately consistent for status change validation
