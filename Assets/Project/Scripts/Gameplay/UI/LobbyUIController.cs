using Game.Core;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.UI
{
    public class LobbyUIController : MonoBehaviour
    {
        [SerializeField] Button _startButton;
        [SerializeField] Button _inviteButton;
        [SerializeField] TMP_Text _lobbyIdText; // fallback join path - friend pastes this into the address field

        ILobbyService _lobby;
        IPresenceService _presence;
        ISessionController _subscribedSession;

        void Start()
        {
            _lobby = GameServices.Current.Resolve<ILobbyService>();
            _presence = GameServices.Current.Resolve<IPresenceService>();

            bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
            if (_startButton != null)
            {
                _startButton.gameObject.SetActive(isHost);
                _startButton.onClick.AddListener(() => ActiveSession.Current?.RequestStart());
            }

            // FriendInviteListController owns the click handler on this same button (opens the
            // in-game friends list) - the native Steam overlay (_lobby.OpenInviteOverlay) doesn't
            // reliably hook into every process, so this button no longer calls it directly.
            if (_inviteButton != null)
                _inviteButton.gameObject.SetActive(isHost && _lobby.IsAvailable);

            if (_lobbyIdText != null)
            {
                bool showId = isHost && _lobby.IsAvailable && !string.IsNullOrEmpty(_lobby.CurrentJoinCode);
                _lobbyIdText.gameObject.SetActive(showId);
                if (showId) _lobbyIdText.text = $"Code: {_lobby.CurrentJoinCode}";
            }

            _presence.SetInLobby();
            ActiveSession.Changed += OnSessionChanged;
            OnSessionChanged();
        }

        void OnDestroy()
        {
            ActiveSession.Changed -= OnSessionChanged;
            if (_subscribedSession != null) _subscribedSession.Changed -= UpdateLobbyPlayerCount;
        }

        void OnSessionChanged()
        {
            if (_subscribedSession != null) _subscribedSession.Changed -= UpdateLobbyPlayerCount;
            _subscribedSession = ActiveSession.Current;
            if (_subscribedSession != null) _subscribedSession.Changed += UpdateLobbyPlayerCount;
            UpdateLobbyPlayerCount();
        }

        void UpdateLobbyPlayerCount()
        {
            if (_lobby == null || _subscribedSession == null) return;
            var config = GameServices.Current.Resolve<AppConfig>();
            _lobby.SetPlayerCount(_subscribedSession.Players.Count, config.MaxPlayers);
        }
    }
}
