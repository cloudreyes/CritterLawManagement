using ApexLegal.Api.Domain.Events;
using Marten.Events.Aggregation;

namespace ApexLegal.Api.Features.ClientManagement;

public record ClientDetails(
    Guid Id,
    string Name,
    DateTime CreatedAt
);

public class ClientDetailsProjection : SingleStreamProjection<ClientDetails, Guid>
{
    public ClientDetails Create(ClientCreated @event)
    {
        return new ClientDetails(
            @event.ClientId,
            @event.Name,
            @event.OccurredAt
        );
    }
}
