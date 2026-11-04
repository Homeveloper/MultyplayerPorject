using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Game.Core
{
    public enum LobbyVisibility { FriendsOnly, Private }

    public readonly struct LobbySettings
    {
        public readonly string Name;
        public readonly int MaxPlayers;
        public readonly LobbyVisibility Visibility;
        public LobbySettings(string name, int maxPlayers, LobbyVisibility visibility)
        {
            Name = name; MaxPlayers = maxPlayers; Visibility = visibility;
        }
    }

    public readonly struct FriendInfo
    {
        public readonly string SteamId;
        public readonly string Name;
        public readonly bool IsOnline;
        public FriendInfo(string steamId, string name, bool isOnline)
        {
            SteamId = steamId; Name = name; IsOnline = isOnline;
        }
    }

    // create/invite/join by id. No-op for Unity Transport, wraps Steam Lobbies for Steam.
    public interface ILobbyService
    {
        bool IsAvailable { get; }
        string CurrentLobbyId { get; }
        string CurrentLobbyOwnerId { get; } // SteamID64 of whoever to connect the transport to
        string CurrentJoinCode { get; } // short human-typeable code - the on-screen fallback join method

        // call eagerly on landing back in the menu (not lazily on first use) - a disconnect can
        // have killed the native Steam session behind our back, and a dead session never fires
        // the "friend accepted your invite" callback, so we can't wait for that callback to ask it
        void EnsureReady();

        Task<bool> CreateLobbyAsync(LobbySettings settings);
        Task<bool> JoinLobbyAsync(string lobbyIdOrJoinCode);
        void LeaveLobby();
        void SetJoinable(bool joinable);
        void SetPlayerCount(int current, int max); // keeps the Steam lobby's displayed count in sync with the roster
        void OpenInviteOverlay();

        // In-game invite list, since the native Steam overlay doesn't reliably hook into every
        // process (observed: Shift+Tab and OpenInviteOverlay both no-op on some setups) - this
        // bypasses the overlay entirely via the friends-list and lobby-invite API directly.
        IReadOnlyList<FriendInfo> GetFriends();
        void InviteFriend(string friendSteamId);

        void CloseLobbyOnHostLeave();

        event Action<string> LobbyJoinRequestedByOverlay; // e.g. "Join Game" from friends list
    }

    public sealed class NullLobbyService : ILobbyService
    {
        public bool IsAvailable => false;
        public string CurrentLobbyId => null;
        public string CurrentLobbyOwnerId => null;
        public string CurrentJoinCode => null;
        public void EnsureReady() { }
        public Task<bool> CreateLobbyAsync(LobbySettings settings) => Task.FromResult(false);
        public Task<bool> JoinLobbyAsync(string lobbyIdOrJoinCode) => Task.FromResult(false);
        public void LeaveLobby() { }
        public void SetJoinable(bool joinable) { }
        public void SetPlayerCount(int current, int max) { }
        public void OpenInviteOverlay() { }
        public IReadOnlyList<FriendInfo> GetFriends() => Array.Empty<FriendInfo>();
        public void InviteFriend(string friendSteamId) { }
        public void CloseLobbyOnHostLeave() { }
        public event Action<string> LobbyJoinRequestedByOverlay { add { } remove { } }
    }
}
