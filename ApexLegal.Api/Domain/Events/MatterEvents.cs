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
    string ClientName,
    string OpposingParty,
    CaseType CaseType,
    decimal InitialClaimAmount,
    DateTimeOffset OccurredAt
);

public record MatterTaggedAsHighPriority(
    Guid MatterId,
    DateTimeOffset OccurredAt
);

public record AttorneyAssigned(
    Guid MatterId,
    Guid AttorneyId,
    DateTimeOffset AssignedAt
);

public record NoteAdded(
    Guid MatterId,
    string Text,
    string Author,
    DateTimeOffset OccurredAt
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
    DateTimeOffset OccurredAt
);

public record SettlementOfferReceived(
    Guid MatterId,
    decimal Amount,
    DateTimeOffset OccurredAt
);

public record TaskCreated(
    Guid MatterId,
    string Description,
    string AssignedTo,
    DateTimeOffset DueDate,
    DateTimeOffset CreatedAt
);

public record ClientNotificationSent(
    Guid MatterId,
    string NotificationType,
    DateTimeOffset SentAt
);
