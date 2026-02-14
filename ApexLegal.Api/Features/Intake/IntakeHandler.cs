using ApexLegal.Api.Domain.Events;
using ApexLegal.Api.Features.MatterManagement;
using Marten;
using Wolverine;

namespace ApexLegal.Api.Features.Intake;

public record OpenMatterCommand(
    string ClientName,
    string OpposingParty,
    CaseType CaseType,
    decimal InitialClaimAmount
);

public class IntakeHandler
{
    public async Task<MatterOpened> Handle(
        OpenMatterCommand command,
        IDocumentSession session,
        IMessageContext bus,
        CancellationToken ct)
    {
        // 1. Conflict check: Validate opposing party is not an existing client
        var conflictExists = await session.Query<MatterDetails>()
            .AnyAsync(m => m.OpposingParty == command.OpposingParty, ct);

        if (conflictExists)
            throw new InvalidOperationException($"Conflict of interest: {command.OpposingParty} is already a client or involved in a matter.");

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
        session.Events.StartStream<Domain.Matter>(matterId, @event);

        // 4. Auto-tag logic (Requirement: If claim > $1M, auto-tag as High Priority)
        if (command.InitialClaimAmount > 1_000_000)
        {
            var priorityEvent = new MatterTaggedAsHighPriority(matterId, DateTimeOffset.UtcNow);
            session.Events.Append(matterId, priorityEvent);
            
            // We could also publish this via Wolverine if we want side effects
            // For now, Marten handles the event storage.
        }

        await session.SaveChangesAsync(ct);

        return @event;
    }
}
