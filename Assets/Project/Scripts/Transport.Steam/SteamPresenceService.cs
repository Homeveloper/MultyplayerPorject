#if GAME_STEAM_SDK
using Game.Core;

namespace Game.Transport.Steam
{
    public class SteamPresenceService : IPresenceService
    {
        public void SetInLobby() => SteamRichPresence.SetInLobby();
        public void SetInGame(int current, int max) => SteamRichPresence.SetInGame(current, max);
        public void Clear() => SteamRichPresence.Clear();
    }
}
#endif
