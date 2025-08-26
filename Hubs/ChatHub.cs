using Microsoft.AspNetCore.SignalR;
using SignalRTest.Services;

namespace SignalRTest.Hubs
{
    public class ChatHub : Hub
    {
        private readonly UserConnectionManagerService _connectionManager;
        public ChatHub(UserConnectionManagerService connectionManager) => _connectionManager = connectionManager;

        private string CurrentUserId => Context.UserIdentifier ?? Context.ConnectionId;

        public override async Task OnConnectedAsync()
        {
            var me = CurrentUserId;
            var isFirst = _connectionManager.AddConnection(me, Context.ConnectionId);

            // If this user already has a partner (e.g., opened a new tab), join this connection to the pair group
            var existingPartner = _connectionManager.GetPartner(me);
            if (existingPartner is not null)
            {
                var group = _connectionManager.GetGroupName(me, existingPartner);
                await Groups.AddToGroupAsync(Context.ConnectionId, group);
                await Clients.Caller.SendAsync("PairedWith", existingPartner);
            }
            // Otherwise, if this is the first connection for this user, attempt auto-pairing
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
                    // Add ALL connections of both users to the group
                    foreach (var c in _connectionManager.GetConnections(me))
                        await Groups.AddToGroupAsync(c, group);
                    foreach (var c in _connectionManager.GetConnections(partner))
                        await Groups.AddToGroupAsync(c, group);

                    await Clients.Clients(_connectionManager.GetConnections(me))
                        .SendAsync("PairedWith", partner);
                    await Clients.Clients(_connectionManager.GetConnections(partner))
                        .SendAsync("PairedWith", me);
                }
            }

            await Clients.All.SendAsync("UserListUpdated", _connectionManager.GetAllUsers());
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var me = CurrentUserId;
            var isLast = _connectionManager.RemoveConnection(me, Context.ConnectionId);

            if (isLast)
            {
                // If fully offline AND had a partner, unpair and try to re-pair the remaining user
                var oldPartner = _connectionManager.Unpair(me);
                if (oldPartner is not null)
                {
                    // Tell partner they were unpaired and are waiting (until re-paired)
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
                        var group = _connectionManager.GetGroupName(oldPartner, newMate);
                        foreach (var c in _connectionManager.GetConnections(oldPartner))
                            await Groups.AddToGroupAsync(c, group);
                        foreach (var c in _connectionManager.GetConnections(newMate))
                            await Groups.AddToGroupAsync(c, group);

                        await Clients.Clients(_connectionManager.GetConnections(oldPartner))
                            .SendAsync("PairedWith", newMate);
                        await Clients.Clients(_connectionManager.GetConnections(newMate))
                            .SendAsync("PairedWith", oldPartner);
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
            var partner = _connectionManager.GetPartner(me);
            if (partner is null) throw new HubException("You are not paired.");
            var targets = _connectionManager.GetConnections(partner).ToList();
            if (targets.Count > 0)
            {
                await Clients.Clients(targets).SendAsync("ReceiveMessage", me, message);
            }
        }

        // (Optional) Still allow explicit targeting by user id
        public async Task SendMessage(string user, string message)
        {
            var me = CurrentUserId;
            var targets = _connectionManager.GetConnections(user).ToList();
            if (targets.Count > 0)
            {
                await Clients.Clients(targets).SendAsync("ReceiveMessage", me, message);
            }
        }

        public Task<IEnumerable<string>> GetConnectedUsers()
            => Task.FromResult(_connectionManager.GetAllUsers());
    }
}
