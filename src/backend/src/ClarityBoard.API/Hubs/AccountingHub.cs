using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ClarityBoard.API.Hubs;

[Authorize]
public class AccountingHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var entityId = Context.User?.FindFirst("entity_id")?.Value;
        if (!string.IsNullOrEmpty(entityId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"accounting:{entityId}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var entityId = Context.User?.FindFirst("entity_id")?.Value;
        if (!string.IsNullOrEmpty(entityId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"accounting:{entityId}");
        }
        await base.OnDisconnectedAsync(exception);
    }
}
