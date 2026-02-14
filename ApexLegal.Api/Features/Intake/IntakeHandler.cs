using ApexLegal.Api.Domain.Events;
using ApexLegal.Api.Features.ClientManagement;
using ApexLegal.Api.Features.MatterManagement;
using Marten;
using Wolverine;

namespace ApexLegal.Api.Features.Intake;

public record OpenMatterCommand(
    Guid ClientId,
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
        // 1. Resolve client name from inline projection
        var client = await session.LoadAsync<ClientDetails>(command.ClientId, ct);
        if (client is null)
            throw new InvalidOperationException($"Client with ID {command.ClientId} not found.");

        // 2. Conflict check: Validate opposing party is not an existing client
        var conflictExists = await session.Query<ClientDetails>()
            .AnyAsync(c => c.Name == command.OpposingParty, ct);

        if (conflictExists)
            throw new InvalidOperationException($"Conflict of interest: {command.OpposingParty} is an existing client.");

        // 3. Create event with denormalized client name
        var matterId = Guid.NewGuid();
        var @event = new MatterOpened(
            matterId,
            command.ClientId,
            client.Name,
            command.OpposingParty,
            command.CaseType,
            command.InitialClaimAmount,
            DateTime.UtcNow
        );

        // 4. Append to event stream
        session.Events.StartStream<Domain.Matter>(matterId, @event);

        // 5. Auto-tag logic (Requirement: If claim > $1M, auto-tag as High Priority)
        if (command.InitialClaimAmount > 1_000_000)
        {
            var priorityEvent = new MatterTaggedAsHighPriority(matterId, DateTime.UtcNow);
            session.Events.Append(matterId, priorityEvent);
        }

        await session.SaveChangesAsync(ct);

        return @event;
    }
}
