using System.Collections.Concurrent;

namespace SignalRTest.Services
{
    public class UserConnectionManagerService
    {
        private static readonly ConcurrentDictionary<string, HashSet<string>> _connections =
            new ConcurrentDictionary<string, HashSet<string>>();

        // user -> current partner (single active pairing)
        private static readonly ConcurrentDictionary<string, string> _partnerOf =
            new ConcurrentDictionary<string, string>();

        // queue of users waiting to be paired (user-level, not connection-level)
        private static readonly HashSet<string> _waiting = new HashSet<string>();
        private static readonly object _waitLock = new object();

        public bool AddConnection(string userId, string connectionId)
        {
            var set = _connections.GetOrAdd(userId, _ => new HashSet<string>());
            lock (set)
            {
                var wasEmpty = set.Count == 0;
                set.Add(connectionId);
                return wasEmpty; // true if this is the first connection of this user
            }
        }

        public bool RemoveConnection(string userId, string connectionId)
        {
            if (_connections.TryGetValue(userId, out var set))
            {
                lock (set)
                {
                    set.Remove(connectionId);
                    if (set.Count == 0)
                    {
                        _connections.TryRemove(userId, out _);
                        // If user goes fully offline, ensure they are not left in the waiting queue
                        lock (_waitLock) _waiting.Remove(userId);
                        return true; // last connection removed
                    }
                }
            }
            return false;
        }

        public IEnumerable<string> GetAllUsers() => _connections.Keys;
        public IEnumerable<string> GetConnections(string userId) =>
            _connections.TryGetValue(userId, out var set) ? set : Enumerable.Empty<string>();
        public bool IsOnline(string userId) => _connections.ContainsKey(userId);

        public string? GetPartner(string userId) =>
            _partnerOf.TryGetValue(userId, out var p) ? p : null;

        public string GetGroupName(string a, string b)
        {
            // canonical group name for this pair
            return "pair:" + (string.CompareOrdinal(a, b) <= 0 ? $"{a}|{b}" : $"{b}|{a}");
        }

        /// <summary>
        /// Called when a user's first connection comes online: either pair them immediately,
        /// or put them into the waiting queue. Returns the partner if paired, otherwise null.
        /// </summary>
        public string? TryAutoPairOnOnline(string userId)
        {
            // Already paired? nothing to do.
            if (_partnerOf.ContainsKey(userId)) return GetPartner(userId);

            lock (_waitLock)
            {
                // Remove self if somehow already present
                _waiting.Remove(userId);

                // Find any other waiting user that is online and not already paired
                var other = _waiting.FirstOrDefault(u => u != userId && !_partnerOf.ContainsKey(u) && IsOnline(u));
                if (other != null)
                {
                    // Pair them
                    _waiting.Remove(other);
                    _partnerOf[userId] = other;
                    _partnerOf[other] = userId;
                    return other;
                }

                // No one available -> enqueue this user
                _waiting.Add(userId);
                return null;
            }
        }

        /// <summary>
        /// Unpairs the user (if had a partner) and returns that old partner (or null).
        /// The old partner is re-queued (if still online and not paired) to get matched again.
        /// </summary>
        public string? Unpair(string userId)
        {
            if (!_partnerOf.TryRemove(userId, out var oldPartner)) return null;
            _partnerOf.TryRemove(oldPartner, out _);

            // Re-queue the old partner if still online
            if (IsOnline(oldPartner))
            {
                lock (_waitLock)
                {
                    if (!_partnerOf.ContainsKey(oldPartner))
                        _waiting.Add(oldPartner);
                }
            }
            return oldPartner;
        }

        /// <summary>
        /// After someone becomes unpaired, try to match them again immediately.
        /// Returns the new partner if paired, else null (they remain waiting).
        /// </summary>
        public string? TryRePairNow(string userId)
        {
            // If already paired, return current partner
            if (_partnerOf.ContainsKey(userId)) return GetPartner(userId);
            return TryAutoPairOnOnline(userId);
        }
    }
}
