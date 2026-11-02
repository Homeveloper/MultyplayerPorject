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
        [SerializeField] float _lookSensitivity = 2f;
        [SerializeField] Camera _camera;
        [SerializeField] InputActionAsset _inputActions;

        CharacterController _controller;
        InputAction _move;
        InputAction _look;
        float _pitch;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
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
            _inputActions.Enable();

            if (_camera != null) _camera.gameObject.SetActive(true);
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
        }
    }
}
