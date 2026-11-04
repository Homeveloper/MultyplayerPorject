#if GAME_STEAM_SDK
using Game.Core;
using Steamworks;

namespace Game.Transport.Steam
{
    public class SteamPlayerIdentityProvider : IPlayerIdentityProvider
    {
        public PlayerId GetLocalPlayerId() => new PlayerId(SteamClient.SteamId.Value.ToString());
        public string GetLocalPlayerName() => SteamClient.Name;
    }
}
#endif
