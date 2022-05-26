using Microsoft.AspNetCore.SignalR;

namespace CheckersWeb.Hubs;

public class GameHub : Hub
{
    public async Task AddToGroup(int id) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, id.ToString());
}