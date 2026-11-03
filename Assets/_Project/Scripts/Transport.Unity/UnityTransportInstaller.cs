using Game.Core;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Game.Transport.Unity
{
    /// <summary>Configures NGO's UnityTransport for plain IP:port connections.</summary>
    public class UnityTransportInstaller : ITransportConfigurator
    {
        readonly NetworkManager _networkManager;

        public TransportType Type => TransportType.UnityTransport;

        public UnityTransportInstaller(NetworkManager networkManager)
        {
            _networkManager = networkManager;
        }

        UnityTransport Transport
        {
            get
            {
                var t = _networkManager.NetworkConfig.NetworkTransport as UnityTransport;
                if (t == null)
                {
                    t = _networkManager.gameObject.GetComponent<UnityTransport>();
                    if (t == null) t = _networkManager.gameObject.AddComponent<UnityTransport>();
                    _networkManager.NetworkConfig.NetworkTransport = t;
                }
                return t;
            }
        }

        public void ConfigureForHost(AppConfig config)
        {
            Transport.SetConnectionData("0.0.0.0", config.ResolvedPort);
            ApplySimulatorFromConfig(config);
        }

        public void ConfigureForClient(AppConfig config, string joinTarget)
        {
            var address = string.IsNullOrEmpty(joinTarget) ? config.ResolvedJoinAddress : joinTarget;
            Transport.SetConnectionData(address, config.ResolvedPort);
            ApplySimulatorFromConfig(config);
        }

        void ApplySimulatorFromConfig(AppConfig config)
        {
            if (config.SimulateNetworkConditions)
                ApplySimulatorPreset(config.SimulatedDelayMs, config.SimulatedJitterMs, config.SimulatedPacketLossPercent);
        }

        /// <summary>Unity Transport's built-in Network Simulator - used to rehearse the ~150ms/packet-loss
        /// conditions the brief says the reviewers will test under. Editor/dev builds only.</summary>
        public void ApplySimulatorPreset(int packetDelayMs, int packetJitterMs, int packetLossPercent)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var t = Transport;
            t.SetDebugSimulatorParameters(packetDelayMs, packetJitterMs, packetLossPercent);
#endif
        }
    }
}
