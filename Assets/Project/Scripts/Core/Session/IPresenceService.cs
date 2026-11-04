namespace Game.Core
{
    // "status in friends list" - Steam Rich Presence behind this, no-op for Unity Transport
    public interface IPresenceService
    {
        void SetInLobby();
        void SetInGame(int current, int max);
        void Clear();
    }

    public sealed class NullPresenceService : IPresenceService
    {
        public void SetInLobby() { }
        public void SetInGame(int current, int max) { }
        public void Clear() { }
    }
}
