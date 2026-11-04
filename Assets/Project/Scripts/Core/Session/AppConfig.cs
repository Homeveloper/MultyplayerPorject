using UnityEngine;

namespace Game.Core
{
    // which transport, which port, how many players - the one place that picks all this.
    // CLI args override the serialized values at runtime without touching the asset.
    [CreateAssetMenu(fileName = "AppConfig", menuName = "Game/App Config")]
    public class AppConfig : ScriptableObject
    {
        [Header("Transport")]
        public TransportType TransportType = TransportType.UnityTransport;
        public ushort Port = 7777;
        public string DefaultJoinAddress = "127.0.0.1";

        [Header("Session")]
        public int MaxPlayers = 4;
        [Tooltip("Compared between host and client on connect; bump on incompatible releases.")]
        public string BuildVersion = "0.1.0";

        [Header("Steam")]
        public uint SteamAppId = 480;

        [Header("Network Simulator (Unity Transport only, editor/dev builds)")]
        [Tooltip("Rehearse the ~150ms latency + packet loss conditions the brief says will be tested under.")]
        public bool SimulateNetworkConditions = false;
        public int SimulatedDelayMs = 150;
        public int SimulatedJitterMs = 20;
        [Range(0, 100)] public int SimulatedPacketLossPercent = 2;

        // Runtime-only overrides (CLI args). Not serialized.
        [System.NonSerialized] public TransportType? TransportTypeOverride;
        [System.NonSerialized] public string JoinAddressOverride;
        [System.NonSerialized] public ushort? PortOverride;

        public TransportType ResolvedTransportType => TransportTypeOverride ?? TransportType;
        public string ResolvedJoinAddress => string.IsNullOrEmpty(JoinAddressOverride) ? DefaultJoinAddress : JoinAddressOverride;
        public ushort ResolvedPort => PortOverride ?? Port;
    }
}
