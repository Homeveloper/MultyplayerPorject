using Game.Gameplay.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.UI
{
    // hold Tab to show the player list, release to hide it. Reads the keyboard directly since
    // this is local-only UI, not part of the networked player's action set.
    public class PlayerListToggleController : MonoBehaviour
    {
        [SerializeField] GameObject _panel;

        void Update()
        {
            if (_panel == null) return;
            var kb = Keyboard.current;
            bool held = kb != null && kb.tabKey.isPressed;
            if (held != _panel.activeSelf) SetOpen(held);
        }

        void SetOpen(bool open)
        {
            _panel.SetActive(open);

            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = open;

            PlayerController.LocalInstance?.SetControlsEnabled(!open);
        }
    }
}
