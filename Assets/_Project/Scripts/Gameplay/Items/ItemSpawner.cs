using Unity.Netcode;
using UnityEngine;

namespace Game.Gameplay.Items
{
    /// <summary>Server-side spawning for player-created items. Spawned items are owned by
    /// the server (not the creator), so they persist after the creator disconnects. A plain
    /// MonoBehaviour (not NetworkBehaviour) - it isn't itself a replicated object, it just
    /// performs spawn logic, so it needs no NetworkObject on its own GameObject.</summary>
    public class ItemSpawner : MonoBehaviour
    {
        public static ItemSpawner Instance { get; private set; }

        [SerializeField] GameObject _itemPrefab;
        [SerializeField] float _spawnDistance = 1.5f;
        [SerializeField] float _spawnHeight = 1f;

        void Awake() => Instance = this;

        /// <summary>Server-only.</summary>
        public void ServerSpawnItemInFrontOf(Transform origin)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
            var pos = origin.position + origin.forward * _spawnDistance + Vector3.up * _spawnHeight;
            var go = Instantiate(_itemPrefab, pos, Quaternion.identity);
            go.GetComponent<NetworkObject>().Spawn(); // no ownerClientId arg => owned by the server
        }
    }
}
