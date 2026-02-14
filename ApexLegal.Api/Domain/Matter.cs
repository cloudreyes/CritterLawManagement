using ApexLegal.Api.Domain.Events;

namespace ApexLegal.Api.Domain;

public class Matter
{
    public Guid Id { get; private set; }
    public Guid ClientId { get; private set; }
    public string ClientName { get; private set; } = default!;
    public string OpposingParty { get; private set; } = default!;
    public MatterStatus Status { get; private set; }
    public bool IsHighPriority { get; private set; }
    public decimal CurrentClaimAmount { get; private set; }
    public Guid? AssignedAttorneyId { get; private set; }

    // Marten requires parameterless constructor for rehydration
    public Matter() { }

    // Apply methods - called during event replay
    public void Apply(MatterOpened @event)
    {
        Id = @event.MatterId;
        ClientId = @event.ClientId;
        ClientName = @event.ClientName;
        OpposingParty = @event.OpposingParty;
        Status = MatterStatus.New;
        CurrentClaimAmount = @event.InitialClaimAmount;
    }

    public void Apply(MatterTaggedAsHighPriority @event)
    {
        IsHighPriority = true;
    }

    public void Apply(StatusChanged @event)
    {
        Status = @event.NewStatus;
    }

    public void Apply(AttorneyAssigned @event)
    {
        AssignedAttorneyId = @event.AttorneyId;
        Status = MatterStatus.Active;
    }
}
