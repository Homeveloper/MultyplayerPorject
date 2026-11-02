using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Game.Gameplay.Items
{
    /// <summary>
    /// A grabbable physics prop. Always server-owned; the server is the only writer of
    /// its NetworkTransform, for its whole lifetime - while held, the holder's client
    /// sends where its hand anchor is (via PlayerItemHolder's RPC) and the server
    /// applies that to the item's transform; on drop/throw the server runs the physics
    /// simulation itself. Every client therefore sees the same position stream from one
    /// source, so there is nothing to reconcile and no teleport when it lands.
    /// </summary>
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
            // Runs on every peer (server and all clients), each with its own local physics
            // world - every one of them needs to stop colliding with the item while it's
            // held, or its collider overlapping the holder's CharacterController pushes the
            // player around as the item follows the hand each frame.
            IsHeld.OnValueChanged += OnHeldChanged;
            ApplyColliderState(IsHeld.Value);
        }

        public override void OnNetworkDespawn() => IsHeld.OnValueChanged -= OnHeldChanged;

        void OnHeldChanged(bool previous, bool current) => ApplyColliderState(current);

        void ApplyColliderState(bool held)
        {
            if (_collider != null) _collider.enabled = !held;
        }

        /// <summary>Server-only: validates and performs a pickup. Returns false (no state change,
        /// no desync) if the item was already held or the requester is out of range.</summary>
        public bool ServerTryPickup(ulong requesterClientId)
        {
            if (!IsServer) return false;
            if (IsHeld.Value) return false; // race: whoever's request lands first on the server wins

            if (NetworkManager.ConnectedClients.TryGetValue(requesterClientId, out var client) && client.PlayerObject != null)
            {
                var dist = Vector3.Distance(client.PlayerObject.transform.position, transform.position);
                if (dist > _maxPickupDistance) return false;
            }

            IsHeld.Value = true;
            HolderClientId.Value = requesterClientId;
            _rigidbody.isKinematic = true;
            return true;
        }

        /// <summary>Server-only: the current holder's hand-anchor pose, applied each frame while
        /// held - validated by caller identity (see PlayerItemHolder), trusted on position since
        /// this is a cooperative prototype with no incentive to cheat a carried prop's pose.</summary>
        public void ServerUpdateHeldPose(Vector3 position, Quaternion rotation)
        {
            if (!IsServer || !IsHeld.Value) return;
            transform.SetPositionAndRotation(position, rotation);
        }

        /// <summary>Server-only: the holder let go without throwing - falls where it is.</summary>
        public void ServerRelease()
        {
            if (!IsServer || !IsHeld.Value) return;
            ReturnToFreePhysics();
        }

        /// <summary>Server-only: applies the throw velocity. Simulated only here, on the host, so
        /// every client sees the same trajectory and landing point.</summary>
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
        }

        /// <summary>Server-only: called when the holder disconnects mid-hold - drop in place instead of
        /// leaving the item stuck "held" by a client that no longer exists.</summary>
        public void ServerForceReleaseIfHeldBy(ulong clientId)
        {
            if (IsServer && IsHeld.Value && HolderClientId.Value == clientId)
                ReturnToFreePhysics();
        }

        /// <summary>Server-only: a walking player's CharacterController bumped into this item -
        /// nudge it along the ground instead of the player walking through/over it, simulated
        /// only here so every client sees the same push.</summary>
        public void ServerApplyPush(Vector3 horizontalVelocity)
        {
            if (!IsServer || IsHeld.Value) return;
            _rigidbody.linearVelocity = new Vector3(horizontalVelocity.x, _rigidbody.linearVelocity.y, horizontalVelocity.z);
        }
    }
}
