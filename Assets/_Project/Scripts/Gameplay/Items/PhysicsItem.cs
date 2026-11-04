using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Game.Gameplay.Items
{
    // Grabbable prop, always server-owned. Server writes the NetworkTransform the whole time -
    // while held it just applies the hand anchor pose sent by the holder's client.
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NetworkTransform))]
    public class PhysicsItem : NetworkBehaviour
    {
        [SerializeField] float _maxPickupDistance = 3f;

        public NetworkVariable<bool> IsHeld = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<ulong> HolderClientId = new(ulong.MaxValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        Rigidbody _rigidbody;
        Collider _collider;

        void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
        }

        public override void OnNetworkSpawn()
        {
            // without this the held item's collider pushes the holder's CharacterController around
            IsHeld.OnValueChanged += OnHeldChanged;
            ApplyColliderState(IsHeld.Value);
        }

        public override void OnNetworkDespawn() => IsHeld.OnValueChanged -= OnHeldChanged;

        void OnHeldChanged(bool previous, bool current) => ApplyColliderState(current);

        void ApplyColliderState(bool held)
        {
            if (_collider != null) _collider.enabled = !held;
        }

        public bool ServerTryPickup(ulong requesterClientId)
        {
            if (!IsServer) return false;
            if (IsHeld.Value) return false; // whoever lands first on the server wins the race

            if (NetworkManager.ConnectedClients.TryGetValue(requesterClientId, out var client) && client.PlayerObject != null)
            {
                var dist = Vector3.Distance(client.PlayerObject.transform.position, transform.position);
                if (dist > _maxPickupDistance) return false;
            }

            IsHeld.Value = true;
            HolderClientId.Value = requesterClientId;
            _rigidbody.isKinematic = true;
            NetworkObject.ChangeOwnership(requesterClientId);
            return true;
        }

        public void ServerUpdateHeldPose(Vector3 position, Quaternion rotation)
        {
            if (!IsServer || !IsHeld.Value) return;
            transform.SetPositionAndRotation(position, rotation);
        }

        public void ServerRelease()
        {
            if (!IsServer || !IsHeld.Value) return;
            ReturnToFreePhysics();
        }

        public void ServerThrow(Vector3 velocity)
        {
            if (!IsServer || !IsHeld.Value) return;
            ReturnToFreePhysics();
            _rigidbody.linearVelocity = velocity;
        }

        void ReturnToFreePhysics()
        {
            IsHeld.Value = false;
            HolderClientId.Value = ulong.MaxValue;
            _rigidbody.isKinematic = false;
            if (NetworkObject.OwnerClientId != NetworkManager.ServerClientId)
                NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
        }

        // holder disconnected mid-carry - drop it instead of leaving it stuck "held" forever
        public void ServerForceReleaseIfHeldBy(ulong clientId)
        {
            if (IsServer && IsHeld.Value && HolderClientId.Value == clientId)
                ReturnToFreePhysics();
        }

        public void ServerApplyPush(Vector3 horizontalVelocity)
        {
            if (!IsServer || IsHeld.Value) return;
            _rigidbody.linearVelocity = new Vector3(horizontalVelocity.x, _rigidbody.linearVelocity.y, horizontalVelocity.z);
        }

        public void ServerDelete()
        {
            if (!IsServer || !IsHeld.Value) return;
            NetworkObject.Despawn(true);
        }
    }
}
