using System;
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

    /// <summary>
    /// Front-end lobby concept (create/invite/join by id). For Unity Transport this is a
    /// no-op (<see cref="NullLobbyService"/>); for Steam it wraps Steam Lobbies and keeps
    /// the lobby's joinable/player-count state in sync with the netcode session.
    /// </summary>
    public interface ILobbyService
    {
        bool IsAvailable { get; }
        string CurrentLobbyId { get; }

        Task<bool> CreateLobbyAsync(LobbySettings settings);
        Task<bool> JoinLobbyAsync(string lobbyId);
        void LeaveLobby();
        void SetJoinable(bool joinable);
        void OpenInviteOverlay();

        event Action<string> LobbyJoinRequestedByOverlay; // e.g. "Join Game" from friends list
    }

    /// <summary>Default no-op lobby for transports without a native lobby concept (Unity Transport/IP).</summary>
    public sealed class NullLobbyService : ILobbyService
    {
        public bool IsAvailable => false;
        public string CurrentLobbyId => null;
        public Task<bool> CreateLobbyAsync(LobbySettings settings) => Task.FromResult(false);
        public Task<bool> JoinLobbyAsync(string lobbyId) => Task.FromResult(false);
        public void LeaveLobby() { }
        public void SetJoinable(bool joinable) { }
        public void OpenInviteOverlay() { }
        public event Action<string> LobbyJoinRequestedByOverlay { add { } remove { } }
    }
}
