using Game.Core;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.Items
{
    // hotkeys for spawn/pickup/drop/throw - every request is a ServerRpc, host decides what succeeds
    public class PlayerItemHolder : NetworkBehaviour
    {
        [SerializeField] Transform _handAnchor;
        [SerializeField] float _throwForce = 6f;
        [SerializeField] InputActionAsset _inputActions;

        PhysicsItem _heldItem;
        InputAction _spawn, _interact, _throw, _delete;
        IStatsService _stats;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;

            _stats = GameServices.Current?.Resolve<IStatsService>();

            var map = _inputActions.FindActionMap("Player", throwIfNotFound: false);
            _spawn = map?.FindAction("Spawn");
            _interact = map?.FindAction("Interact");
            _throw = map?.FindAction("Throw");
            _delete = map?.FindAction("Delete");
            _inputActions.Enable();
        }

        void Update()
        {
            if (!IsOwner) return;

            _heldItem = FindMyHeldItem();

            if (_heldItem != null && _handAnchor != null)
                UpdateHeldPoseServerRpc(_handAnchor.position, _handAnchor.rotation);

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
                {
                    RequestThrowServerRpc(transform.forward * _throwForce + Vector3.up * (_throwForce * 0.3f));
                    _stats?.Increment("items_thrown"); // fired locally, on the throwing client - not routed through the host
                }
                else if (_delete != null && _delete.WasPressedThisFrame())
                {
                    RequestDeleteServerRpc();
                }
            }
        }

        PhysicsItem FindMyHeldItem()
        {
            foreach (var item in FindObjectsByType<PhysicsItem>(FindObjectsSortMode.None))
                if (item.IsHeld.Value && item.HolderClientId.Value == OwnerClientId)
                    return item;
            return null;
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
        void UpdateHeldPoseServerRpc(Vector3 position, Quaternion rotation, RpcParams rpcParams = default)
        {
            FindServerHeldItem(rpcParams.Receive.SenderClientId)?.ServerUpdateHeldPose(position, rotation);
        }

        [Rpc(SendTo.Server)]
        void RequestDropServerRpc(RpcParams rpcParams = default)
        {
            FindServerHeldItem(rpcParams.Receive.SenderClientId)?.ServerRelease();
        }

        [Rpc(SendTo.Server)]
        void RequestThrowServerRpc(Vector3 velocity, RpcParams rpcParams = default)
        {
            FindServerHeldItem(rpcParams.Receive.SenderClientId)?.ServerThrow(velocity);
        }

        [Rpc(SendTo.Server)]
        void RequestDeleteServerRpc(RpcParams rpcParams = default)
        {
            FindServerHeldItem(rpcParams.Receive.SenderClientId)?.ServerDelete();
        }

        // trusts only the RPC sender's own clientId, never one passed in from the client
        static PhysicsItem FindServerHeldItem(ulong clientId)
        {
            foreach (var item in FindObjectsByType<PhysicsItem>(FindObjectsSortMode.None))
                if (item.IsHeld.Value && item.HolderClientId.Value == clientId)
                    return item;
            return null;
        }
    }
}
