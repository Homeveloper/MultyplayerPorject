using Unity.Netcode;
using UnityEngine;

namespace Game.Gameplay.Items
{
    /// <summary>Server-side spawning for player-created items. Spawned items are owned by
    /// the server (not the creator), so they persist after the creator disconnects.</summary>
    public class ItemSpawner : NetworkBehaviour
    {
        public static ItemSpawner Instance { get; private set; }

        [SerializeField] GameObject _itemPrefab;
        [SerializeField] float _spawnDistance = 1.5f;
        [SerializeField] float _spawnHeight = 1f;

        void Awake() => Instance = this;

        /// <summary>Server-only.</summary>
        public void ServerSpawnItemInFrontOf(Transform origin)
        {
            if (!IsServer) return;
            var pos = origin.position + origin.forward * _spawnDistance + Vector3.up * _spawnHeight;
            var go = Instantiate(_itemPrefab, pos, Quaternion.identity);
            go.GetComponent<NetworkObject>().Spawn(); // no ownerClientId arg => owned by the server
        }
    }
}
