using Game.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Gameplay.Session
{
    /// <summary>
    /// Persistent (DontDestroyOnLoad) watcher that reacts to connection loss:
    /// host left / we got rejected / we got kicked -> drop back to MainMenu with
    /// a human-readable reason. Host migration is explicitly out of scope per the brief.
    /// </summary>
    public class SessionFlowController : MonoBehaviour
    {
        public const string MainMenuScene = "MainMenu";

        IConnectionService _connection;
        public static string LastDisconnectReason { get; private set; }

        public void Construct(IConnectionService connection)
        {
            _connection = connection;
            _connection.LocalDisconnected += OnLocalDisconnected;
        }

        void OnDestroy()
        {
            if (_connection != null) _connection.LocalDisconnected -= OnLocalDisconnected;
        }

        void OnLocalDisconnected(string reason)
        {
            LastDisconnectReason = DisconnectReason.ToDisplayText(reason);
            if (SceneManager.GetActiveScene().name != MainMenuScene)
                SceneManager.LoadScene(MainMenuScene, LoadSceneMode.Single);
        }
    }
}
