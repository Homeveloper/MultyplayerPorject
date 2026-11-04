using System;
using System.Collections.Generic;
using Game.Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Game.Netcode
{
    // server-authoritative player roster, syncs to everyone including late joiners via NetworkList.
    // also owns the Start trigger that moves everyone into GameLevel.
    public class LobbySessionController : NetworkBehaviour, ISessionController
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
            ActiveSession.Current = this;
            _players.OnListChanged += _ => Changed?.Invoke();

            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
                AddOrUpdate(NetworkManager.ServerClientId);
                StartRequestedByHost += () => GameServices.Current.Resolve<NetworkSceneLoader>().LoadSceneForAll("GameLevel");
            }
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == this) Instance = null;
            if (ActiveSession.Current == (ISessionController)this) ActiveSession.Current = null;
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
                PlayerId = ToFixedString64Safe(playerId),
                Name = ToFixedString64Safe(name),
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

        // FixedString64Bytes throws on overflow, and Steam display names can have emoji/CJK that
        // blow past ~61 bytes - caught this crashing the whole roster for one long name, so truncate.
        static FixedString64Bytes ToFixedString64Safe(string value)
        {
            if (string.IsNullOrEmpty(value)) return default;
            const int maxBytes = 60; // a few bytes of slack under the ~61-byte capacity
            while (System.Text.Encoding.UTF8.GetByteCount(value) > maxBytes && value.Length > 0)
                value = value[..^1];
            return new FixedString64Bytes(value);
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

        // only ever called on the host, Start is hidden for clients - plain check, no RPC hop needed
        public void RequestStart()
        {
            if (!IsServer) return;
            StartRequestedByHost?.Invoke();
        }

        // tells the target client why *before* the host disconnects them - see KickClient() in
        // NetcodeConnectionService for why this can't just rely on NGO's own disconnect reason
        public void NotifyKicked(ulong clientId)
        {
            if (!IsServer) return;
            var target = new RpcParams { Send = new RpcSendParams { Target = RpcTarget.Single(clientId, RpcTargetUse.Temp) } };
            NotifyKickedRpc(target);
        }

        [Rpc(SendTo.SpecifiedInParams)]
        void NotifyKickedRpc(RpcParams rpcParams = default)
        {
            GameServices.Current.Resolve<IConnectionService>().NoteLocalKickReceived();
        }
    }
}
