#if GAME_STEAM_SDK
using System;
using Steamworks;
using UnityEngine;

namespace Game.Transport.Steam
{
    // owns Steamworks init/shutdown, lets the rest of the game fall back to Unity Transport
    // instead of crashing when Steam isn't running
    public class SteamBootstrap : IDisposable
    {
        public bool IsAvailable { get; private set; }

        // fires whenever SteamClient.Init() actually runs again after being shut down - native
        // callback subscriptions (e.g. SteamFriends.OnGameLobbyJoinRequested) can go stale across
        // that cycle, so anyone relying on one should re-attach when this fires
        public event Action Reinitialized;

        uint _appId;

        public bool TryInit(uint appId)
        {
            _appId = appId;
            try
            {
                SteamClient.Init(appId, asyncCallbacks: true);
                // Init() can return fine even when Steam isn't actually ready - caught this
                // from a later SteamClient.SteamId NRE. IsValid is the real signal.
                IsAvailable = SteamClient.IsValid;
                if (!IsAvailable)
                    Debug.LogWarning("[Steam] SteamClient.Init() returned without error but IsValid is false - falling back to Unity Transport.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Steam] Steam unavailable, falling back to Unity Transport: {e.Message}");
                IsAvailable = false;
            }
            return IsAvailable;
        }

        // FacepunchTransport.Shutdown() calls the global SteamClient.Shutdown(), not just a P2P
        // teardown - so any NetworkManager.Shutdown() (a kick, our own retry cleanup, a host
        // leaving) kills Steam for the rest of the process. Every Steamworks call anywhere
        // (lobby create/join, not just the transport) needs to go through this first, or a
        // kicked client's rejoin attempt fails at the lobby step before the transport even runs.
        public bool EnsureValid()
        {
            if (SteamClient.IsValid) { IsAvailable = true; return true; }
            try
            {
                SteamClient.Init(_appId, asyncCallbacks: true);
                IsAvailable = SteamClient.IsValid;
                if (IsAvailable) Reinitialized?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Steam] Re-init failed: {e.Message}");
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
