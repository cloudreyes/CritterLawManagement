using ApexLegal.Api.Domain.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace ApexLegal.Api.Features.Intake;

public record IntakeRequest(
    string ClientName,
    string OpposingParty,
    CaseType CaseType,
    decimal InitialClaimAmount
);

public static class IntakeEndpoint
{
    public static void MapIntakeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/intake", async (
            IntakeRequest request,
            IMessageBus bus,
            CancellationToken ct) =>
        {
            var command = new OpenMatterCommand(
                request.ClientName,
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
