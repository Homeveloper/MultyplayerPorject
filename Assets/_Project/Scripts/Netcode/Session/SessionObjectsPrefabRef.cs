using UnityEngine;

namespace Game.Netcode
{
    /// <summary>DI-friendly wrapper around the "SessionObjects" prefab (LobbySessionController +
    /// PingTracker) that the host spawns once per session - a plain GameObject reference isn't
    /// distinctive enough to register directly without risking collisions with other registrations.</summary>
    public class SessionObjectsPrefabRef
    {
        public readonly GameObject Prefab;
        public SessionObjectsPrefabRef(GameObject prefab) => Prefab = prefab;
    }
}
