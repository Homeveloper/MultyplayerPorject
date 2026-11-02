using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Stats/achievements sink. Called locally, on whichever machine the game event
    /// happened on (each player tracks their own Steam stats) - never routed through
    /// the host. Default implementation just logs, so the game works with no Steam.
    /// </summary>
    public interface IStatsService
    {
        void Increment(string statName, int amount = 1);
        void Flush(); // StoreStats-equivalent, debounced by the implementation
    }

    public sealed class LoggingStatsService : IStatsService
    {
        public void Increment(string statName, int amount = 1) =>
            Debug.Log($"[Stats] {statName} +{amount} (no Steam - logged only)");
        public void Flush() { }
    }
}
