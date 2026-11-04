using UnityEngine;

namespace Game.Core
{
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
