using Game.Core;
using Unity.Netcode;
using UnityEngine;

namespace Game.Netcode
{
    // spawns each client's character once GameLevel is reached, not at approval time - also
    // handles late joiners connecting after GameLevel is already active
    public class PlayerSpawner
    {
        const string GameLevelSceneName = "GameLevel";
        const float SpawnRadius = 3f;

        readonly NetworkManager _networkManager;
        readonly AppConfig _config;
        int _spawnIndex;

        public PlayerSpawner(NetworkManager networkManager, AppConfig config)
        {
            _networkManager = networkManager;
            _config = config;
        }

        public void Install()
        {
            // SceneManager doesn't exist until networking actually starts
            _networkManager.OnServerStarted += OnServerStarted;
            _networkManager.OnClientConnectedCallback += OnClientConnected;
        }

        public void Uninstall()
        {
            _networkManager.OnServerStarted -= OnServerStarted;
            if (_networkManager.SceneManager != null)
                _networkManager.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
            _networkManager.OnClientConnectedCallback -= OnClientConnected;
        }

        void OnServerStarted()
        {
            _networkManager.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
        }

        void OnLoadEventCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode mode,
            System.Collections.Generic.List<ulong> clientsCompleted, System.Collections.Generic.List<ulong> clientsTimedOut)
        {
            if (!_networkManager.IsServer || sceneName != GameLevelSceneName) return;
            foreach (var clientId in clientsCompleted)
                SpawnIfNeeded(clientId);
        }

        void OnClientConnected(ulong clientId)
        {
            if (!_networkManager.IsServer) return; // late joiner into an already-active GameLevel
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == GameLevelSceneName)
                SpawnIfNeeded(clientId);
        }

        void SpawnIfNeeded(ulong clientId)
        {
            if (_networkManager.ConnectedClients.TryGetValue(clientId, out var client) && client.PlayerObject != null)
                return;

            var prefab = _networkManager.NetworkConfig.PlayerPrefab;
            if (prefab == null) return;

            var (position, rotation) = NextSpawnTransform();
            var go = UnityEngine.Object.Instantiate(prefab, position, rotation);
            go.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
        }

        // spreads players evenly around a circle facing the center, so 2 players spawn opposite
        // each other instead of stacked on the same spot
        (Vector3 position, Quaternion rotation) NextSpawnTransform()
        {
            int slots = Mathf.Max(_config.MaxPlayers, 1);
            float angleRad = (_spawnIndex % slots) * (360f / slots) * Mathf.Deg2Rad;
            _spawnIndex++;

            var position = new Vector3(Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad)) * SpawnRadius;
            var rotation = Quaternion.LookRotation(-position.normalized, Vector3.up);
            return (position, rotation);
        }
    }
}
