using System;
using Unity.Netcode;

namespace Game.Netcode
{
    // thin wrapper over NGO's scene sync, exposed as plain events for the loading UI
    public class NetworkSceneLoader
    {
        readonly NetworkManager _networkManager;

        public event Action<ulong, bool> ClientLoadStatusChanged; // clientId, isDone
        public event Action AllClientsLoaded;

        public NetworkSceneLoader(NetworkManager networkManager)
        {
            _networkManager = networkManager;
        }

        public void Install()
        {
            _networkManager.SceneManager.OnLoadComplete += OnLoadComplete;
            _networkManager.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
        }

        public void Uninstall()
        {
            if (_networkManager.SceneManager == null) return;
            _networkManager.SceneManager.OnLoadComplete -= OnLoadComplete;
            _networkManager.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
        }

        public void LoadSceneForAll(string sceneName)
        {
            if (!_networkManager.IsServer) return;
            _networkManager.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }

        void OnLoadComplete(ulong clientId, string sceneName, UnityEngine.SceneManagement.LoadSceneMode mode) =>
            ClientLoadStatusChanged?.Invoke(clientId, true);

        void OnLoadEventCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode mode,
            System.Collections.Generic.List<ulong> clientsCompleted, System.Collections.Generic.List<ulong> clientsTimedOut) =>
            AllClientsLoaded?.Invoke();
    }
}
