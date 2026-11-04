// needs GAME_STEAM_SDK defined (Steamworks + steam_api64.dll + the community transport package)
#if GAME_STEAM_SDK
using Game.Core;
using Netcode.Transports.Facepunch;
using Unity.Netcode;

namespace Game.Transport.Steam
{
    public class SteamTransportInstaller : ITransportConfigurator
    {
        readonly NetworkManager _networkManager;
        readonly SteamBootstrap _steamBootstrap;

        public TransportType Type => TransportType.SteamP2P;

        public SteamTransportInstaller(NetworkManager networkManager, SteamBootstrap steamBootstrap)
        {
            _networkManager = networkManager;
            _steamBootstrap = steamBootstrap;
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
            _steamBootstrap.EnsureValid();
            _ = Transport; // getter has to run once or NetworkTransport stays null and StartHost() throws
        }

        public void ConfigureForClient(AppConfig config, string joinTarget)
        {
            _steamBootstrap.EnsureValid();
            var transport = Transport; // same as above - create it even if the SteamID parse below fails
            if (ulong.TryParse(joinTarget?.Trim(), out var steamId))
                transport.targetSteamId = steamId;
        }
    }
}
#endif
