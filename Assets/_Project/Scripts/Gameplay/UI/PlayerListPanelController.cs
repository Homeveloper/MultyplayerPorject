using Game.Core;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.UI
{
    // renders the roster from ActiveSession - ping + kick (host only). Used in both the lobby
    // screen and the in-game Tab panel.
    public class PlayerListPanelController : MonoBehaviour
    {
        [SerializeField] Transform _rowContainer;
        [SerializeField] GameObject _rowPrefab; // Text "name — ping ms" + optional Kick Button

        ISessionController _subscribedSession;

        void OnEnable()
        {
            // the roster NetworkObject can spawn/sync after this OnEnable already ran, so a
            // one-shot Current read would leave a joining player with an empty list forever
            ActiveSession.Changed += OnSessionChanged;
            OnSessionChanged();
        }

        void OnDisable()
        {
            ActiveSession.Changed -= OnSessionChanged;
            if (_subscribedSession != null) _subscribedSession.Changed -= Refresh;
            _subscribedSession = null;
        }

        void OnSessionChanged()
        {
            if (_subscribedSession != null) _subscribedSession.Changed -= Refresh;
            _subscribedSession = ActiveSession.Current;
            if (_subscribedSession != null) _subscribedSession.Changed += Refresh;
            Refresh();
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
                var label = row.GetComponentInChildren<TMP_Text>();
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
            // routed through IConnectionService (not NetworkManager directly) so the kick is
            // also remembered for the session - lets the host's ApprovalHandler reject a rejoin
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
                GameServices.Current.Resolve<IConnectionService>().KickClient(clientId);
        }
    }
}
