using Game.Core;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.UI
{
    /// <summary>Lobby screen chrome: Start button (host only).</summary>
    public class LobbyUIController : MonoBehaviour
    {
        [SerializeField] Button _startButton;

        void Start()
        {
            bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
            if (_startButton != null)
            {
                _startButton.gameObject.SetActive(isHost);
                _startButton.onClick.AddListener(() => ActiveSession.Current?.RequestStart());
            }
        }
    }
}
