#if GAME_STEAM_SDK
using System.Collections.Generic;
using Game.Core;
using Steamworks;
using UnityEngine;

namespace Game.Transport.Steam
{
    // writes to this machine's own Steam stats; StoreStats debounced so we don't spam Steam
    public class SteamStatsService : IStatsService
    {
        readonly Dictionary<string, int> _pending = new();
        float _lastFlush;
        const float FlushIntervalSeconds = 5f;

        public void Increment(string statName, int amount = 1)
        {
            if (!SteamClient.IsValid)
            {
                Debug.Log($"[Stats:Steam-unavailable] {statName} +{amount}");
                return;
            }

            SteamUserStats.AddStat(statName, amount);
            _pending[statName] = _pending.TryGetValue(statName, out var p) ? p + amount : amount;

            if (Time.unscaledTime - _lastFlush > FlushIntervalSeconds)
                Flush();
        }

        public void Flush()
        {
            if (!SteamClient.IsValid || _pending.Count == 0) return;
            SteamUserStats.StoreStats();
            _pending.Clear();
            _lastFlush = Time.unscaledTime;
        }
    }
}
#endif
