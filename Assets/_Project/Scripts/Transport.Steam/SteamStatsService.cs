// Compiled only once the Steamworks SDK (Facepunch.Steamworks + steam_api64.dll) and the
// community Steam P2P transport package have been added - see README "Steam setup".
// Define GAME_STEAM_SDK in Project Settings > Player > Scripting Define Symbols to enable.
#if GAME_STEAM_SDK
using System.Collections.Generic;
using Game.Core;
using Steamworks;
using UnityEngine;

namespace Game.Transport.Steam
{
    /// <summary>
    /// Writes to this machine's own Steam account stats - called locally wherever the
    /// triggering event happens (e.g. the throwing client increments "items_thrown"),
    /// never routed through the host. StoreStats is debounced to avoid spamming Steam.
    /// </summary>
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
