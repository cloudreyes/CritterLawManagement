using ApexLegal.Api.Domain;
using ApexLegal.Api.Domain.Events;
using Marten;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ApexLegal.Api.Features.ClientManagement;

public record CreateClientRequest(string Name);

public static class ClientManagementEndpoints
{
    public static void MapClientManagementEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/clients", async (
            CreateClientRequest request,
            IDocumentSession session,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest("Client name is required.");

            var exists = await session.Query<ClientDetails>()
                .AnyAsync(c => c.Name == request.Name.Trim(), ct);

            if (exists)
                return Results.Conflict($"A client named '{request.Name.Trim()}' already exists.");

            var clientId = Guid.NewGuid();
            var @event = new ClientCreated(clientId, request.Name.Trim(), DateTime.UtcNow);

            session.Events.StartStream<Client>(clientId, @event);
            await session.SaveChangesAsync(ct);

            return Results.Created($"/api/clients/{clientId}", new { clientId, @event.Name });
        })
        .WithName("CreateClient")
        .WithTags("ClientManagement");

        app.MapGet("/api/clients", async (
            IQuerySession session,
            CancellationToken ct) =>
        {
            var clients = await session.Query<ClientDetails>()
                .OrderBy(c => c.Name)
                .ToListAsync(ct);

            return Results.Ok(clients);
        })
        .WithName("ListClients")
        .WithTags("ClientManagement");

        app.MapGet("/api/clients/{clientId:guid}", async (
            Guid clientId,
            IQuerySession session,
            CancellationToken ct) =>
        {
            var client = await session.LoadAsync<ClientDetails>(clientId, ct);
            return client is not null ? Results.Ok(client) : Results.NotFound();
        })
        .WithName("GetClient")
        .WithTags("ClientManagement");
    }
}
