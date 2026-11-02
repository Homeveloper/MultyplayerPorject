using System;
using System.Collections.Generic;
using Game.Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Game.Netcode
{
    /// <summary>
    /// Server-authoritative player roster, replicated to everyone (including late
    /// joiners automatically, since NetworkList state syncs on spawn). Also owns the
    /// "Start" trigger the host uses to leave the lobby and load the game scene.
    /// </summary>
    public class LobbySessionController : NetworkBehaviour, IPlayerRegistry
    {
        public static LobbySessionController Instance { get; private set; }

        readonly NetworkList<NetworkPlayerInfo> _players = new();

        public event Action Changed;
        public event Action StartRequestedByHost;

        public IReadOnlyList<PlayerInfo> Players
        {
            get
            {
                var list = new List<PlayerInfo>(_players.Count);
                foreach (var p in _players)
                {
                    list.Add(new PlayerInfo
                    {
                        ClientId = p.ClientId,
                        Id = new PlayerId(p.PlayerId.ToString()),
                        Name = p.Name.ToString(),
                        Color = p.Color,
                        IsHost = p.IsHost
                    });
                }
                return list;
            }
        }

        public bool TryGet(ulong clientId, out PlayerInfo info)
        {
            foreach (var p in _players)
            {
                if (p.ClientId != clientId) continue;
                info = new PlayerInfo { ClientId = p.ClientId, Id = new PlayerId(p.PlayerId.ToString()), Name = p.Name.ToString(), Color = p.Color, IsHost = p.IsHost };
                return true;
            }
            info = default;
            return false;
        }

        public override void OnNetworkSpawn()
        {
            Instance = this;
            _players.OnListChanged += _ => Changed?.Invoke();

            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
                AddOrUpdate(NetworkManager.ServerClientId);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == this) Instance = null;
            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        void OnClientConnected(ulong clientId) => AddOrUpdate(clientId);

        void OnClientDisconnected(ulong clientId)
        {
            for (int i = 0; i < _players.Count; i++)
            {
                if (_players[i].ClientId != clientId) continue;
                _players.RemoveAt(i);
                break;
            }
        }

        void AddOrUpdate(ulong clientId)
        {
            string playerId = clientId.ToString();
            string name = $"Player {clientId}";

            var approval = GameServices.Current.Resolve<ApprovalHandler>();
            if (approval != null && approval.TryGetApprovedPayload(clientId, out var payload) && payload != null)
            {
                if (!string.IsNullOrEmpty(payload.PlayerId)) playerId = payload.PlayerId;
                if (!string.IsNullOrEmpty(payload.PlayerName)) name = payload.PlayerName;
            }

            var entry = new NetworkPlayerInfo
            {
                ClientId = clientId,
                PlayerId = new FixedString64Bytes(playerId),
                Name = new FixedString64Bytes(name),
                Color = Color.white,
                IsHost = clientId == NetworkManager.ServerClientId
            };

            for (int i = 0; i < _players.Count; i++)
            {
                if (_players[i].ClientId != clientId) continue;
                _players[i] = entry;
                return;
            }
            _players.Add(entry);
        }

        public void SetColor(ulong clientId, Color color)
        {
            if (!IsServer) return;
            for (int i = 0; i < _players.Count; i++)
            {
                if (_players[i].ClientId != clientId) continue;
                var entry = _players[i];
                entry.Color = color;
                _players[i] = entry;
                return;
            }
        }

        /// <summary>Only ever called on the host (the Start button is hidden for clients), so a
        /// plain authority check is enough - no RPC hop needed since host == server here.</summary>
        public void HostRequestsStart()
        {
            if (!IsServer) return;
            StartRequestedByHost?.Invoke();
        }
    }
}
