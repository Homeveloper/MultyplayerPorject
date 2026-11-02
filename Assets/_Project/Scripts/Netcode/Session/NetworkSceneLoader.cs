using System;
using Unity.Netcode;

namespace Game.Netcode
{
    /// <summary>
    /// Thin wrapper over NGO's built-in scene synchronization. NGO already tracks each
    /// client's load status, drops disconnected clients from the pending set so the
    /// others never hang, and auto-syncs late joiners to the active scene and all
    /// spawned NetworkObjects - this class just exposes that as simple events for the
    /// loading-screen UI.
    /// </summary>
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

        /// <summary>Server-only: starts the synchronized scene load for every connected client.</summary>
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
