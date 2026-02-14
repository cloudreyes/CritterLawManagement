using Marten;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ApexLegal.Api.Features.Dashboard;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dashboard", async (
            IQuerySession session,
            CancellationToken ct) =>
        {
            var stats = await session.LoadAsync<DashboardStatisticsView>(DashboardStatisticsProjection.DashboardId, ct);
            
            // If no events have been processed yet, stats might be null
            return stats ?? new DashboardStatisticsView
            {
                Id = DashboardStatisticsProjection.DashboardId,
                TotalActiveCases = 0,
                TotalPotentialSettlementValue = 0,
                HighPriorityCaseCount = 0
            };
        })
        .WithName("GetDashboard")
        .WithTags("Dashboard");
    }
}
