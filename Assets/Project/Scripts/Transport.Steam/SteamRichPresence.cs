#if GAME_STEAM_SDK
using Steamworks;

namespace Game.Transport.Steam
{
    // "In Lobby" / "In Game 2/4" text in the friends list
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
