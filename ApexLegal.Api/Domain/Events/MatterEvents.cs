namespace ApexLegal.Api.Domain.Events;

public enum CaseType
{
    PersonalInjury,
    Employment,
    Commercial,
    RealEstate
}

public record MatterOpened(
    Guid MatterId,
    Guid ClientId,
    string ClientName,
    string OpposingParty,
    CaseType CaseType,
    decimal InitialClaimAmount,
    DateTime OccurredAt
);

public record MatterTaggedAsHighPriority(
    Guid MatterId,
    DateTime OccurredAt
);

public record AttorneyAssigned(
    Guid MatterId,
    Guid AttorneyId,
    DateTime AssignedAt
);

public record NoteAdded(
    Guid MatterId,
    string Text,
    string Author,
    DateTime OccurredAt
);

public enum MatterStatus
{
    New,
    Active,
    Discovery,
    Settled,
    Closed
}

public record StatusChanged(
    Guid MatterId,
    MatterStatus OldStatus,
    MatterStatus NewStatus,
    string Reason,
    DateTime OccurredAt
);

public record SettlementOfferReceived(
    Guid MatterId,
    decimal Amount,
    DateTime OccurredAt
);

public record TaskCreated(
    Guid MatterId,
    string Description,
    string AssignedTo,
    DateTime DueDate,
    DateTime CreatedAt
);

public record ClientNotificationSent(
    Guid MatterId,
    string NotificationType,
    DateTime SentAt
);
