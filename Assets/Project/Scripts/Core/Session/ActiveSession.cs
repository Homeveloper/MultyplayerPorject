using UnityEngine;

namespace Game.Core
{
    public interface ISessionController : IPlayerRegistry
    {
        // no-op unless called on the host
        void RequestStart();

        // lets a player who disconnects and reconnects within the same hosted session get their
        // chosen color back instead of resetting to default - host-only, no-op on a client
        bool TryGetLastKnownColor(string playerId, out Color color);
        void RememberColor(string playerId, Color color);
    }

    // points UI at whichever NGO-spawned session object is active right now, so Gameplay/UI
    // doesn't need a reference to the Netcode-layer type
    public static class ActiveSession
    {
        static ISessionController _current;

        public static ISessionController Current
        {
            get => _current;
            set
            {
                _current = value;
                Changed?.Invoke();
            }
        }

        // fires on (re)assign - a listener's OnEnable can run before the roster NetworkObject
        // finishes syncing on a fresh connection, so reading Current once there isn't enough
        public static event System.Action Changed;
    }
}
