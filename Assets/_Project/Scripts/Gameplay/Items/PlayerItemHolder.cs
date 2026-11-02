using Game.Core;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.Items
{
    /// <summary>
    /// Player-side hotkeys for spawn/pickup/drop/throw. Every request is a ServerRpc;
    /// the host (PhysicsItem/ItemSpawner) is the only one that decides whether it
    /// succeeds. While holding an item this client owns, it drives the item's transform
    /// toward the hand anchor each frame (see PhysicsItem ownership events).
    /// </summary>
    public class PlayerItemHolder : NetworkBehaviour
    {
        [SerializeField] Transform _handAnchor;
        [SerializeField] float _throwForce = 6f;
        [SerializeField] InputActionAsset _inputActions;

        PhysicsItem _heldItem;
        InputAction _spawn, _interact, _throw;
        IStatsService _stats;

        public void Construct(IStatsService stats) => _stats = stats;

        public override void OnNetworkSpawn()
        {
            PhysicsItem.OwnershipGainedLocally += OnOwnershipGained;
            PhysicsItem.OwnershipLostLocally += OnOwnershipLost;

            if (!IsOwner) return;

            var map = _inputActions.FindActionMap("Player", throwIfNotFound: false);
            _spawn = map?.FindAction("Spawn");
            _interact = map?.FindAction("Interact");
            _throw = map?.FindAction("Throw");
            _inputActions.Enable();
        }

        public override void OnNetworkDespawn()
        {
            PhysicsItem.OwnershipGainedLocally -= OnOwnershipGained;
            PhysicsItem.OwnershipLostLocally -= OnOwnershipLost;
        }

        void OnOwnershipGained(PhysicsItem item)
        {
            if (item.OwnerClientId == OwnerClientId && IsOwner) _heldItem = item;
        }

        void OnOwnershipLost(PhysicsItem item)
        {
            if (_heldItem == item) _heldItem = null;
        }

        void Update()
        {
            if (!IsOwner) return;

            if (_heldItem != null && _handAnchor != null)
            {
                _heldItem.transform.SetPositionAndRotation(_handAnchor.position, _handAnchor.rotation);
            }

            if (_spawn != null && _spawn.WasPressedThisFrame())
                RequestSpawnItemServerRpc();

            if (_heldItem == null)
            {
                if (_interact != null && _interact.WasPressedThisFrame())
                    RequestPickupNearestServerRpc();
            }
            else
            {
                if (_interact != null && _interact.WasPressedThisFrame())
                    RequestDropServerRpc();
                else if (_throw != null && _throw.WasPressedThisFrame())
                    RequestThrowServerRpc(transform.forward * _throwForce + Vector3.up * (_throwForce * 0.3f));
            }
        }

        [Rpc(SendTo.Server)]
        void RequestSpawnItemServerRpc()
        {
            ItemSpawner.Instance?.ServerSpawnItemInFrontOf(transform);
        }

        [Rpc(SendTo.Server)]
        void RequestPickupNearestServerRpc(RpcParams rpcParams = default)
        {
            var requester = rpcParams.Receive.SenderClientId;
            PhysicsItem nearest = null;
            float best = float.MaxValue;

            foreach (var item in FindObjectsByType<PhysicsItem>(FindObjectsSortMode.None))
            {
                if (item.IsHeld.Value) continue;
                var d = Vector3.Distance(item.transform.position, transform.position);
                if (d < best) { best = d; nearest = item; }
            }

            nearest?.ServerTryPickup(requester);
        }

        [Rpc(SendTo.Server)]
        void RequestDropServerRpc()
        {
            _heldItemServerRef?.ServerRelease();
        }

        [Rpc(SendTo.Server)]
        void RequestThrowServerRpc(Vector3 velocity)
        {
            _heldItemServerRef?.ServerThrow(velocity);
        }

        /// <summary>Server-side lookup of what this player currently holds (server doesn't
        /// trust the client's local `_heldItem` cache - it re-derives authority from the
        /// item's own replicated state).</summary>
        PhysicsItem _heldItemServerRef
        {
            get
            {
                foreach (var item in FindObjectsByType<PhysicsItem>(FindObjectsSortMode.None))
                    if (item.IsHeld.Value && item.HolderClientId.Value == OwnerClientId)
                        return item;
                return null;
            }
        }
    }
}
