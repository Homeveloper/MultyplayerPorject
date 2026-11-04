namespace Game.Core
{
    // same deal as ActiveSession - PingTracker is NGO-owned so it can't be a DI singleton
    public static class ActivePing
    {
        public static IPingProvider Current { get; set; }
    }
}
