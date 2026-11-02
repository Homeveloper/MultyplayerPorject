// Compiled only once the Steamworks SDK (Facepunch.Steamworks + steam_api64.dll) and the
// community Steam P2P transport package have been added - see README "Steam setup".
// Define GAME_STEAM_SDK in Project Settings > Player > Scripting Define Symbols to enable.
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
