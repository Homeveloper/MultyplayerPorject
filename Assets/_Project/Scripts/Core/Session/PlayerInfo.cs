using UnityEngine;

namespace Game.Core
{
    /// <summary>Snapshot of a player as shown in lobby / in-session player list UI.</summary>
    public struct PlayerInfo
    {
        public ulong ClientId;
        public PlayerId Id;
        public string Name;
        public Color Color;
        public bool IsHost;
        public float RttMilliseconds;
    }
}
