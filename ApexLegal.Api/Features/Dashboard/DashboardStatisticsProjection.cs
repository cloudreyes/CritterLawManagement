using ApexLegal.Api.Domain.Events;
using Marten.Events.Projections;

namespace ApexLegal.Api.Features.Dashboard;

public class DashboardStatisticsView
{
    public Guid Id { get; set; }
    public int TotalActiveCases { get; set; }
    public decimal TotalPotentialSettlementValue { get; set; }
    public int HighPriorityCaseCount { get; set; }
}

public class DashboardStatisticsProjection : MultiStreamProjection<DashboardStatisticsView, Guid>
{
    public static readonly Guid DashboardId = Guid.Parse("018da675-9b36-7c98-a836-9321f6494901");

    public DashboardStatisticsProjection()
    {
        Identity<MatterOpened>(_ => DashboardId);
        Identity<MatterTaggedAsHighPriority>(_ => DashboardId);
        Identity<StatusChanged>(_ => DashboardId);
    }

    public void Apply(MatterOpened @event, DashboardStatisticsView view)
    {
        view.Id = DashboardId;
        view.TotalActiveCases++;
        view.TotalPotentialSettlementValue += @event.InitialClaimAmount;
    }

    public void Apply(MatterTaggedAsHighPriority @event, DashboardStatisticsView view)
    {
        view.HighPriorityCaseCount++;
    }

    public void Apply(StatusChanged @event, DashboardStatisticsView view)
    {
        if (@event.NewStatus == MatterStatus.Closed)
        {
            view.TotalActiveCases--;
        }
    }
}
