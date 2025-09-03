using Microsoft.AspNetCore.SignalR;

namespace SignalRTest.Services
{
    public class CustomUserIdProvider : IUserIdProvider
    {
        public virtual string? GetUserId(HubConnectionContext connection)
        {
            // First try to get from session
            var sessionUserId = connection.GetHttpContext()?.Session?.GetString("UserId");
            if (!string.IsNullOrEmpty(sessionUserId))
            {
                return sessionUserId;
            }

            // Fallback to authenticated user identity
            var userIdentity = connection.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(userIdentity))
            {
                return userIdentity;
            }

            // Last resort: use connection ID
            return connection.ConnectionId;
        }
    }
}