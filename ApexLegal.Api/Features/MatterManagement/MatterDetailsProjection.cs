using ApexLegal.Api.Domain.Events;
using Marten.Events.Aggregation;

namespace ApexLegal.Api.Features.MatterManagement;

public record MatterDetails(
    Guid Id,
    Guid ClientId,
    string ClientName,
    string OpposingParty,
    CaseType CaseType,
    MatterStatus Status,
    bool IsHighPriority,
    decimal CurrentClaimAmount,
    Guid? AssignedAttorneyId,
    DateTime CreatedAt
);

public class MatterDetailsProjection : SingleStreamProjection<MatterDetails, Guid>
{
    public MatterDetails Create(MatterOpened @event)
    {
        return new MatterDetails(
            @event.MatterId,
            @event.ClientId,
            @event.ClientName,
            @event.OpposingParty,
            @event.CaseType,
            MatterStatus.New,
            false,
            @event.InitialClaimAmount,
            null,
            @event.OccurredAt
        );
    }

    public MatterDetails Apply(MatterTaggedAsHighPriority @event, MatterDetails current)
    {
        return current with { IsHighPriority = true };
    }

    public MatterDetails Apply(StatusChanged @event, MatterDetails current)
    {
        return current with { Status = @event.NewStatus };
    }

    public MatterDetails Apply(AttorneyAssigned @event, MatterDetails current)
    {
        return current with 
        { 
            AssignedAttorneyId = @event.AttorneyId,
            Status = current.Status == MatterStatus.New ? MatterStatus.Active : current.Status
        };
    }
}
