using System;
using Game.Core;
using Unity.Netcode;
using UnityEngine;

namespace Game.Netcode
{
    /// <summary>
    /// NGO implementation of <see cref="IConnectionService"/>. Wraps NetworkManager so
    /// Gameplay/UI code never touches it directly - swapping netcode libraries means
    /// swapping this one class.
    /// </summary>
    public class NetcodeConnectionService : IConnectionService, IDisposable
    {
        readonly NetworkManager _networkManager;
        readonly ITransportConfigurator _transport;
        readonly IPlayerIdentityProvider _identity;
        readonly AppConfig _config;

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
            AppConfig config)
        {
            _networkManager = networkManager;
            _transport = transport;
            _identity = identity;
            _config = config;

            _networkManager.OnClientConnectedCallback += OnClientConnected;
            _networkManager.OnClientDisconnectCallback += OnClientDisconnect;
        }

        public void StartHost()
        {
            _networkManager.NetworkConfig.ConnectionApproval = true;
            _transport.ConfigureForHost(_config);
            SetLocalPayload();
            _networkManager.StartHost();
        }

        public void StartClient(ConnectRequest request)
        {
            _transport.ConfigureForClient(_config, request.Address);
            SetLocalPayload();
            _networkManager.StartClient();
        }

        public void Shutdown()
        {
            _networkManager.Shutdown();
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
                var reason = _networkManager.DisconnectReason;
                LocalDisconnected?.Invoke(string.IsNullOrEmpty(reason) ? DisconnectReason.HostLeft : reason);
                return;
            }

            ClientDisconnected?.Invoke(clientId, _networkManager.DisconnectReason);
        }

        public void Dispose()
        {
            if (_networkManager == null) return;
            _networkManager.OnClientConnectedCallback -= OnClientConnected;
            _networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }
}
