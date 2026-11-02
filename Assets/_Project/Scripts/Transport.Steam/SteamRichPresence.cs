// Compiled only once the Steamworks SDK (Facepunch.Steamworks + steam_api64.dll) and the
// community Steam P2P transport package have been added - see README "Steam setup".
// Define GAME_STEAM_SDK in Project Settings > Player > Scripting Define Symbols to enable.
#if GAME_STEAM_SDK
using Steamworks;

namespace Game.Transport.Steam
{
    /// <summary>Updates the "status in friends list" text (e.g. "In Lobby", "In Game 2/4").</summary>
    public static class SteamRichPresence
    {
        public static void SetInLobby()
        {
            if (!SteamClient.IsValid) return;
            SteamFriends.SetRichPresence("steam_display", "#StatusLobby");
        }

        public static void SetInGame(int current, int max)
        {
            if (!SteamClient.IsValid) return;
            SteamFriends.SetRichPresence("steam_display", "#StatusInGame");
            SteamFriends.SetRichPresence("players", $"{current}/{max}");
        }

        public static void Clear()
        {
            if (!SteamClient.IsValid) return;
            SteamFriends.SetRichPresence("steam_display", null);
        }
    }
}
#endif
