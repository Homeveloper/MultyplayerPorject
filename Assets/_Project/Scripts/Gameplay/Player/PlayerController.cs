using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// Owner-authoritative movement: only the owning client reads input and moves its
    /// own CharacterController; a NetworkTransform (configured as owner-authoritative in
    /// the prefab) replicates the result. Chosen over server-authoritative + prediction
    /// because this is a cooperative, non-competitive prototype - there is no incentive
    /// to cheat movement, so the extra complexity of server reconciliation isn't worth it
    /// (see README "Networking decisions" for the full rationale).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : NetworkBehaviour
    {
        [SerializeField] float _moveSpeed = 5f;
        [SerializeField] float _lookSensitivity = 0.1f; // Look reads raw mouse delta in pixels, so this stays small
        [SerializeField] Camera _camera;
        [SerializeField] InputActionAsset _inputActions;

        static readonly Color[] _palette = { Color.white, Color.red, Color.green, Color.blue, Color.yellow, Color.magenta };

        CharacterController _controller;
        PlayerNetworkIdentity _identity;
        InputAction _move;
        InputAction _look;
        InputAction _changeColor;
        float _pitch;
        int _colorIndex;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _identity = GetComponent<PlayerNetworkIdentity>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                if (_camera != null) _camera.gameObject.SetActive(false);
                enabled = false;
                return;
            }

            var map = _inputActions.FindActionMap("Player", throwIfNotFound: false);
            _move = map?.FindAction("Move");
            _look = map?.FindAction("Look");
            _changeColor = map?.FindAction("ChangeColor");
            _inputActions.Enable();

            if (_camera != null) _camera.gameObject.SetActive(true);

            // Locking the cursor also keeps the Game view reliably focused for keyboard/mouse
            // input - without this, hotkeys can silently stop registering after a UI click
            // (e.g. the Lobby's Start button) moved focus away from the game view.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Update()
        {
            if (!IsOwner) return;

            var moveInput = _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;
            var lookInput = _look != null ? _look.ReadValue<Vector2>() : Vector2.zero;

            var move = (transform.right * moveInput.x + transform.forward * moveInput.y) * _moveSpeed;
            move.y = -2f; // simple grounding, not a full gravity model - fine for a flat prototype level
            _controller.Move(move * Time.deltaTime);

            transform.Rotate(Vector3.up, lookInput.x * _lookSensitivity);
            if (_camera != null)
            {
                _pitch = Mathf.Clamp(_pitch - lookInput.y * _lookSensitivity, -80f, 80f);
                _camera.transform.localEulerAngles = new Vector3(_pitch, 0f, 0f);
            }

            if (_changeColor != null && _changeColor.WasPressedThisFrame() && _identity != null)
            {
                _colorIndex = (_colorIndex + 1) % _palette.Length;
                _identity.RequestColorChange(_palette[_colorIndex]);
            }
        }
    }
}
