// Compiled only once the Steamworks SDK (Facepunch.Steamworks + steam_api64.dll) and the
// community Steam P2P transport package have been added - see README "Steam setup".
// Define GAME_STEAM_SDK in Project Settings > Player > Scripting Define Symbols to enable.
#if GAME_STEAM_SDK
using System;
using System.Threading.Tasks;
using Game.Core;
using Steamworks;
using Steamworks.Data;

namespace Game.Transport.Steam
{
    public class SteamLobbyService : ILobbyService, IDisposable
    {
        Lobby? _current;

        public bool IsAvailable => SteamClient.IsValid;
        public string CurrentLobbyId => _current?.Id.Value.ToString();

        public event Action<string> LobbyJoinRequestedByOverlay;

        public SteamLobbyService()
        {
            if (!SteamClient.IsValid) return;
            SteamFriends.OnGameLobbyJoinRequested += OnOverlayJoinRequested;
        }

        void OnOverlayJoinRequested(Lobby lobby, SteamId steamId) =>
            LobbyJoinRequestedByOverlay?.Invoke(lobby.Id.Value.ToString());

        public async Task<bool> CreateLobbyAsync(LobbySettings settings)
        {
            if (!IsAvailable) return false;
            var result = await SteamMatchmaking.CreateLobbyAsync(settings.MaxPlayers);
            if (!result.HasValue) return false;

            _current = result.Value;
            _current.Value.SetData("name", settings.Name);
            _current.Value.SetJoinable(true);
            if (settings.Visibility == LobbyVisibility.FriendsOnly)
                _current.Value.SetFriendsOnly();
            else
                _current.Value.SetPrivate();
            return true;
        }

        public async Task<bool> JoinLobbyAsync(string lobbyId)
        {
            if (!IsAvailable || !ulong.TryParse(lobbyId, out var id)) return false;
            var lobby = new Lobby(id);
            var result = await lobby.Join();
            if (result == RoomEnter.Success)
            {
                _current = lobby;
                return true;
            }
            return false;
        }

        public void LeaveLobby()
        {
            _current?.Leave();
            _current = null;
        }

        public void SetJoinable(bool joinable) => _current?.SetJoinable(joinable);

        public void OpenInviteOverlay()
        {
            if (_current.HasValue) SteamFriends.OpenGameInviteOverlay(_current.Value.Id);
        }

        /// <summary>Call when the host leaves - closes the lobby for everyone, per the brief.</summary>
        public void CloseLobbyOnHostLeave()
        {
            if (!_current.HasValue) return;
            _current.Value.Leave();
            _current = null;
        }

        public void Dispose()
        {
            if (IsAvailable) SteamFriends.OnGameLobbyJoinRequested -= OnOverlayJoinRequested;
        }
    }
}
#endif
