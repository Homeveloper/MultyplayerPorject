using System;
using Steamworks;
using UnityEngine;

namespace Game.Transport.Steam
{
    /// <summary>
    /// Owns Steamworks init/shutdown and exposes availability so the rest of the game
    /// can fall back to Unity Transport instead of crashing when Steam isn't running.
    /// </summary>
    public class SteamBootstrap : IDisposable
    {
        public bool IsAvailable { get; private set; }

        public bool TryInit(uint appId)
        {
            try
            {
                SteamClient.Init(appId, asyncCallbacks: true);
                IsAvailable = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Steam] Steam unavailable, falling back to Unity Transport: {e.Message}");
                IsAvailable = false;
            }
            return IsAvailable;
        }

        public void Tick()
        {
            if (IsAvailable) SteamClient.RunCallbacks();
        }

        public void Dispose()
        {
            if (IsAvailable) SteamClient.Shutdown();
            IsAvailable = false;
        }
    }
}
