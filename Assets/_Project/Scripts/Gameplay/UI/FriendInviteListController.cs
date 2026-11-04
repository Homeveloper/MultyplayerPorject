using Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.UI
{
    // custom friends list + invite button - bypasses the native Steam overlay, which doesn't
    // reliably hook into every process (see LobbyUIController's invite button).
    public class FriendInviteListController : MonoBehaviour
    {
        [SerializeField] GameObject _panelRoot;
        [SerializeField] Transform _rowContainer;
        [SerializeField] GameObject _rowPrefab; // TMP_Text name + Button "Invite"
        [SerializeField] TMP_Text _emptyStateText;
        [SerializeField] Button _closeButton;
        [SerializeField] Button _openButton;

        ILobbyService _lobby;

        void Start()
        {
            _lobby = GameServices.Current.Resolve<ILobbyService>();
            _closeButton?.onClick.AddListener(Close);
            _openButton?.onClick.AddListener(Open);
            if (_panelRoot != null) _panelRoot.SetActive(false);
        }

        void Open()
        {
            if (_panelRoot == null || _rowContainer == null || _rowPrefab == null) return;

            foreach (Transform child in _rowContainer) Destroy(child.gameObject);

            int shown = 0;
            foreach (var friend in _lobby.GetFriends())
            {
                if (!friend.IsOnline) continue; // offline friends can't join right now - hide them, not grey them out

                var row = Instantiate(_rowPrefab, _rowContainer);
                var label = row.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = friend.Name;

                var inviteButton = row.GetComponentInChildren<Button>();
                if (inviteButton != null)
                {
                    var steamId = friend.SteamId;
                    inviteButton.onClick.AddListener(() => _lobby.InviteFriend(steamId));
                }
                shown++;
            }

            if (_emptyStateText != null)
                _emptyStateText.gameObject.SetActive(shown == 0);

            _panelRoot.SetActive(true);
        }

        void Close()
        {
            if (_panelRoot != null) _panelRoot.SetActive(false);
        }
    }
}
