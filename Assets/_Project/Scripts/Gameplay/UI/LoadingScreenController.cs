using Unity.Netcode;
using UnityEngine;

namespace Game.Gameplay.UI
{
    // Hides the overlay once this client finishes loading. Two NGO events can mean "done":
    // OnLoadEventCompleted fires for a normal broadcasted scene change (Lobby -> GameLevel).
    // OnSynchronizeComplete fires instead for a late joiner / reconnect into a scene that's
    // already active - no LoadScene() call happens for them, so OnLoadEventCompleted never
    // fires and the overlay used to stay up forever even though they were already playable.
    public class LoadingScreenController : MonoBehaviour
    {
        [SerializeField] GameObject _overlayRoot;

        void OnEnable()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnAllLoaded;
                NetworkManager.Singleton.SceneManager.OnSynchronizeComplete += OnSynchronizeComplete;
            }
        }

        void OnDisable()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnAllLoaded;
                NetworkManager.Singleton.SceneManager.OnSynchronizeComplete -= OnSynchronizeComplete;
            }
        }

        void OnAllLoaded(string sceneName, UnityEngine.SceneManagement.LoadSceneMode mode,
            System.Collections.Generic.List<ulong> completed, System.Collections.Generic.List<ulong> timedOut)
        {
            HideOverlay();
        }

        void OnSynchronizeComplete(ulong clientId)
        {
            // fires for every client on the server too, so only react to our own
            if (NetworkManager.Singleton != null && clientId == NetworkManager.Singleton.LocalClientId)
                HideOverlay();
        }

        void HideOverlay()
        {
            if (_overlayRoot != null) _overlayRoot.SetActive(false);
        }
    }
}
