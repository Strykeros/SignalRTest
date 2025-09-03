using Microsoft.AspNetCore.SignalR;
using SignalRTest.Services;

namespace SignalRTest.Hubs
{
public class ChatHub : Hub
{
    private readonly UserConnectionManagerService _connectionManager;
    public ChatHub(UserConnectionManagerService connectionManager) => _connectionManager = connectionManager;

    // Prefer UserIdentifier 
    private string CurrentUserId => Context.UserIdentifier ?? Context.ConnectionId;
    public string GetUserId() => CurrentUserId;

    public override async Task OnConnectedAsync()
    {
        var me = CurrentUserId;

        // Track this connection
        var isFirst = _connectionManager.AddConnection(me, Context.ConnectionId);

        // If I already have a partner (e.g., opening a new tab) just join the existing group and notify caller
        var existingPartner = _connectionManager.GetPartner(me);
        if (existingPartner is not null)
        {
            var group = _connectionManager.GetGroupName(me, existingPartner);
            await Groups.AddToGroupAsync(Context.ConnectionId, group, Context.ConnectionAborted);
            await Clients.Caller.SendAsync("PairedWith", existingPartner);
        }
        // Otherwise, if this is my first connection, try to auto-pair me
        else if (isFirst)
        {
            var partner = _connectionManager.TryAutoPairOnOnline(me);
            if (partner is null)
            {
                await Clients.Caller.SendAsync("Waiting");
            }
            else
            {
                var group = _connectionManager.GetGroupName(me, partner);
                await AddBothSidesToGroupAsync(me, partner, group);
                await NotifyPairedAsync(me, partner);
            }
        }
        // else: not first connection and no partner — do nothing (the caller remains "waiting")

        await Clients.All.SendAsync("UserListUpdated", _connectionManager.GetAllUsers());
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var me = CurrentUserId;

        // Remove this connection; see if this was the user's last open connection
        var isLast = _connectionManager.RemoveConnection(me, Context.ConnectionId);

        if (isLast)
        {
            // If fully offline and paired, unpair and try to re-pair the remaining user
            var oldPartner = _connectionManager.Unpair(me);
            if (oldPartner is not null)
            {
                // Clean up the old group for both users
                var oldGroup = _connectionManager.GetGroupName(me, oldPartner);
                await RemoveBothSidesFromGroupAsync(me, oldPartner, oldGroup);

                // Tell the remaining partner they were unpaired
                await Clients.Clients(_connectionManager.GetConnections(oldPartner))
                    .SendAsync("Unpaired", me);

                // Try to instantly re-pair that partner
                var newMate = _connectionManager.TryRePairNow(oldPartner);
                if (newMate is null)
                {
                    await Clients.Clients(_connectionManager.GetConnections(oldPartner))
                        .SendAsync("Waiting");
                }
                else
                {
                    var newGroup = _connectionManager.GetGroupName(oldPartner, newMate);
                    await AddBothSidesToGroupAsync(oldPartner, newMate, newGroup);
                    await NotifyPairedAsync(oldPartner, newMate);
                }
            }
        }

        await Clients.All.SendAsync("UserListUpdated", _connectionManager.GetAllUsers());
        await base.OnDisconnectedAsync(exception);
    }

    // Send to your current partner (no need to specify user id)
    public async Task SendToPartner(string message)
    {
        var me = CurrentUserId;
        if (string.IsNullOrWhiteSpace(message)) return;

        var partner = _connectionManager.GetPartner(me);
        if (partner is null)
            throw new HubException("You are not paired.");

        var targets = _connectionManager.GetConnections(partner);
        if (targets is not null)
        {
            await Clients.Clients(targets).SendAsync("ReceiveMessage", me, message);
        }
    }

    // (Optional) Still allow explicit targeting by user id
    public async Task SendMessage(string user, string message)
    {
        var me = CurrentUserId;
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(message)) return;

        var targets = _connectionManager.GetConnections(user);
        if (targets is not null)
        {
            await Clients.Clients(targets).SendAsync("ReceiveMessage", me, message);
        }
    }

    public Task<IEnumerable<string>> GetConnectedUsers()
        => Task.FromResult(_connectionManager.GetAllUsers());

    // ----------------- helpers -----------------

    private async Task AddBothSidesToGroupAsync(string a, string b, string group)
    {
        foreach (var c in _connectionManager.GetConnections(a))
            await Groups.AddToGroupAsync(c, group, Context.ConnectionAborted);

        foreach (var c in _connectionManager.GetConnections(b))
            await Groups.AddToGroupAsync(c, group, Context.ConnectionAborted);
    }

    private async Task RemoveBothSidesFromGroupAsync(string a, string b, string group)
    {
        foreach (var c in _connectionManager.GetConnections(a))
            await Groups.RemoveFromGroupAsync(c, group, Context.ConnectionAborted);

        foreach (var c in _connectionManager.GetConnections(b))
            await Groups.RemoveFromGroupAsync(c, group, Context.ConnectionAborted);
    }

    private async Task NotifyPairedAsync(string a, string b)
    {
        var aConns = _connectionManager.GetConnections(a);
        var bConns = _connectionManager.GetConnections(b);

        if (aConns is not null)
            await Clients.Clients(aConns).SendAsync("PairedWith", b);

        if (bConns is not null)
            await Clients.Clients(bConns).SendAsync("PairedWith", a);
    }
}
}
