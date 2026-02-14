namespace ApexLegal.Web.Models;

public enum CaseType
{
    PersonalInjury,
    Employment,
    Commercial,
    RealEstate
}

public enum MatterStatus
{
    New,
    Active,
    Discovery,
    Settled,
    Closed
}

public record MatterDetails(
    Guid Id,
    string ClientName,
    string OpposingParty,
    MatterStatus Status,
    bool IsHighPriority,
    decimal CurrentClaimAmount,
    Guid? AssignedAttorneyId,
    DateTime CreatedAt
);

public record DashboardStatisticsView(
    Guid Id,
    int TotalActiveCases,
    decimal TotalPotentialSettlementValue,
    int HighPriorityCaseCount
);

public record EventRecord(
    Guid Id,
    object Data,
    string EventTypeName,
    long Version,
    long Sequence,
    DateTime Timestamp
);
