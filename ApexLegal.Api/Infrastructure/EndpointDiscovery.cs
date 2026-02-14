using ApexLegal.Api.Features.Dashboard;
using ApexLegal.Api.Features.Intake;
using ApexLegal.Api.Features.MatterManagement;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace ApexLegal.Api.Infrastructure;

public static class EndpointDiscovery
{
    public static void MapFeatureEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapIntakeEndpoints();
        app.MapMatterManagementEndpoints();
        app.MapDashboardEndpoints();
    }
}
