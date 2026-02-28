using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ClarityBoard.API.Hubs;

[Authorize]
public class KpiHub : Hub
{
    private readonly ILogger<KpiHub> _logger;

    public KpiHub(ILogger<KpiHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var entityId = Context.User?.FindFirst("entity_id")?.Value;
        if (!string.IsNullOrEmpty(entityId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"entity:{entityId}");
            _logger.LogInformation("Client {ConnectionId} joined entity group {EntityId}",
                Context.ConnectionId, entityId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var entityId = Context.User?.FindFirst("entity_id")?.Value;
        if (!string.IsNullOrEmpty(entityId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"entity:{entityId}");
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SwitchEntity(string newEntityId)
    {
        var currentEntityId = Context.User?.FindFirst("entity_id")?.Value;
        if (!string.IsNullOrEmpty(currentEntityId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"entity:{currentEntityId}");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"entity:{newEntityId}");
        _logger.LogInformation("Client {ConnectionId} switched to entity {EntityId}",
            Context.ConnectionId, newEntityId);
    }
}
