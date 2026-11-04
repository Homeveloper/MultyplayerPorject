using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Items
{
    public class ItemSessionCleanup : MonoBehaviour
    {
        IConnectionService _connection;

        public void Construct(IConnectionService connection)
        {
            _connection = connection;
            _connection.ClientDisconnected += OnClientDisconnected;
        }

        void OnDestroy()
        {
            if (_connection != null) _connection.ClientDisconnected -= OnClientDisconnected;
        }

        void OnClientDisconnected(ulong clientId, string reason)
        {
            foreach (var item in FindObjectsByType<PhysicsItem>(FindObjectsSortMode.None))
                item.ServerForceReleaseIfHeldBy(clientId);
        }
    }
}
