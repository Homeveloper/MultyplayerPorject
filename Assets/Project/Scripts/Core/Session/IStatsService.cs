using UnityEngine;

namespace Game.Core
{
    // called locally on whoever triggered the event, never routed through the host
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
