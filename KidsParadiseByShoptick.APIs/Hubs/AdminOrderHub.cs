using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace KidsParadiseByShoptick.APIs.Hubs;

[Authorize(Roles = "Admin")]
public class AdminOrderHub : Hub
{
    public const string GroupName = "admin-order-alerts";

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName);
        await base.OnConnectedAsync();
    }
}
