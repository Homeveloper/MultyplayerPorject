using Game.Core;
using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Game.Transport.Unity
{
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

        // UnityTransport's own DebugSimulator is a no-op in this UTP version, so this goes through
        // the Multiplayer Tools package's NetworkSimulator component instead - editor/dev only.
        public void ApplySimulatorPreset(int packetDelayMs, int packetJitterMs, int packetLossPercent)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _ = Transport; // ensures UnityTransport exists before the simulator wraps it
            var sim = _networkManager.gameObject.GetComponent<NetworkSimulator>();
            if (sim == null) sim = _networkManager.gameObject.AddComponent<NetworkSimulator>();
            sim.ConnectionPreset = NetworkSimulatorPreset.Create(
                "ReviewBrief", "~150ms latency + packet loss, per the assignment brief",
                packetDelayMs, packetJitterMs, packetLossInterval: 100, packetLossPercent);
#endif
        }
    }
}
