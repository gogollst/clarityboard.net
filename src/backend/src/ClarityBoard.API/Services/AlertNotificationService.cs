using ClarityBoard.API.Hubs;
using ClarityBoard.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace ClarityBoard.API.Services;

/// <summary>
/// Pushes alert events to connected clients via SignalR AlertHub.
/// </summary>
public class AlertNotificationService : IAlertNotificationService
{
    private readonly IHubContext<AlertHub> _hubContext;

    public AlertNotificationService(IHubContext<AlertHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendAlertAsync(
        Guid entityId,
        object alertPayload,
        CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group($"alerts:{entityId}")
            .SendAsync("AlertTriggered", alertPayload, ct);
    }
}
