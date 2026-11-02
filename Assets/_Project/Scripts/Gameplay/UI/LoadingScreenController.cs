using Unity.Netcode;
using UnityEngine;

namespace Game.Gameplay.UI
{
    /// <summary>
    /// Shows/hides the loading overlay. Stays visible per-client until NGO reports every
    /// (still connected) client finished loading the new scene - see NetworkSceneLoader,
    /// which forwards NGO's own OnLoadEventCompleted (NGO already drops disconnected
    /// clients from that wait, so nobody hangs on the overlay if someone drops mid-load).
    /// </summary>
    public class LoadingScreenController : MonoBehaviour
    {
        [SerializeField] GameObject _overlayRoot;

        void OnEnable()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnAllLoaded;
        }

        void OnDisable()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnAllLoaded;
        }

        void OnAllLoaded(string sceneName, UnityEngine.SceneManagement.LoadSceneMode mode,
            System.Collections.Generic.List<ulong> completed, System.Collections.Generic.List<ulong> timedOut)
        {
            if (_overlayRoot != null) _overlayRoot.SetActive(false);
        }
    }
}
