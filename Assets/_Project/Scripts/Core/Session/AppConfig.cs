using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Single source of truth for "which transport, which port, how many players".
    /// This is the one place that picks the transport (see plan: "transport selection
    /// lives in one place"); CLI args in Bootstrap may override the serialized values
    /// at runtime without touching this asset.
    /// </summary>
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

        // Runtime-only overrides (CLI args). Not serialized.
        [System.NonSerialized] public TransportType? TransportTypeOverride;
        [System.NonSerialized] public string JoinAddressOverride;
        [System.NonSerialized] public ushort? PortOverride;

        public TransportType ResolvedTransportType => TransportTypeOverride ?? TransportType;
        public string ResolvedJoinAddress => string.IsNullOrEmpty(JoinAddressOverride) ? DefaultJoinAddress : JoinAddressOverride;
        public ushort ResolvedPort => PortOverride ?? Port;
    }
}
