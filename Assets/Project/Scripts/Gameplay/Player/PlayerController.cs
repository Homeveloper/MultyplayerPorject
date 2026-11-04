using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.Player
{
    // Owner-authoritative movement - only the owning client reads input, NetworkTransform
    // on the prefab replicates it. No real reason to bother with server reconciliation here,
    // it's co-op, not competitive.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : NetworkBehaviour
    {
        [SerializeField] float _moveSpeed = 5f;
        [SerializeField] float _lookSensitivity = 0.1f; // Look reads raw mouse delta in pixels, so this stays small
        [SerializeField] float _pushForce = 2.5f;
        [SerializeField] Camera _camera;
        [SerializeField] InputActionAsset _inputActions;

        static readonly Color[] _palette = { Color.white, Color.red, Color.green, Color.blue, Color.yellow, Color.magenta };

        // lets local-only UI (Tab player list) grab the owning player without an RPC round-trip
        public static PlayerController LocalInstance { get; private set; }

        CharacterController _controller;
        PlayerNetworkIdentity _identity;
        InputActionMap _playerMap;
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

            _playerMap = _inputActions.FindActionMap("Player", throwIfNotFound: false);
            _move = _playerMap?.FindAction("Move");
            _look = _playerMap?.FindAction("Look");
            _changeColor = _playerMap?.FindAction("ChangeColor");
            _inputActions.Enable();

            if (_camera != null) _camera.gameObject.SetActive(true);

            // also keeps the game view focused, otherwise hotkeys stop working after a UI click
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            LocalInstance = this;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;
            if (LocalInstance == this) LocalInstance = null;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // used while Tab is held so you can't move/throw/etc while reading the roster
        public void SetControlsEnabled(bool enabled)
        {
            if (_playerMap == null) return;
            if (enabled) _playerMap.Enable();
            else _playerMap.Disable();
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

        // CharacterController doesn't push rigidbodies by itself - push server-side so everyone sees the same result
        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!IsOwner) return;
            if (hit.moveDirection.y < -0.3f) return; // landing on top of it, not walking into it

            var item = hit.collider.GetComponentInParent<Game.Gameplay.Items.PhysicsItem>();
            if (item == null || item.IsHeld.Value) return;

            var pushDirection = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
            RequestPushItemServerRpc(new NetworkObjectReference(item.NetworkObject), pushDirection * _pushForce);
        }

        [Rpc(SendTo.Server)]
        void RequestPushItemServerRpc(NetworkObjectReference itemRef, Vector3 velocity)
        {
            if (itemRef.TryGet(out var netObj))
                netObj.GetComponent<Game.Gameplay.Items.PhysicsItem>()?.ServerApplyPush(velocity);
        }
    }
}
