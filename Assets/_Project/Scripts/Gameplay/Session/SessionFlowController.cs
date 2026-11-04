using Game.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Gameplay.Session
{
    // persistent watcher - host left / rejected / kicked -> back to MainMenu with a reason
    public class SessionFlowController : MonoBehaviour
    {
        public const string MainMenuScene = "MainMenu";

        IConnectionService _connection;
        IPresenceService _presence;
        public static string LastDisconnectReason { get; private set; }

        public void Construct(IConnectionService connection, IPresenceService presence)
        {
            _connection = connection;
            _presence = presence;
            _connection.LocalDisconnected += OnLocalDisconnected;
        }

        void OnDestroy()
        {
            if (_connection != null) _connection.LocalDisconnected -= OnLocalDisconnected;
        }

        void OnLocalDisconnected(string reason)
        {
            LastDisconnectReason = DisconnectReason.ToDisplayText(reason);
            _presence?.Clear();
            if (SceneManager.GetActiveScene().name != MainMenuScene)
                SceneManager.LoadScene(MainMenuScene, LoadSceneMode.Single);
        }
    }
}
