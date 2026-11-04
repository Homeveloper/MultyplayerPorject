using Game.Core;
using Game.Gameplay.Session;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] TMP_InputField _addressField;
        [SerializeField] Button _hostButton;
        [SerializeField] Button _joinButton;
        [SerializeField] TMP_Text _statusText;

        IConnectionService _connection;
        ILobbyService _lobby;
        AppConfig _config;
        bool _steamMode;

        void Awake()
        {
            _connection = GameServices.Current.Resolve<IConnectionService>();
            _lobby = GameServices.Current.Resolve<ILobbyService>();
            _config = GameServices.Current.Resolve<AppConfig>();
            _steamMode = _config.ResolvedTransportType == TransportType.SteamP2P;
        }

        void Start()
        {
            // a previous session's disconnect can have killed the native Steam session behind
            // our back (see SteamBootstrap) - re-validate now, not lazily on the next lobby
            // action, since a dead session never fires the "friend accepted your invite" callback
            _lobby.EnsureReady();

            if (_addressField != null)
                _addressField.text = _steamMode ? string.Empty : _config.ResolvedJoinAddress;
            if (_steamMode && _addressField?.placeholder is TMP_Text placeholder)
                placeholder.text = "Code";

            if (!string.IsNullOrEmpty(SessionFlowController.LastDisconnectReason) && _statusText != null)
                _statusText.text = SessionFlowController.LastDisconnectReason;

            _hostButton?.onClick.AddListener(OnHostClicked);
            _joinButton?.onClick.AddListener(OnJoinClicked);

            // a failed Host/Join fires this on the same screen (no reload, so Start() won't rerun)
            _connection.LocalDisconnected += OnLocalDisconnected;
            _lobby.LobbyJoinRequestedByOverlay += OnOverlayJoinRequested;
        }

        void OnDestroy()
        {
            if (_connection != null) _connection.LocalDisconnected -= OnLocalDisconnected;
            if (_lobby != null) _lobby.LobbyJoinRequestedByOverlay -= OnOverlayJoinRequested;
        }

        void OnLocalDisconnected(string reason)
        {
            if (_statusText != null) _statusText.text = DisconnectReason.ToDisplayText(reason);
        }

        async void OnHostClicked()
        {
            if (_steamMode)
            {
                var created = await _lobby.CreateLobbyAsync(new LobbySettings("Co-op Session", _config.MaxPlayers, LobbyVisibility.FriendsOnly));
                if (!created && _statusText != null)
                    _statusText.text = "Could not create Steam lobby - hosting without it.";
            }

            _connection.StartHost();
            _connection.HostLoadScene("Lobby"); // plain SceneManager.LoadScene would kill the spawned roster/ping objects
        }

        async void OnJoinClicked()
        {
            if (_steamMode)
            {
                var code = _addressField != null ? _addressField.text.Trim() : string.Empty;
                await JoinSteamLobby(code);
                return;
            }

            var address = _addressField != null ? _addressField.text : _config.ResolvedJoinAddress;
            _connection.StartClient(new ConnectRequest(address, _config.ResolvedPort));
            // no scene change here - NGO's own sync moves us into the host's scene
        }

        // fired when a friend clicks "Join Game" in the Steam overlay/friends list
        async void OnOverlayJoinRequested(string lobbyId) => await JoinSteamLobby(lobbyId);

        async System.Threading.Tasks.Task JoinSteamLobby(string lobbyIdOrCode)
        {
            if (string.IsNullOrEmpty(lobbyIdOrCode))
            {
                if (_statusText != null) _statusText.text = "Enter the lobby code or join via Steam overlay.";
                return;
            }

            var joined = await _lobby.JoinLobbyAsync(lobbyIdOrCode);
            if (!joined || string.IsNullOrEmpty(_lobby.CurrentLobbyOwnerId))
            {
                if (_statusText != null) _statusText.text = "Could not join that Steam lobby.";
                return;
            }

            _connection.StartClient(new ConnectRequest(_lobby.CurrentLobbyOwnerId, _config.ResolvedPort));
        }
    }
}
