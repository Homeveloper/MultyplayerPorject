namespace Game.Core
{
    public interface ISessionController : IPlayerRegistry
    {
        // no-op unless called on the host
        void RequestStart();
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
