using ApexLegal.Api.Domain.Events;
using Marten;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace ApexLegal.Api.Features.MatterManagement;

public record UpdateStatusRequest(
    MatterStatus NewStatus,
    string Reason
);

public static class MatterManagementEndpoints
{
    public static void MapMatterManagementEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/matters/{matterId:guid}", async (
            Guid matterId,
            IQuerySession session,
            CancellationToken ct) =>
        {
            var matter = await session.LoadAsync<MatterDetails>(matterId, ct);
            return matter is not null ? Results.Ok(matter) : Results.NotFound();
        })
        .WithName("GetMatter")
        .WithTags("MatterManagement");

        app.MapGet("/api/matters/{matterId:guid}/history", async (
            Guid matterId,
            IQuerySession session,
            CancellationToken ct) =>
        {
            var events = await session.Events.FetchStreamAsync(matterId, token: ct);
            var result = events.Select(e => new
            {
                e.Id,
                e.Version,
                e.Sequence,
                e.Timestamp,
                EventTypeName = e.EventType.Name,
                Data = e.Data
            });
            return Results.Ok(result);
        })
        .WithName("GetMatterHistory")
        .WithTags("MatterManagement");

        app.MapPost("/api/matters/{matterId:guid}/status", async (
            Guid matterId,
            UpdateStatusRequest request,
            IDocumentSession session,
            IMessageBus bus,
            CancellationToken ct) =>
        {
            var matter = await session.LoadAsync<MatterDetails>(matterId, ct);
            if (matter == null) return Results.NotFound();

            var @event = new StatusChanged(
                matterId,
                matter.Status,
                request.NewStatus,
                request.Reason,
                DateTime.UtcNow
            );

            session.Events.Append(matterId, @event);
            await session.SaveChangesAsync(ct);

            // Publish through Wolverine so workflow handlers (e.g., DiscoveryWorkflowHandler) are triggered
            await bus.PublishAsync(@event);

            return Results.Accepted($"/api/matters/{matterId}", @event);
        })
        .WithName("UpdateStatus")
        .WithTags("MatterManagement");
    }
}
