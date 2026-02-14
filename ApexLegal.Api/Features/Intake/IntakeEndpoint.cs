using ApexLegal.Api.Domain.Events;
using ApexLegal.Api.Features.MatterManagement;
using Marten;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace ApexLegal.Api.Features.Intake;

public record IntakeRequest(
    Guid ClientId,
    string OpposingParty,
    CaseType CaseType,
    decimal InitialClaimAmount,
    bool ConfirmDuplicate = false
);

public static class IntakeEndpoint
{
    public static void MapIntakeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/intake", async (
            IntakeRequest request,
            IMessageBus bus,
            IQuerySession session,
            CancellationToken ct) =>
        {
            // Duplicate check: warn (don't block) if all four fields match
            if (!request.ConfirmDuplicate)
            {
                var duplicateExists = await session.Query<MatterDetails>()
                    .AnyAsync(m =>
                        m.ClientId == request.ClientId &&
                        m.OpposingParty == request.OpposingParty &&
                        m.CaseType == request.CaseType &&
                        m.CurrentClaimAmount == request.InitialClaimAmount, ct);

                if (duplicateExists)
                    return Results.Conflict(new
                    {
                        type = "duplicate",
                        message = "A matter with identical client, opposing party, type, and amount already exists."
                    });
            }

            var command = new OpenMatterCommand(
                request.ClientId,
                request.OpposingParty,
                request.CaseType,
                request.InitialClaimAmount
            );

            var result = await bus.InvokeAsync<MatterOpened>(command, ct);

            return Results.Created($"/api/matters/{result.MatterId}", result);
        })
        .WithName("OpenMatter")
        .WithTags("Intake");
    }
}
