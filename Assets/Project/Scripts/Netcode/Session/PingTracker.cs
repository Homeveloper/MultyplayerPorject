using System.Collections.Generic;
using Game.Core;
using Unity.Netcode;
using UnityEngine;

namespace Game.Netcode
{
    // app-level RTT via RPC round trip - works the same on Unity Transport or Steam P2P
    public class PingTracker : NetworkBehaviour, IPingProvider
    {
        const float IntervalSeconds = 2f;

        readonly Dictionary<ulong, float> _rttByClient = new();
        float _timer;

        public override void OnNetworkSpawn()
        {
            _timer = 0f;
            ActivePing.Current = this;
        }

        public override void OnNetworkDespawn()
        {
            if (ActivePing.Current == (IPingProvider)this) ActivePing.Current = null;
        }

        void Update()
        {
            if (!IsServer) return;
            _timer += Time.deltaTime;
            if (_timer < IntervalSeconds) return;
            _timer = 0f;

            foreach (var clientId in NetworkManager.ConnectedClientsIds)
            {
                var target = new RpcParams { Send = new RpcSendParams { Target = RpcTarget.Single(clientId, RpcTargetUse.Temp) } };
                PingClientRpc(Time.realtimeSinceStartup, target);
            }
        }

        [Rpc(SendTo.SpecifiedInParams)]
        void PingClientRpc(float sentAt, RpcParams rpcParams = default)
        {
            PongServerRpc(sentAt);
        }

        [Rpc(SendTo.Server)]
        void PongServerRpc(float sentAt, RpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;
            var rttMs = Mathf.Max(0f, (Time.realtimeSinceStartup - sentAt) * 1000f);
            _rttByClient[clientId] = rttMs;
        }

        public float GetRttMilliseconds(ulong clientId) =>
            _rttByClient.TryGetValue(clientId, out var rtt) ? rtt : 0f;
    }
}
