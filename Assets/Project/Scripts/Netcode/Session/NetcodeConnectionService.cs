using System;
using System.Threading.Tasks;
using Game.Core;
using Unity.Netcode;
using UnityEngine;

namespace Game.Netcode
{
    // NGO implementation of IConnectionService - wraps NetworkManager so Gameplay/UI never touch it directly
    public class NetcodeConnectionService : IConnectionService, IDisposable
    {
        readonly NetworkManager _networkManager;
        readonly ITransportConfigurator _transport;
        readonly IPlayerIdentityProvider _identity;
        readonly AppConfig _config;
        readonly ApprovalHandler _approval;
        readonly SessionObjectsPrefabRef _sessionObjects;
        readonly ILobbyService _lobby;
        bool _localKickPending;

        public bool IsHost => _networkManager.IsHost;
        public bool IsClient => _networkManager.IsClient;
        public bool IsConnected => _networkManager.IsListening;

        public event Action<ulong> ClientConnected;
        public event Action<ulong, string> ClientDisconnected;
        public event Action<string> LocalDisconnected;

        public NetcodeConnectionService(
            NetworkManager networkManager,
            ITransportConfigurator transport,
            IPlayerIdentityProvider identity,
            AppConfig config,
            ApprovalHandler approval,
            SessionObjectsPrefabRef sessionObjects,
            ILobbyService lobby)
        {
            _networkManager = networkManager;
            _transport = transport;
            _identity = identity;
            _config = config;
            _approval = approval;
            _sessionObjects = sessionObjects;
            _lobby = lobby;

            _networkManager.OnClientConnectedCallback += OnClientConnected;
            _networkManager.OnClientDisconnectCallback += OnClientDisconnect;
            Application.quitting += OnApplicationQuitting; // best-effort: closes the Steam lobby on a graceful Alt+F4/quit
        }

        public void StartHost()
        {
            try
            {
                _networkManager.NetworkConfig.ConnectionApproval = true;
                _approval.Install();
                _transport.ConfigureForHost(_config);
                SetLocalPayload();
                _networkManager.StartHost();

                if (_sessionObjects?.Prefab != null)
                {
                    var go = UnityEngine.Object.Instantiate(_sessionObjects.Prefab);
                    var netObj = go.GetComponent<NetworkObject>();
                    netObj.DestroyWithScene = false; // must survive the MainMenu -> Lobby -> GameLevel scene loads
                    netObj.Spawn();
                }
            }
            catch (Exception e)
            {
                // transport setup / Steamworks / NGO internals can all throw in ways we can't
                // fully enumerate - turn it into a normal failed-connection message, not a crash
                Debug.LogError($"[Connection] StartHost failed: {e}");
                if (_networkManager.IsListening) _networkManager.Shutdown();
                LocalDisconnected?.Invoke(DisconnectReason.LocalError);
            }
        }

        public void StartClient(ConnectRequest request)
        {
            try
            {
                // NGO leaves a failed client "listening" and refuses a second StartClient() otherwise
                if (_networkManager.IsListening) _networkManager.Shutdown();

                // must match the host's flag or it reads our request as malformed and silently drops it
                _networkManager.NetworkConfig.ConnectionApproval = true;

                _transport.ConfigureForClient(_config, request.Address);
                SetLocalPayload();
                _networkManager.StartClient();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Connection] StartClient failed: {e}");
                if (_networkManager.IsListening) _networkManager.Shutdown();
                LocalDisconnected?.Invoke(DisconnectReason.LocalError);
            }
        }

        public void HostLoadScene(string sceneName)
        {
            if (!_networkManager.IsServer) return;
            _networkManager.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }

        public async void KickClient(ulong clientId)
        {
            if (!_networkManager.IsHost) return;
            _approval.MarkKicked(clientId);

            // tell them why first and give the Steam P2P relay real time to deliver it - the
            // Facepunch transport can close the connection before NGO's own built-in
            // DisconnectReasonMessage arrives, which is what caused "the host has left the
            // session" to show up on a kick instead of the correct reason
            LobbySessionController.Instance?.NotifyKicked(clientId);
            await Task.Delay(300);

            _networkManager.DisconnectClient(clientId, DisconnectReason.Kicked);
        }

        public void NoteLocalKickReceived() => _localKickPending = true;

        public void Shutdown()
        {
            if (_networkManager.IsServer)
            {
                _approval.Uninstall();
                _lobby.CloseLobbyOnHostLeave();
            }
            _networkManager.Shutdown();
        }

        void OnApplicationQuitting()
        {
            if (_networkManager.IsServer) _lobby.CloseLobbyOnHostLeave();
        }

        void SetLocalPayload()
        {
            var payload = new ConnectionPayload
            {
                BuildVersion = _config.BuildVersion,
                PlayerId = _identity.GetLocalPlayerId().Value,
                PlayerName = _identity.GetLocalPlayerName()
            };
            _networkManager.NetworkConfig.ConnectionData = payload.ToBytes();
        }

        void OnClientConnected(ulong clientId)
        {
            if (clientId == _networkManager.LocalClientId && !_networkManager.IsHost)
                return; // local "connected" event for clients is handled via scene flow, not here
            ClientConnected?.Invoke(clientId);
        }

        void OnClientDisconnect(ulong clientId)
        {
            if (clientId == _networkManager.LocalClientId && !_networkManager.IsHost)
            {
                // prefer the kick notice from NotifyKickedRpc - it beat the race against
                // NetworkManager.DisconnectReason on the Steam transport, see KickClient() above
                var reason = _localKickPending ? DisconnectReason.Kicked : _networkManager.DisconnectReason;
                LocalDisconnected?.Invoke(string.IsNullOrEmpty(reason) ? DisconnectReason.HostLeft : reason);
                return;
            }

            ClientDisconnected?.Invoke(clientId, _networkManager.DisconnectReason);
        }

        public void Dispose()
        {
            Application.quitting -= OnApplicationQuitting;
            if (_networkManager == null) return;
            _networkManager.OnClientConnectedCallback -= OnClientConnected;
            _networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }
}
