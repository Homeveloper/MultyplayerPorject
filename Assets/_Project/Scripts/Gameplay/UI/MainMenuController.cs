using Game.Core;
using Game.Gameplay.Session;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.UI
{
    /// <summary>Host/Join screen. Pulls its dependencies from <see cref="GameServices"/> in
    /// Awake - this scene is loaded after Bootstrap, which is where CompositionRoot sets up
    /// the resolver (see Core.GameServices for why UI/NGO objects use this instead of
    /// constructor injection).</summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] InputField _addressField;
        [SerializeField] Button _hostButton;
        [SerializeField] Button _joinButton;
        [SerializeField] Text _statusText;

        IConnectionService _connection;
        AppConfig _config;

        void Awake()
        {
            _connection = GameServices.Current.Resolve<IConnectionService>();
            _config = GameServices.Current.Resolve<AppConfig>();
        }

        void Start()
        {
            if (_addressField != null) _addressField.text = _config.ResolvedJoinAddress;
            if (!string.IsNullOrEmpty(SessionFlowController.LastDisconnectReason) && _statusText != null)
                _statusText.text = SessionFlowController.LastDisconnectReason;

            _hostButton?.onClick.AddListener(OnHostClicked);
            _joinButton?.onClick.AddListener(OnJoinClicked);
        }

        void OnHostClicked()
        {
            _connection.StartHost();
            // Synchronized (NGO-managed) scene load - a plain SceneManager.LoadScene here would
            // destroy the NetworkObjects just spawned by StartHost (session roster, ping tracker).
            _connection.HostLoadScene("Lobby");
        }

        void OnJoinClicked()
        {
            var address = _addressField != null ? _addressField.text : _config.ResolvedJoinAddress;
            _connection.StartClient(new ConnectRequest(address, _config.ResolvedPort));
            // Clients do not change scene themselves here - NGO's scene sync will move them
            // into whatever scene the host is in (Lobby) once the connection completes.
        }
    }
}
