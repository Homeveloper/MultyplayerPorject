using Unity.Netcode;
using UnityEngine;

namespace Game.Gameplay.Items
{
    // server-owned spawning for player-created items, so they survive the creator disconnecting
    public class ItemSpawner : MonoBehaviour
    {
        public static ItemSpawner Instance { get; private set; }

        [SerializeField] GameObject _itemPrefab;
        [SerializeField] float _spawnDistance = 1.5f;
        [SerializeField] float _spawnHeight = 1f;

        void Awake() => Instance = this;

        public void ServerSpawnItemInFrontOf(Transform origin)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
            var pos = origin.position + origin.forward * _spawnDistance + Vector3.up * _spawnHeight;
            var go = Instantiate(_itemPrefab, pos, Quaternion.identity);
            go.GetComponent<NetworkObject>().Spawn(); // no ownerClientId arg => owned by the server
        }
    }
}
