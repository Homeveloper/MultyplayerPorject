namespace Game.Core
{
    public interface ISessionController : IPlayerRegistry
    {
        /// <summary>No-op unless called on the host. Moves everyone from Lobby into the game level.</summary>
        void RequestStart();
    }

    /// <summary>
    /// Points UI code at whichever NGO-spawned session object is currently active,
    /// without Gameplay/UI needing a direct reference to the Netcode-layer type that
    /// implements it (keeps the asmdef graph a strict DAG: Gameplay -> Core only).
    /// </summary>
    public static class ActiveSession
    {
        public static ISessionController Current { get; set; }
    }
}
