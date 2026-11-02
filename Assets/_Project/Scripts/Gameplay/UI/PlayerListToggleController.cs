using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.UI
{
    /// <summary>Tab toggles the in-game player list / pause panel. Reads the keyboard directly
    /// rather than through the shared gameplay InputActionAsset - this is local UI, not
    /// something that needs to be part of the networked player's action set.</summary>
    public class PlayerListToggleController : MonoBehaviour
    {
        [SerializeField] GameObject _panel;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.tabKey.wasPressedThisFrame && _panel != null)
                _panel.SetActive(!_panel.activeSelf);
        }
    }
}
