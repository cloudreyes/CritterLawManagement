namespace ApexLegal.Api.Domain.Events;

public record ClientCreated(
    Guid ClientId,
    string Name,
    DateTime OccurredAt
);
