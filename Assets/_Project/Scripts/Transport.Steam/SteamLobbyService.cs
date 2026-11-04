#if GAME_STEAM_SDK
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Game.Core;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

namespace Game.Transport.Steam
{
    public class SteamLobbyService : ILobbyService, IDisposable
    {
        // excludes 0/O/1/I/L - easy to misread when read aloud or handwritten
        const string JoinCodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        const int JoinCodeLength = 5;

        readonly SteamBootstrap _steamBootstrap;
        readonly System.Random _rng = new();
        Lobby? _current;
        bool _subscribed;

        public bool IsAvailable => SteamClient.IsValid;
        public string CurrentLobbyId => _current?.Id.Value.ToString();
        public string CurrentLobbyOwnerId => _current?.Owner.Id.Value.ToString();
        public string CurrentJoinCode { get; private set; }

        public event Action<string> LobbyJoinRequestedByOverlay;

        public SteamLobbyService(SteamBootstrap steamBootstrap)
        {
            _steamBootstrap = steamBootstrap;
            _steamBootstrap.Reinitialized += OnSteamReinitialized;
            SubscribeIfNeeded();
        }

        // a disconnect can kill the native Steam session behind our back (NGO calls
        // transport.Shutdown() on every disconnect, which kills SteamClient globally in the
        // Facepunch transport) - that invalidates the old native callback hookup, so the stale
        // _subscribed flag must be cleared or a friend's "Join Game" accept silently does nothing
        void OnSteamReinitialized()
        {
            _subscribed = false;
            SubscribeIfNeeded();
        }

        // SteamClient.Init() (via EnsureValid, after a kick killed it) hands back a fresh native
        // session - re-subscribing here is cheap and avoids silently losing this callback.
        void SubscribeIfNeeded()
        {
            if (_subscribed || !SteamClient.IsValid) return;
            SteamFriends.OnGameLobbyJoinRequested += OnOverlayJoinRequested;
            _subscribed = true;
        }

        // call eagerly when landing back in the menu - don't wait for the next lobby action to
        // discover the native session died, because the overlay-join callback this depends on
        // needs a live session just to fire, let alone to reach any of our other methods
        public void EnsureReady() => _steamBootstrap.EnsureValid();

        // the overlay always hands back the real numeric Steam lobby ID, not our short code
        void OnOverlayJoinRequested(Lobby lobby, SteamId steamId) =>
            LobbyJoinRequestedByOverlay?.Invoke(lobby.Id.Value.ToString());

        string GenerateJoinCode()
        {
            var chars = new char[JoinCodeLength];
            for (int i = 0; i < chars.Length; i++) chars[i] = JoinCodeAlphabet[_rng.Next(JoinCodeAlphabet.Length)];
            return new string(chars);
        }

        public async Task<bool> CreateLobbyAsync(LobbySettings settings)
        {
            if (!_steamBootstrap.EnsureValid())
            {
                Debug.LogWarning("[SteamLobby] CreateLobbyAsync: SteamClient not valid.");
                return false;
            }
            SubscribeIfNeeded();

            var result = await SteamMatchmaking.CreateLobbyAsync(settings.MaxPlayers);
            if (!result.HasValue)
            {
                Debug.LogWarning("[SteamLobby] CreateLobbyAsync: SteamMatchmaking.CreateLobbyAsync returned no lobby.");
                return false;
            }

            _current = result.Value;
            CurrentJoinCode = GenerateJoinCode();
            _current.Value.SetData("name", settings.Name);
            _current.Value.SetData("joincode", CurrentJoinCode);
            _current.Value.SetJoinable(true);
            // SteamMatchmaking.LobbyList (how join-by-code looks a lobby up) only ever returns
            // Public lobbies - FriendsOnly/Private ones are invisible to it regardless of key
            // filters, which is why typing a code always failed with "no lobby found" before.
            // Direct invites (InviteFriend/overlay) still work the same regardless of this.
            _current.Value.SetPublic();
            return true;
        }

        // Accepts either the real numeric Steam lobby ID (overlay "Join Game" path - that big
        // number is unmistakably >100 billion) or the short human-typed join code (looked up via
        // the "joincode" lobby data set in CreateLobbyAsync).
        public async Task<bool> JoinLobbyAsync(string input)
        {
            if (!_steamBootstrap.EnsureValid())
            {
                Debug.LogWarning("[SteamLobby] JoinLobbyAsync: SteamClient not valid.");
                return false;
            }
            SubscribeIfNeeded();

            input = input?.Trim();
            if (string.IsNullOrEmpty(input))
            {
                Debug.LogWarning("[SteamLobby] JoinLobbyAsync: empty input.");
                return false;
            }

            Lobby target;
            if (ulong.TryParse(input, out var rawId) && rawId > 100_000_000_000UL)
            {
                target = new Lobby(rawId);
            }
            else
            {
                var code = input.ToUpperInvariant();
                var matches = await SteamMatchmaking.LobbyList.WithKeyValue("joincode", code).RequestAsync();
                if (matches == null || matches.Length == 0)
                {
                    Debug.LogWarning($"[SteamLobby] JoinLobbyAsync: no lobby found for code '{code}'.");
                    return false;
                }
                target = matches[0];
            }

            var result = await target.Join();
            if (result == RoomEnter.Success)
            {
                _current = target;
                return true;
            }
            Debug.LogWarning($"[SteamLobby] JoinLobbyAsync: join failed with {result}.");
            return false;
        }

        public void LeaveLobby()
        {
            _current?.Leave();
            _current = null;
            CurrentJoinCode = null;
        }

        public void SetJoinable(bool joinable) => _current?.SetJoinable(joinable);

        public void SetPlayerCount(int current, int max) =>
            _current?.SetData("players", $"{current}/{max}");

        public void OpenInviteOverlay()
        {
            _steamBootstrap.EnsureValid();
            if (_current.HasValue) SteamFriends.OpenGameInviteOverlay(_current.Value.Id);
            else Debug.LogWarning("[SteamLobby] OpenInviteOverlay: no current lobby to invite to.");
        }

        public IReadOnlyList<FriendInfo> GetFriends()
        {
            if (!_steamBootstrap.EnsureValid()) return Array.Empty<FriendInfo>();
            return SteamFriends.GetFriends()
                .Select(f => new FriendInfo(f.Id.Value.ToString(), f.Name, f.IsOnline))
                .OrderByDescending(f => f.IsOnline)
                .ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public void InviteFriend(string friendSteamId)
        {
            _steamBootstrap.EnsureValid();
            if (!_current.HasValue)
            {
                Debug.LogWarning("[SteamLobby] InviteFriend: no current lobby to invite to.");
                return;
            }
            if (!ulong.TryParse(friendSteamId, out var id))
            {
                Debug.LogWarning($"[SteamLobby] InviteFriend: '{friendSteamId}' is not a valid SteamID.");
                return;
            }
            _current.Value.InviteFriend(id);
        }

        // closes the lobby for everyone when the host leaves
        public void CloseLobbyOnHostLeave()
        {
            if (!_current.HasValue) return;
            _current.Value.Leave();
            _current = null;
            CurrentJoinCode = null;
        }

        public void Dispose()
        {
            _steamBootstrap.Reinitialized -= OnSteamReinitialized;
            if (_subscribed) SteamFriends.OnGameLobbyJoinRequested -= OnOverlayJoinRequested;
        }
    }
}
#endif
