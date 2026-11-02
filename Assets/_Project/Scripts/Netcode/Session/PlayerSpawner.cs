using Unity.Netcode;

namespace Game.Netcode
{
    /// <summary>
    /// Spawns each client's 3D character only once the session reaches GameLevel - not at
    /// connection/approval time, while the player is still in the Lobby UI. Also covers
    /// late joiners who connect after GameLevel is already active.
    /// </summary>
    public class PlayerSpawner
    {
        const string GameLevelSceneName = "GameLevel";

        readonly NetworkManager _networkManager;

        public PlayerSpawner(NetworkManager networkManager)
        {
            _networkManager = networkManager;
        }

        public void Install()
        {
            _networkManager.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
            _networkManager.OnClientConnectedCallback += OnClientConnected;
        }

        public void Uninstall()
        {
            if (_networkManager.SceneManager != null)
                _networkManager.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
            _networkManager.OnClientConnectedCallback -= OnClientConnected;
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
            // Covers a late joiner connecting while GameLevel is already the active scene -
            // clients who load together with the host's scene switch are handled above instead.
            if (!_networkManager.IsServer) return;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == GameLevelSceneName)
                SpawnIfNeeded(clientId);
        }

        void SpawnIfNeeded(ulong clientId)
        {
            if (_networkManager.ConnectedClients.TryGetValue(clientId, out var client) && client.PlayerObject != null)
                return;

            var prefab = _networkManager.NetworkConfig.PlayerPrefab;
            if (prefab == null) return;

            var go = UnityEngine.Object.Instantiate(prefab);
            go.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
        }
    }
}
