using Game.Core;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.UI
{
    /// <summary>Renders the roster from <see cref="ActiveSession"/> (ping + kick button for the
    /// host, both bonus features). Used in the lobby screen and the in-game player
    /// list / pause panel.</summary>
    public class PlayerListPanelController : MonoBehaviour
    {
        [SerializeField] Transform _rowContainer;
        [SerializeField] GameObject _rowPrefab; // Text "name — ping ms" + optional Kick Button

        void OnEnable()
        {
            if (ActiveSession.Current != null)
                ActiveSession.Current.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            if (ActiveSession.Current != null)
                ActiveSession.Current.Changed -= Refresh;
        }

        void Refresh()
        {
            if (_rowContainer == null || _rowPrefab == null || ActiveSession.Current == null) return;

            foreach (Transform child in _rowContainer) Destroy(child.gameObject);

            var registry = ActiveSession.Current;
            var ping = ActivePing.Current;
            bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;

            foreach (var player in registry.Players)
            {
                var row = Instantiate(_rowPrefab, _rowContainer);
                var rtt = ping?.GetRttMilliseconds(player.ClientId) ?? 0f;
                var label = row.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = $"{player.Name}{(player.IsHost ? " (Host)" : string.Empty)} - {rtt:0}ms";

                var kickButton = row.GetComponentInChildren<Button>();
                if (kickButton != null)
                {
                    kickButton.gameObject.SetActive(isHost && !player.IsHost);
                    var clientId = player.ClientId;
                    kickButton.onClick.AddListener(() => Kick(clientId));
                }
            }
        }

        void Kick(ulong clientId)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
                NetworkManager.Singleton.DisconnectClient(clientId, DisconnectReason.Kicked);
        }
    }
}
