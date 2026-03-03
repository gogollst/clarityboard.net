using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ClarityBoard.API.Hubs;

[Authorize]
public class HrHub : Hub
{
    public async Task JoinEntityGroup(string entityId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"hr-{entityId}");
}
