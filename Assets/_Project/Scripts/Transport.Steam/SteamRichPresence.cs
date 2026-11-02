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
