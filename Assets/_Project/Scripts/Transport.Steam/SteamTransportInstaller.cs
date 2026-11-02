// Compiled only once the Steamworks SDK (Facepunch.Steamworks + steam_api64.dll) and the
// community Steam P2P transport package have been added - see README "Steam setup".
// Define GAME_STEAM_SDK in Project Settings > Player > Scripting Define Symbols to enable.
#if GAME_STEAM_SDK
using Game.Core;
using Netcode.Transports.Facepunch;
using Steamworks;
using Unity.Netcode;

namespace Game.Transport.Steam
{
    /// <summary>Configures NGO's Facepunch Steam P2P transport (Steam Datagram Relay) - no IP/port needed.</summary>
    public class SteamTransportInstaller : ITransportConfigurator
    {
        readonly NetworkManager _networkManager;

        public TransportType Type => TransportType.SteamP2P;

        public SteamTransportInstaller(NetworkManager networkManager)
        {
            _networkManager = networkManager;
        }

        FacepunchTransport Transport
        {
            get
            {
                var t = _networkManager.NetworkConfig.NetworkTransport as FacepunchTransport;
                if (t == null)
                {
                    t = _networkManager.gameObject.GetComponent<FacepunchTransport>();
                    if (t == null) t = _networkManager.gameObject.AddComponent<FacepunchTransport>();
                    _networkManager.NetworkConfig.NetworkTransport = t;
                }
                return t;
            }
        }

        public void ConfigureForHost(AppConfig config)
        {
            // Host listens for Steam P2P connections; no explicit target needed.
        }

        public void ConfigureForClient(AppConfig config, string joinTarget)
        {
            if (ulong.TryParse(joinTarget, out var steamId))
                Transport.targetSteamId = steamId;
        }
    }
}
#endif
