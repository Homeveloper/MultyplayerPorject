namespace Game.Core
{
    /// <summary>Same pattern as <see cref="ActiveSession"/>: points at whichever NGO-spawned
    /// PingTracker is live, since it can't be a DI-registered singleton (NGO owns its lifecycle).</summary>
    public static class ActivePing
    {
        public static IPingProvider Current { get; set; }
    }
}
