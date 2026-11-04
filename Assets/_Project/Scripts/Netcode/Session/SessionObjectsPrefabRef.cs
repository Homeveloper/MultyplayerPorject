using UnityEngine;

namespace Game.Netcode
{
    // DI wrapper around the SessionObjects prefab the host spawns once per session
    public class SessionObjectsPrefabRef
    {
        public readonly GameObject Prefab;
        public SessionObjectsPrefabRef(GameObject prefab) => Prefab = prefab;
    }
}
