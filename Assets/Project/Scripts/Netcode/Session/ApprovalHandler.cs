using System.Collections.Generic;
using Game.Core;
using Unity.Netcode;
using UnityEngine;

namespace Game.Netcode
{
    // host-side approval: player limit, version match, duplicate PlayerId rejection
    public class ApprovalHandler
    {
        readonly NetworkManager _networkManager;
        readonly AppConfig _config;
        readonly HashSet<string> _connectedPlayerIds = new();
        readonly HashSet<string> _kickedPlayerIds = new();
        readonly Dictionary<ulong, ConnectionPayload> _approvedPayloads = new();

        public ApprovalHandler(NetworkManager networkManager, AppConfig config)
        {
            _networkManager = networkManager;
            _config = config;
        }

        public void Install()
        {
            _networkManager.ConnectionApprovalCallback = Approve;
            _networkManager.OnClientDisconnectCallback += OnDisconnect;
        }

        public void Uninstall()
        {
            _networkManager.ConnectionApprovalCallback = null;
            _networkManager.OnClientDisconnectCallback -= OnDisconnect;
            _connectedPlayerIds.Clear();
            _kickedPlayerIds.Clear();
        }

        void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            var payload = ConnectionPayload.FromBytes(request.Payload);

            if (payload == null)
            {
                Reject(response, DisconnectReason.VersionMismatch);
                return;
            }

            if (payload.BuildVersion != _config.BuildVersion)
            {
                Reject(response, DisconnectReason.VersionMismatch);
                return;
            }

            // +1 counts the host itself, who is always ClientId 0 and connects "for free".
            if (_networkManager.ConnectedClientsIds.Count >= _config.MaxPlayers)
            {
                Reject(response, DisconnectReason.SessionFull);
                return;
            }

            if (!string.IsNullOrEmpty(payload.PlayerId) && _connectedPlayerIds.Contains(payload.PlayerId))
            {
                Reject(response, DisconnectReason.DuplicatePlayer);
                return;
            }

            if (!string.IsNullOrEmpty(payload.PlayerId) && _kickedPlayerIds.Contains(payload.PlayerId))
            {
                Reject(response, DisconnectReason.Kicked);
                return;
            }

            if (!string.IsNullOrEmpty(payload.PlayerId))
                _connectedPlayerIds.Add(payload.PlayerId);
            _approvedPayloads[request.ClientNetworkId] = payload;

            response.Approved = true;
            // No auto-spawned player object here - this approval fires while the player is
            // still in the Lobby (menu/UI only); the 3D character is spawned by PlayerSpawner
            // once the session actually loads GameLevel.
            response.CreatePlayerObject = false;
            response.Pending = false;
        }

        public bool TryGetApprovedPayload(ulong clientId, out ConnectionPayload payload) =>
            _approvedPayloads.TryGetValue(clientId, out payload);

        // must run before NetworkManager.DisconnectClient() - that triggers OnDisconnect below,
        // which erases the payload this needs to resolve the clientId to a PlayerId
        public void MarkKicked(ulong clientId)
        {
            if (_approvedPayloads.TryGetValue(clientId, out var payload) && payload != null && !string.IsNullOrEmpty(payload.PlayerId))
                _kickedPlayerIds.Add(payload.PlayerId);
        }

        void Reject(NetworkManager.ConnectionApprovalResponse response, string reason)
        {
            response.Approved = false;
            response.Reason = reason;
            response.Pending = false;
        }

        void OnDisconnect(ulong clientId)
        {
            if (_approvedPayloads.TryGetValue(clientId, out var payload) && payload != null)
                _connectedPlayerIds.Remove(payload.PlayerId);
            _approvedPayloads.Remove(clientId);
        }
    }
}
