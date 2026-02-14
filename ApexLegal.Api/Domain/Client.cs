using ApexLegal.Api.Domain.Events;

namespace ApexLegal.Api.Domain;

public class Client
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;

    // Marten requires parameterless constructor for rehydration
    public Client() { }

    public void Apply(ClientCreated @event)
    {
        Id = @event.ClientId;
        Name = @event.Name;
    }
}
