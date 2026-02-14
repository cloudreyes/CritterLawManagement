using ApexLegal.Api.Domain.Events;
using Marten;
using Microsoft.Extensions.Logging;

namespace ApexLegal.Api.Features.Workflows;

public class DiscoveryWorkflowHandler
{
    private readonly ILogger<DiscoveryWorkflowHandler> _logger;

    public DiscoveryWorkflowHandler(ILogger<DiscoveryWorkflowHandler> logger)
    {
        _logger = logger;
    }

    public void Handle(
        StatusChanged @event,
        IDocumentSession session)
    {
        if (@event.NewStatus != MatterStatus.Discovery) return;

        _logger.LogInformation("Matter {MatterId} transitioned to Discovery. Starting workflow...", @event.MatterId);

        // 1. Auto-create task for discovery phase
        var taskEvent = new TaskCreated(
            @event.MatterId,
            "Request Evidence task for assigned attorney",
            "Assigned Attorney",
            DateTimeOffset.UtcNow.AddDays(7),
            DateTimeOffset.UtcNow
        );

        session.Events.Append(@event.MatterId, taskEvent);

        // 2. Simulated email notification event
        var notificationEvent = new ClientNotificationSent(
            @event.MatterId,
            "Discovery Started Email",
            DateTimeOffset.UtcNow
        );

        session.Events.Append(@event.MatterId, notificationEvent);

        // Wolverine's AutoApplyTransactions saves the session automatically

        // 3. Side effect: Log simulated email
        _logger.LogInformation("[SIMULATED EMAIL] To: Client of Matter {MatterId}, Subject: Discovery Phase Started", @event.MatterId);
    }
}
