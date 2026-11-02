using System.Collections.Generic;
using Game.Core;
using Unity.Netcode;
using UnityEngine;

namespace Game.Netcode
{
    /// <summary>
    /// Host-side connection approval: player limit, build version match, duplicate
    /// PlayerId rejection. Installed on the host's NetworkManager by CompositionRoot.
    /// </summary>
    public class ApprovalHandler
    {
        readonly NetworkManager _networkManager;
        readonly AppConfig _config;
        readonly HashSet<string> _connectedPlayerIds = new();
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

            if (!string.IsNullOrEmpty(payload.PlayerId))
                _connectedPlayerIds.Add(payload.PlayerId);
            _approvedPayloads[request.ClientNetworkId] = payload;

            response.Approved = true;
            response.CreatePlayerObject = true;
            response.Pending = false;
        }

        public bool TryGetApprovedPayload(ulong clientId, out ConnectionPayload payload) =>
            _approvedPayloads.TryGetValue(clientId, out payload);

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
