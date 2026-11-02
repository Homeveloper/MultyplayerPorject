// Compiled only once the Steamworks SDK (Facepunch.Steamworks + steam_api64.dll) and the
// community Steam P2P transport package have been added - see README "Steam setup".
// Define GAME_STEAM_SDK in Project Settings > Player > Scripting Define Symbols to enable.
#if GAME_STEAM_SDK
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
#endif
