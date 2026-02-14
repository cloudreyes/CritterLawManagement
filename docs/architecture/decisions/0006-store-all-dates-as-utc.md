# ADR-0006: Store All Dates as UTC

## Status
Accepted

## Context
The system stores timestamps on domain events (e.g., `OccurredAt`, `CreatedAt`, `SentAt`, `DueDate`) and projection read models. These timestamps are persisted in PostgreSQL via Marten's JSON serialization and returned to the Web frontend through the API.

Previously, the codebase used `DateTimeOffset` for all timestamp properties. While `DateTimeOffset` carries an explicit offset, it introduces unnecessary complexity:
- Marten serializes `DateTimeOffset` with offset information that varies by server timezone
- Comparing and sorting across different offsets adds ambiguity
- The Web frontend must handle offset normalization for display

Since this application operates in a single-timezone context and all timestamps represent "when something happened on the server," a simpler approach is preferred.

## Decision
All date and time values in domain events, projection read models, and DTOs use `DateTime` with `DateTimeKind.Utc`. Timestamps are always created via `DateTime.UtcNow`.

The UI layer is responsible for converting UTC timestamps to the user's local timezone for display purposes.

### Rules
1. **Events**: All timestamp properties use `DateTime` (e.g., `DateTime OccurredAt`)
2. **Construction**: Always use `DateTime.UtcNow` — never `DateTime.Now`
3. **Serialization**: Marten's `UseSystemTextJsonForSerialization` stores UTC values as `"2026-02-14T12:00:00Z"` — the trailing `Z` is unambiguous
4. **Display**: The Web project handles any local timezone formatting at the Razor view level

## Consequences
**Positive:**
- Simpler type — `DateTime` is universally understood and has no offset ambiguity when consistently UTC
- JSON serialization is clean and compact (`"Z"` suffix vs `"+00:00"`)
- No risk of mixing offsets from different server timezones
- Sorting and comparison are straightforward

**Negative:**
- Developers must remember to use `DateTime.UtcNow`, not `DateTime.Now` — code review checklist should enforce this
- `DateTime` does not self-document its kind the way `DateTimeOffset` does — the convention must be understood
- Breaking change for existing event streams — requires a fresh database when migrating from `DateTimeOffset`

## Alternatives Considered
- **Keep `DateTimeOffset`**: Rejected — adds complexity without benefit in a single-timezone server deployment; offset information was always `+00:00` since we used `DateTimeOffset.UtcNow`
- **Use `DateOnly`/`TimeOnly` where applicable**: Considered for `DueDate` but rejected for now — `DateTime` keeps the API surface consistent; can be revisited if date-only semantics become important
